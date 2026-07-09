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

resolve_tag_commit() {
  local tag="$1"
  if ! gh api "/repos/${REPO}/git/refs/tags/${tag}" >/dev/null 2>&1; then
    return 1
  fi
  local type obj_sha
  type="$(gh api "/repos/${REPO}/git/refs/tags/${tag}" -q '.object.type')"
  obj_sha="$(gh api "/repos/${REPO}/git/refs/tags/${tag}" -q '.object.sha')"
  if [ "$type" = "tag" ]; then
    obj_sha="$(gh api "/repos/${REPO}/git/tags/${obj_sha}" -q '.object.sha')"
  fi
  printf '%s' "$obj_sha"
}

tag_sha=""
if tag_sha="$(resolve_tag_commit "$TAG")"; then
  echo "Tag ${TAG} already exists at ${tag_sha}"
  if [ "$tag_sha" != "$SHA" ]; then
    echo "Moving tag ${TAG} from ${tag_sha} to ${SHA}"
    gh api --method PATCH "/repos/${REPO}/git/refs/tags/${TAG}" \
      -f sha="${SHA}" \
      -F force=true >/dev/null
  fi
else
  echo "Creating tag ${TAG} at ${SHA}"
  if ! gh api --method POST "/repos/${REPO}/git/refs" \
    -f ref="refs/tags/${TAG}" \
    -f sha="${SHA}" >/dev/null 2>post_err.txt; then
    if grep -q 'Reference already exists' post_err.txt || resolve_tag_commit "$TAG" >/dev/null; then
      tag_sha="$(resolve_tag_commit "$TAG")"
      echo "Tag ${TAG} already exists on GitHub at ${tag_sha}"
    else
      cat post_err.txt >&2
      exit 1
    fi
  else
    tag_sha="$(resolve_tag_commit "$TAG")"
  fi
fi

echo "Tag ${TAG} -> ${tag_sha:-<missing>}"

bash "$(dirname "$0")/build-release-notes.sh" "$TAG" "$SHA" > release-notes.md

release_id="$(gh release view "$TAG" --json databaseId -q '.databaseId')"
jq -n --rawfile body release-notes.md \
  '{body: $body, draft: false, make_latest: true, name: $name, tag_name: $tag}' \
  --arg name "Deathborn ${TAG}" \
  --arg tag "$TAG" \
  | gh api --method PATCH "/repos/${REPO}/releases/${release_id}" --input - >/dev/null

is_draft="$(gh release view "$TAG" --json isDraft -q '.isDraft')"
is_latest="$(gh release list --limit 50 --json tagName,isLatest \
  -q ".[] | select(.tagName==\"${TAG}\") | .isLatest")"
echo "Release ${TAG}: draft=${is_draft} latest=${is_latest}"

if [ "$is_draft" != "false" ]; then
  echo "Release is still a draft." >&2
  exit 1
fi

if [ "$is_latest" != "true" ]; then
  echo "Release is not marked Latest; retrying make_latest"
  gh api --method PATCH "/repos/${REPO}/releases/${release_id}" \
    -f make_latest=true >/dev/null
  is_latest="$(gh release list --limit 50 --json tagName,isLatest \
    -q ".[] | select(.tagName==\"${TAG}\") | .isLatest")"
fi

if [ "$is_latest" != "true" ]; then
  echo "Release publish verification failed (not Latest)." >&2
  exit 1
fi

echo "Published ${TAG} with ${asset_count} client asset(s)"
