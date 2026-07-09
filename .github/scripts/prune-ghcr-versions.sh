#!/usr/bin/env bash
# Keep the newest N GHCR container package versions; delete older ones.
set -euo pipefail

KEEP="${PRUNE_KEEP:-${1:-5}}"
REPO="${GITHUB_REPOSITORY:?}"
OWNER="${REPO%%/*}"
PKG="$(echo "${REPO##*/}" | tr '[:upper:]' '[:lower:]')"

owner_type="$(gh api "/repos/${REPO}" -q '.owner.type')"
if [ "$owner_type" = "Organization" ]; then
  base="/orgs/${OWNER}/packages/container/${PKG}/versions"
else
  base="/users/${OWNER}/packages/container/${PKG}/versions"
fi

if ! gh api "${base}?per_page=1&state=active" >/dev/null 2>&1; then
  echo "No GHCR package ${PKG} yet; skipping version prune."
  exit 0
fi

mapfile -t stale_ids < <(
  gh api "${base}?per_page=100&state=active" \
    --jq "sort_by(.created_at) | reverse | .[${KEEP}:] | .[].id"
)

if [ "${#stale_ids[@]}" -eq 0 ]; then
  echo "GHCR package ${PKG}: nothing to prune (keeping ${KEEP} newest)."
  exit 0
fi

for id in "${stale_ids[@]}"; do
  [ -n "$id" ] || continue
  echo "Deleting GHCR package version ${id}"
  gh api --method DELETE "${base}/${id}" || true
done

echo "GHCR package ${PKG}: pruned ${#stale_ids[@]} version(s); keeping ${KEEP} newest."
