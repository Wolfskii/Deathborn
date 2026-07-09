#!/usr/bin/env bash
# Render GitHub Release notes with clickable asset links.
set -euo pipefail

TAG="${1:?release tag}"
SHA="${2:?target commit sha}"
REPO="${GITHUB_REPOSITORY:?}"

prev="$(git tag -l 'v[0-9]*.[0-9]*.[0-9]*' --sort=-v:refname | grep -vFx "$TAG" | head -1 || true)"
BASE="https://github.com/${REPO}/releases/download/${TAG}"

mapfile -t assets < <(gh release view "$TAG" --json assets -q '.assets[].name' | sort)

asset_link() {
  local pattern="$1"
  local name
  name="$(printf '%s\n' "${assets[@]}" | grep -E "$pattern" | head -1 || true)"
  if [ -n "$name" ]; then
    printf '[%s](%s/%s)' "$name" "$BASE" "$name"
  else
    printf '_missing_'
  fi
}

{
  echo "## What's Changed"
  echo ""
  if [ -z "$prev" ]; then
    git log --pretty=format:'- %s (%h)' --reverse "$SHA"
  else
    git log "${prev}..${SHA}" --pretty=format:'- %s (%h)' --reverse
  fi
  echo ""
  echo ""
  echo "## Downloads"
  echo ""

  if [ "${#assets[@]}" -eq 0 ]; then
    echo "_No release files were uploaded._"
  else
    echo "### All files"
    echo ""
    for name in "${assets[@]}"; do
      echo "- [${name}](${BASE}/${name})"
    done
    echo ""
    echo "### Quick picks"
    echo ""
    echo "| Platform | Portable | Installer |"
    echo "| --- | --- | --- |"
    echo "| Windows x64 | $(asset_link 'Deathborn-.*-win-x64\.zip$') | $(asset_link 'Deathborn-.*-win-x64-Setup\.exe$') |"
    echo "| Linux x64 | $(asset_link 'Deathborn-.*-linux-x64\.zip$') | $(asset_link 'Deathborn-.*-linux-x64\.tar\.gz$') (run \`install.sh\`) |"
    echo "| macOS Intel | $(asset_link 'Deathborn-.*-osx-x64\.zip$') | $(asset_link 'Deathborn-.*-osx-x64\.dmg$') |"
    echo "| macOS Apple Silicon | $(asset_link 'Deathborn-.*-osx-arm64\.zip$') | $(asset_link 'Deathborn-.*-osx-arm64\.dmg$') |"
  fi

  echo ""
  if [ -n "$prev" ]; then
    echo "**Full Changelog**: https://github.com/${REPO}/compare/${prev}...${TAG}"
  fi
} 
