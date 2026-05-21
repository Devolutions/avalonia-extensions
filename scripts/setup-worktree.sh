#!/bin/bash
# Copies gitignored local config from the main worktree into the current worktree.
# Run this once after creating a new worktree to get .claude/local/, .github/skills/local/, and .vscode/.
#
# Usage: bash scripts/setup-worktree.sh

set -e

# Derive main worktree from the shared .git dir — robust against spaces in paths
# and always finds the correct main worktree regardless of listing order.
GIT_COMMON_DIR=$(git rev-parse --git-common-dir)
# In the main worktree git returns ".git" (relative); resolve to an absolute path.
if [[ "$GIT_COMMON_DIR" != /* ]]; then
  GIT_COMMON_DIR="$(pwd -P)/$GIT_COMMON_DIR"
fi
MAIN_REPO=$(dirname "$GIT_COMMON_DIR")

if [ -z "$MAIN_REPO" ]; then
  echo "Error: could not determine main worktree path." >&2
  exit 1
fi

# Normalise the main worktree path (resolve symlinks).
MAIN_REPO=$(cd "$MAIN_REPO" && pwd -P)

# Get the current worktree root via git instead of pwd — correct even when the
# script is invoked from a subdirectory, and ensures destination paths are right.
CURRENT_WORKTREE=$(cd "$(git rev-parse --show-toplevel)" && pwd -P)
cd "$CURRENT_WORKTREE"

if [ "$MAIN_REPO" = "$CURRENT_WORKTREE" ]; then
  echo "Already in the main worktree — nothing to copy."
  exit 0
fi

echo "Main worktree: $MAIN_REPO"
echo "Current worktree: $CURRENT_WORKTREE"
echo ""

copy_if_exists() {
  local src="$1"
  local dst="$2"
  if [ -d "$src" ]; then
    mkdir -p "$dst"
    # cp -n exits 1 on macOS/BSD when some destination files already existed and were
    # skipped — that is not an error. Exit codes > 1 indicate genuine I/O failures.
    local rc=0
    cp -rn "$src/." "$dst/" || rc=$?
    if [ "$rc" -gt 1 ]; then
      echo "  ✗ Failed to copy $src → $dst" >&2
      exit "$rc"
    fi
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
