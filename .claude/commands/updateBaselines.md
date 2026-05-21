---
description: "/updateBaselines - Run visual regression tests and update baseline screenshots across Mac, Windows, and Linux VMs, then open a draft PR"
---

You are the **baseline update specialist**. Your job is to run the visual regression tests, update screenshot baselines on all platforms (Mac, Windows, Linux) via SSH, and open a draft PR — all in one automated workflow.

**Before starting**: Read `.claude/docs/processes/baseline-vm-setup.md` to verify SSH aliases and VM repo paths are configured.

---

## Step 1: VM readiness check

Show this message and wait for the user to press Enter (empty reply) before continuing:

> **Before I start, please make sure:**
> - ✅ Windows VM is running
> - ✅ Linux VM is running  
> - ✅ WARP is **disconnected** on both VMs (the Parallels subnet is not excluded from the managed WARP policy — WARP will block SSH)
>
> Type **Y** when ready, or anything else to cancel.

If the user types Y (or y), continue to Step 2. If they type anything else, stop and wait for their instruction.

---

## Step 2: Verify SSH connectivity

Check both VMs are reachable:
```bash
ssh baseline-win "echo win-ok" && ssh baseline-linux "echo linux-ok"
```

If either fails:
- Check the VM is running and WARP is off
- Refer to `.claude/docs/processes/baseline-vm-setup.md` for troubleshooting

---

## Step 3: Verify SSH config aliases exist

1. Check that `~/.ssh/config` contains entries for `baseline-win` and `baseline-linux`:
   ```bash
   grep -A3 "baseline-win\|baseline-linux" ~/.ssh/config
   ```
2. If the aliases are missing, stop and tell the user to follow the setup guide at `.claude/docs/processes/baseline-vm-setup.md`.

---

## Step 4: Determine PR range for title and description

**Find the latest merged PR (y):**
```bash
gh pr list --repo Devolutions/avalonia-extensions --state merged --limit 1 --json number,title --jq '.[0]'
```

**Find the last "Baseline updates" PR to determine x (first PR after it):**
```bash
gh pr list --repo Devolutions/avalonia-extensions --state merged --limit 20 --json number,title --jq '.[] | "\(.number) \(.title)"'
```

Find the most recent PR whose title starts with "Baseline updates". The first PR after that is `x`. Store:
- `LAST_PR` = y (latest merged PR number) — used for the branch name `baseline-updates-{LAST_PR}`
- `FIRST_PR` = x (first PR after the last baseline update PR)
- `PR_RANGE_TITLE` = "Baseline updates for changes in PR #x - #y" (or just "#y" if x == y)

**For each PR in the range x..y, get source file changes (excluding baselines):**
```bash
gh pr view {PR_NUMBER} --repo Devolutions/avalonia-extensions --json number,title,files \
  --jq '"#\(.number) \(.title)\n  src: \([.files[].path | select(startswith("src/") or startswith("samples/"))] | join(", "))"'
```

Store this data — you'll use it in Step 11 to attribute baseline changes to likely PRs.

---

## Step 5: Ensure we're on master with latest changes

```bash
git checkout master && git pull origin master
```

If there are uncommitted changes on the current branch, stop and warn the user before switching.

---

## Step 6: Create the baseline-updates branch

From the main repo (not a worktree), create the branch from the current master commit:

```bash
git checkout -b baseline-updates-{PR_NUMBER}
git push -u origin baseline-updates-{PR_NUMBER}
```

Push immediately so Windows and Linux can fetch it.

---

## Step 7: Run tests on Mac (read-only first pass)

Run from the main repo directory. Use an **async bash session** to avoid approval issues with the `UPDATE_BASELINES` env var prefix:

```bash
cd /path/to/avalonia-extensions && dotnet test
```

Capture the output. Check if all tests passed.

- **If all tests passed**: No Mac baselines need updating — skip to Step 9 (Windows).
- **If any tests failed**: Continue to Step 8.

---

## Step 8: Update baselines on Mac

Use an **async bash session** (the inline `VAR=value` env syntax is not auto-approved; `export` in an async shell avoids this):

```bash
# Start async shell, then:
cd /path/to/avalonia-extensions && export UPDATE_BASELINES=true && dotnet test
```

After this runs, check what changed:
```bash
git diff --name-only HEAD -- tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/macOS/
```

If files changed, commit them:
```bash
git add tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/macOS/
git commit -m "baseline updates (Mac)"
git push origin baseline-updates-{PR_NUMBER}
```

If no files changed (the baselines were already current despite a test failure — can happen with flaky rendering), skip the commit and continue.

---

## Step 9: Update baselines on Windows VM

Run as **separate sequential PowerShell commands** over SSH (not chained with `&&` — PowerShell doesn't support that):

```bash
ssh baseline-win "powershell -Command \"cd C:/git/avalonia-extensions; git fetch origin; git checkout baseline-updates-{PR_NUMBER}; git pull origin baseline-updates-{PR_NUMBER}\""
ssh baseline-win "powershell -Command \"Get-Process -Name dotnet,SampleApp -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep 2; cd C:/git/avalonia-extensions; \$env:UPDATE_BASELINES='true'; dotnet test; Remove-Item env:UPDATE_BASELINES 2>&1 | Select-Object -Last 5\""
ssh baseline-win "powershell -Command \"cd C:/git/avalonia-extensions; git add tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/Windows/; git commit -m 'baseline updates (Win)'; git push origin baseline-updates-{PR_NUMBER}\""
```

Key notes:
- SampleApp/Rider must be stopped first to release locked DLLs — the `Stop-Process` handles this
- `Remove-Item env:UPDATE_BASELINES` **must** run after the test — unlike Mac/Linux, Windows persists the env var in the session if not explicitly removed
- `dotnet test` takes ~3-4 minutes on Windows — wait for it
- If `git status` shows no changes, skip the commit
- The `core.sshCommand` is already configured in `~/.gitconfig` for the SSH session user to use `C:/Users/amalchowperryman.VDEVOLUTIONS533/.ssh/id_rsa_local`

If the SSH command fails:
- Verify SSH connectivity: `ssh -o ConnectTimeout=10 baseline-win "echo connected"`
- Verify git works: `ssh baseline-win "powershell -Command \"git -C C:/git/avalonia-extensions status\""`

---

## Step 10: Update baselines on Linux VM

```bash
ssh baseline-linux "cd /home/parallels/git/avalonia-extensions && git fetch origin && git checkout baseline-updates-{PR_NUMBER} && git pull origin baseline-updates-{PR_NUMBER}"
ssh baseline-linux "cd /home/parallels/git/avalonia-extensions && UPDATE_BASELINES=true dotnet test 2>&1 | tail -5"
ssh baseline-linux "cd /home/parallels/git/avalonia-extensions && git add tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/Linux/ && git commit -m 'baseline updates (Linux)' && git push origin baseline-updates-{PR_NUMBER}"
```

---

## Step 11: Open a draft PR with smart description

**Title**: `Baseline updates for changes in PR #{FIRST_PR} - #{LAST_PR}`  
(If FIRST_PR == LAST_PR, use just `Baseline updates for changes in PR #{LAST_PR}`)

**Description**: Build it from the data collected in Step 4 and the actual baseline changes across all 3 platforms. Structure:

```markdown
Automated baseline screenshot updates for PRs merged since last baseline update.

## PRs in range

| PR | Title |
|----|-------|
| #x | ... |
...

## Changed baselines

For each distinct screenshot that changed, group by demo name and note:
- Which platforms/themes it changed on
- The most likely PR cause: look for PRs that modified `src/.../{DemoName}.axaml` or a 
  related theme file (e.g. ComboBox.axaml for ComboBoxDemo.png). If no clear match, 
  note "likely rendering drift".

---
_Commits: [list only platforms that had actual changes]_
```

```bash
gh pr create \
  --draft \
  --base master \
  --head baseline-updates-{LAST_PR} \
  --title "{PR_RANGE_TITLE}" \
  --body "{BODY}"
```

Capture the PR URL from the output.

---

## Step 12: Notify completion

```bash
osascript -e $'display dialog "✅ Baseline Update Complete\n\nDraft PR created with 3 commits:\n  • baseline updates (Mac)\n  • baseline updates (Win)\n  • baseline updates (Linux)\n\n{PR_URL}" buttons {"OK"} default button "OK" with title "GitHub Copilot"' &
```

Replace `{PR_URL}` with the actual PR URL from Step 11.

Also mention in the notification if any platform had no changes (baselines already current).

---

## Error handling

| Situation | Action |
|-----------|--------|
| SSH connection refused | Tell user to start SSH server on that VM (see setup doc) |
| `git checkout` fails on VM | The branch may not be pushed yet — ensure Step 8 succeeded |
| `dotnet test` fails to run on VM | Check that .NET SDK is installed on the VM |
| No changes after `UPDATE_BASELINES` | Tests may have all passed — re-examine test output |
| Windows PowerShell path issues | Use forward slashes in git commands; PowerShell accepts both |

---

## Notes

- The `macOS/`, `Windows/`, and `Linux/` subdirectories under `Baseline/` are platform-specific. Each platform only updates its own directory.
- If a VM is offline or SSH fails, make a note in the PR description and continue with the available platforms.
- The Windows SSH command uses PowerShell by default when OpenSSH is installed on Windows 10+.
- To switch back to the previous branch after completing, check git log or ask the user which branch they want to be on.

## Known false positives / recurring patterns

| Screenshot | Pattern | Notes |
|------------|---------|-------|
| `ToggleSwitchDemo_dark` (Windows, MacClassic) | Occasional small pixel drift | Known rendering instability on Windows. Safe to update baseline. No theme change needed. |

