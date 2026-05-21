#!/bin/bash
# Copies gitignored local config from the main worktree into the current worktree.
# Run this once after creating a new worktree to get .claude/local/, .github/skills/local/, and .vscode/.
#
# Usage: bash scripts/setup-worktree.sh

set -e

MAIN_REPO=$(git worktree list --porcelain | awk 'NR==1{print $2}')

if [ -z "$MAIN_REPO" ]; then
  echo "Error: could not determine main worktree path." >&2
  exit 1
fi

if [ "$MAIN_REPO" = "$(pwd)" ]; then
  echo "Already in the main worktree — nothing to copy."
  exit 0
fi

echo "Main worktree: $MAIN_REPO"
echo "Current worktree: $(pwd)"
echo ""

copy_if_exists() {
  local src="$1"
  local dst="$2"
  if [ -d "$src" ]; then
    mkdir -p "$dst"
    cp -rn "$src/." "$dst/" || true
    echo "✓ Copied $src → $dst"
  else
    echo "  (skipped $src — not found in main worktree)"
  fi
}

copy_if_exists "$MAIN_REPO/.claude/local"        ".claude/local"
copy_if_exists "$MAIN_REPO/.github/skills/local" ".github/skills/local"
copy_if_exists "$MAIN_REPO/.vscode"              ".vscode"

echo ""
echo "Done. Local config is ready in this worktree."
