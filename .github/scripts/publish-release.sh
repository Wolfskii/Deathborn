#!/usr/bin/env bash
# Verify git tag, publish release notes, and mark as Latest on GitHub.
set -euo pipefail

TAG="${1:?release tag}"
SHA="${2:?target commit sha}"
REPO="${GITHUB_REPOSITORY:?}"

echo "Publishing ${TAG} at ${SHA}"

if ! gh release view "$TAG" >/dev/null 2>&1; then
  echo "Release ${TAG} does not exist." >&2
  exit 1
fi

asset_count="$(gh release view "$TAG" --json assets -q '.assets | length')"
echo "Release assets: ${asset_count}"
if [ "${asset_count}" -eq 0 ]; then
  echo "No release files uploaded; leaving draft unpublished." >&2
  exit 1
fi

# gh release create should create the tag; ensure it exists and points at this commit.
tag_sha="$(git ls-remote origin "refs/tags/${TAG}^{}" 2>/dev/null | awk '{print $1}' | head -1 || true)"
if [ -z "$tag_sha" ]; then
  echo "Tag ${TAG} missing on origin; creating at ${SHA}"
  gh api --method POST "/repos/${REPO}/git/refs" \
    -f ref="refs/tags/${TAG}" \
    -f sha="${SHA}" >/dev/null
elif [ "$tag_sha" != "$SHA" ]; then
  echo "Tag ${TAG} points at ${tag_sha}; moving to ${SHA}"
  gh api --method PATCH "/repos/${REPO}/git/refs/tags/${TAG}" \
    -f sha="${SHA}" \
    -F force=true >/dev/null
fi

tag_sha="$(git ls-remote origin "refs/tags/${TAG}^{}" 2>/dev/null | awk '{print $1}' | head -1 || true)"
echo "Tag ${TAG} -> ${tag_sha:-<missing>}"

mkdir -p release
gh release download "$TAG" --dir release
(
  cd release
  find . -maxdepth 1 -type f ! -name 'SHA256SUMS.txt' -print0 | sort -z | xargs -0 sha256sum > SHA256SUMS.txt
)
gh release upload "$TAG" release/SHA256SUMS.txt --clobber

bash "$(dirname "$0")/build-release-notes.sh" "$TAG" "$SHA" > release-notes.md

release_id="$(gh release view "$TAG" --json databaseId -q '.databaseId')"
jq -n --rawfile body release-notes.md \
  '{body: $body, draft: false, make_latest: true, name: $name, tag_name: $tag}' \
  --arg name "Deathborn ${TAG}" \
  --arg tag "$TAG" \
  | gh api --method PATCH "/repos/${REPO}/releases/${release_id}" --input - >/dev/null

is_latest="$(gh release view "$TAG" --json isLatest -q '.isLatest')"
is_draft="$(gh release view "$TAG" --json isDraft -q '.isDraft')"
echo "Release ${TAG}: draft=${is_draft} latest=${is_latest}"

if [ "$is_draft" != "false" ] || [ "$is_latest" != "true" ]; then
  echo "Release publish verification failed." >&2
  exit 1
fi

echo "Published ${TAG} with ${asset_count} asset(s) plus SHA256SUMS.txt"
