#!/usr/bin/env bash
#
# Publishes the Markdown pages in docs/ to the repository wiki.
#
# The wiki lives in a separate git repository (<repo>.wiki.git). Pushing to it
# requires a token that is allowed to write to the wiki:
#   * set a repository secret named WIKI_TOKEN (classic PAT with the `repo` scope), or
#   * rely on GITHUB_TOKEN, which is used when WIKI_TOKEN is not configured. Some
#     repositories do not allow GITHUB_TOKEN to push to the wiki; in that case the
#     script emits a warning and skips the sync instead of failing the workflow.
#
# The script:
#   1. clones the wiki repository,
#   2. copies every docs/*.md page (YAML front matter stripped, index.md -> Home.md),
#   3. regenerates _Sidebar.md from the page titles and sections,
#   4. commits and pushes only when something changed.

set -euo pipefail

DOCS_DIR="docs"
REPO="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
TOKEN="${WIKI_TOKEN:?A wiki token is required (WIKI_TOKEN or GITHUB_TOKEN)}"

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

WIKI_URL="https://x-access-token:${TOKEN}@github.com/${REPO}.wiki.git"

if ! git clone --depth 1 "$WIKI_URL" "$WORK_DIR/wiki" 2>"$WORK_DIR/clone.log"; then
  echo "::warning::Could not clone the wiki repository (${REPO}.wiki). Create the first wiki page in the GitHub UI (or configure the WIKI_TOKEN secret) and re-run the workflow."
  exit 0
fi

WIKI_DIR="$WORK_DIR/wiki"

# Detect the wiki's default branch (usually master, sometimes main)
WIKI_BRANCH="$(git -C "$WIKI_DIR" symbolic-ref --short HEAD)"

# Extract only the YAML front matter (between the first two "---" lines)
extract_front_matter() {
  awk 'NR == 1 && $0 == "---" { fm = 1; next }
       fm == 1 && $0 == "---" { exit }
       fm == 1 { print }' "$1"
}

# Read a single value from YAML front matter
front_matter_value() {
  printf '%s\n' "$1" | sed -n "s/^$2:[[:space:]]*//p" | head -n 1
}

# Remove the YAML front matter from a page so the wiki renders the content directly
strip_front_matter() {
  awk 'BEGIN { fm = 0 }
       NR == 1 && $0 == "---" { fm = 1; next }
       fm == 1 && $0 == "---" { fm = 2; next }
       fm != 1 { print }' "$1"
}

shopt -s nullglob

files=()
pages=()
titles=()
parents=()
navs=()

for file in "$DOCS_DIR"/*.md; do
  base="$(basename "$file")"
  page="${base%.md}"
  if [ "$page" = "index" ]; then
    page="Home"
  fi

  fm="$(extract_front_matter "$file")"
  title="$(front_matter_value "$fm" title)"
  parent="$(front_matter_value "$fm" parent)"
  nav="$(front_matter_value "$fm" nav_order)"
  [ -z "$title" ] && title="$page"
  [ -z "$nav" ] && nav=999

  files+=("$file")
  pages+=("$page")
  titles+=("$title")
  parents+=("$parent")
  navs+=("$nav")
done

# Map each parent page title to its nav_order so sections are ordered like the site
declare -A parent_nav=()
for i in "${!files[@]}"; do
  if [ -z "${parents[$i]}" ]; then
    parent_nav["${titles[$i]}"]="${navs[$i]}"
  fi
done

# Replace the existing Markdown pages, keeping non-Markdown assets untouched
find "$WIKI_DIR" -maxdepth 1 -name "*.md" -delete

entries=()
for i in "${!files[@]}"; do
  file="${files[$i]}"
  page="${pages[$i]}"
  parent="${parents[$i]}"
  nav="${navs[$i]}"

  strip_front_matter "$file" > "$WIKI_DIR/$page.md"

  if [ -z "$parent" ]; then
    section_nav="$nav"
  else
    section_nav="${parent_nav[$parent]:-999}"
  fi

  entries+=("${section_nav}|${parent}|${nav}|${titles[$i]}|${page}")
done

# Regenerate the wiki sidebar: Home first, then top-level pages, then sections
{
  echo "**[Home](Home)**"
  if [ "${#entries[@]}" -gt 0 ]; then
    printf '%s\n' "${entries[@]}" | sort -t'|' -k1,1n -k2,2 -k3,3n | awk -F'|' '
      { lines[NR] = $0 }
      END {
        for (i = 1; i <= NR; i++) {
          split(lines[i], f, "|")
          if (f[2] == "" && f[5] != "Home") printf "- [%s](%s)\n", f[4], f[5]
        }
        for (i = 1; i <= NR; i++) {
          split(lines[i], f, "|")
          if (f[2] != "" && !(f[2] in seen)) {
            seen[f[2]] = 1
            print ""; print "**" f[2] "**"
            for (j = 1; j <= NR; j++) {
              split(lines[j], g, "|")
              if (g[2] == f[2]) printf "- [%s](%s)\n", g[4], g[5]
            }
          }
        }
      }'
  fi
} > "$WIKI_DIR/_Sidebar.md"

cd "$WIKI_DIR"
git config user.name "github-actions[bot]"
git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git add -A

if git diff --cached --quiet; then
  echo "Wiki is already up to date."
  exit 0
fi

git commit -m "Sync documentation from ${GITHUB_SHA::7}"
git push origin "HEAD:${WIKI_BRANCH}"
echo "Wiki updated."
