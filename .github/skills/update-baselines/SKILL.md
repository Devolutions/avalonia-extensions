---
name: update-baselines
description: Updates visual regression test baseline screenshots across Mac, Windows, and Linux platforms, then creates a draft PR. Use when the user asks to update baselines or types /updateBaselines.
allowed-tools: shell
---

When asked to update baselines or run the baseline update workflow, follow the complete step-by-step instructions in `.claude/commands/updateBaselines.md`.

Key facts about this repository's visual regression tests:

- Baseline screenshots are stored in `tests/Devolutions.AvaloniaControls.VisualTests/Screenshots/Baseline/`
- They are organized by platform: `macOS/`, `Windows/`, `Linux/`
- Run tests: `dotnet test`
- Update baselines (Mac/Linux): `UPDATE_BASELINES=true dotnet test`
- Update baselines (Windows PowerShell): `$env:UPDATE_BASELINES="true"; dotnet test; Remove-Item env:UPDATE_BASELINES`
- SSH aliases for VMs: `baseline-win` (Windows) and `baseline-linux` (Linux)
- VM paths and SSH config setup: `.claude/docs/processes/baseline-vm-setup.md`
