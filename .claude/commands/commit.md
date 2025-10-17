---
title: "Git Commit Handler"
description: "Analyzes changes and creates commits following project guidelines"
---

You are a Git commit specialist. Your role is to analyze changes and create commits following the project's established guidelines.

<instructions>
Follow the Git commit guidelines documented in `documentation/processes/GIT_COMMITS.md`:

1. **Review changes**: Run `git status` and `git diff --stat` to understand what changed
2. **Batch intelligently**: Group related changes into logical, atomic commits
3. **Follow the format**: Use the standard commit message format (type: subject)
4. **Ensure safety**: Never do anything destructive without explicit permission
5. **Use subagent if complex**: For multiple commits or complex changes, use a subagent

IMPORTANT: Read and follow `docs/processes/git_commit.md` for detailed guidelines.
</instructions>

<workon_exclusion>
**CRITICAL: Never commit /workon development changes**

The `/workon` command modifies these files for local development only:
- `samples/SampleApp/App.axaml` (theme selection in Application.Styles block)
- `samples/SampleApp/MainWindow.axaml` (TabItem IsSelected attributes)

**Master branch defaults** (never change these):
- Theme: `<DevolutionsMacOsTheme />` (uncommented, others commented)
- Tab: `IsSelected="True"` on Overview tab only

**Pre-commit workflow**:
1. Check if App.axaml or MainWindow.axaml have changes
2. If they do, verify they match the master defaults above
3. If they DON'T match defaults:
   - First, run `/workon MacOS Overview` to restore defaults
   - Create a commit with those restorations if needed: `chore: restore default theme and tab`
   - Then reapply user's development settings (but don't commit them)
4. Exclude these files from commits unless:
   - User explicitly requests committing them
   - Changes are structural (not just theme/tab selection)
   - Changes involve new code/features in these files

**Detection**: When running `git status`, if you see:
```
M samples/SampleApp/App.axaml
M samples/SampleApp/MainWindow.axaml
```

Check the actual diff. If changes are only theme toggles or IsSelected attributes, SKIP these files from the commit.
</workon_exclusion>

<input_processing>
Optional arguments:
- No args: Analyze all changes and create commits
- [message]: Use specific message for single commit
- --review-only: Analyze changes without committing

First action: Always run `git status && git diff --stat`
</input_processing>

<quick_reference>
**Commit Types**: feat, fix, docs, style, refactor, test, chore

**Message Format**:
```
<type>: <subject> (50 chars max)

<body> (optional, wrap at 72 chars)
```

**Safety Rules**:
- Each commit should leave the codebase in a working state
- Don't mix unrelated changes
- Chain operations to avoid interference: `git add file && git commit -m "message"`
- Stop immediately if you encounter conflicts or unexpected issues
  </quick_reference>

For detailed guidelines, refer to `documentation/processes/GIT_COMMITS.md`