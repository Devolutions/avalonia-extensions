# Copies gitignored local config from the main worktree into the current worktree.
# Run this once after creating a new worktree to get .claude/local/, .github/skills/local/, and .vscode/.
#
# Usage: pwsh scripts/setup-worktree.ps1

$ErrorActionPreference = "Stop"

# Derive main worktree from the shared .git dir — robust against spaces in paths
# and always finds the correct main worktree regardless of listing order.
$gitCommonDir = (git rev-parse --git-common-dir).Trim()
# In the main worktree git returns ".git" (relative); resolve to an absolute path.
if (-not [System.IO.Path]::IsPathRooted($gitCommonDir)) {
    $gitCommonDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $gitCommonDir))
}
$mainRepo = Split-Path $gitCommonDir -Parent

if (-not $mainRepo) {
    Write-Error "Could not determine main worktree path."
    exit 1
}

# Normalise both paths (resolve symlinks, strip trailing separators) before comparing.
$mainRepo        = (Resolve-Path $mainRepo).Path.TrimEnd('\', '/')
$currentWorktree = (Resolve-Path (Get-Location)).Path.TrimEnd('\', '/')

if ($mainRepo -eq $currentWorktree) {
    Write-Host "Already in the main worktree - nothing to copy."
    exit 0
}

Write-Host "Main worktree: $mainRepo"
Write-Host "Current worktree: $currentWorktree"
Write-Host ""

function Copy-IfExists {
    param([string]$Src, [string]$Dst)
    if (Test-Path $Src) {
        New-Item -ItemType Directory -Force -Path $Dst | Out-Null
        Get-ChildItem -Path $Src -Recurse -File | ForEach-Object {
            $relative = $_.FullName.Substring($Src.Length).TrimStart('\', '/')
            $target = Join-Path $Dst $relative
            if (-not (Test-Path $target)) {
                $targetDir = Split-Path $target -Parent
                New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
                Copy-Item $_.FullName $target
            }
        }
        Write-Host "✓ Copied $Src -> $Dst"
    } else {
        Write-Host "  (skipped $Src - not found in main worktree)"
    }
}

Copy-IfExists (Join-Path $mainRepo ".claude/local")         ".claude/local"
Copy-IfExists (Join-Path $mainRepo ".github/skills/local")  ".github/skills/local"
Copy-IfExists (Join-Path $mainRepo ".vscode")               ".vscode"

Write-Host ""
Write-Host "Done. Local config is ready in this worktree."
