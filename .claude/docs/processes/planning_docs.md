# Project Management Practices

This is a guide for writing plans or project management `.md` files, e.g. `.claude/docs/planning/upcoming/2025_10_21__Complex_Project.md`. These are for thinking through & documenting decisions, breaking down complex projects into multiple stages, and tracking progress.

Aim to keep these concise, but emphasise & clearly capture all the decisions, responses, and requirements from the user.

If you're starting the doc from scratch, store it in `.claude/docs/planning/upcoming/`, and first ask the user questions about their project requirements to clarify key decisions. (Use MCP or run `date +"%Y_%m_%d"` command first to get the current date for naming the file)

**Planning Document Lifecycle:**
- **New docs start in `/upcoming/`** - for initial planning and requirements gathering
- **Move to `/current/`** when active development begins
- **Move to `/completed/`** when implementation is finished and delivered

see also:
- `.claude/docs/processes/evergreen_docs.md` for instructions on writing evergreen docs
- `.claude/docs/processes/planning_theme_controls.md` for specialized guidance on creating/styling control themes
- `.claude/docs/processes/planning_custom_controls.md` for specialized guidance on creating custom controls in AvaloniaControls


## File naming conventions

Planning docs should follow this naming format: `yyyy_MM_dd__Description_With_Proper_Capitalization.md`

- Date prefix: `yyyy_MM_dd` format (e.g., `2025_10_21` for 21 October 2025)
- Separator: Two underscores `__` between date and description
- Description: Use proper capitalization with underscores separating words
  - Capitalize proper nouns (e.g., Ursa, TagInput, DevExpress)
  - Capitalize first letter of each significant word
  - Examples:
    - `2025_10_21__Ursa_TagInput_theming.md`
    - `2025_05_26__ToC_Hierarchical_Summary_Tooltips.md`
    - `2025_08_15__Custom_Control_Development.md`

Update this doc regularly to keep the actions up-to-date. When you change it, make minimal, focused changes, based on new user input.

**Document Status Management:**
- Update the document location as work progresses through the lifecycle
- When moving from `/upcoming/` to `/current/`, add current date and status update
- When moving from `/current/` to `/completed/`, mark completion date and final status
- Update cross-references in other documents when moving locations


## Document structure

Don't include a `Date` section at the top since it's implicit from the filename.


### Goal, context

- Clear problem/goal statement(s) at top, plus enough context/background to pick up where we left off
- If the goal is complex, break things down in detail about the desired behaviour.


### References

- Mention relevant evergreen docs (in `.claude/docs/reference/`), other planning docs (in `.claude/docs/planning/current/`, `/upcoming/`, or `/completed/`), code files/functions, links, or anything else that could provide context, with a 1-sentence summary for each of what it's about/why it's relevant


### Principles, key decisions

- Include any specific principles/approaches or decisions that have been explicitly agreed with the user (over and above existing project rules, examples, best practices, etc).
- As you get new information from the user, update this doc so it's always up-to-date.


### Theme Architecture

**Note:** For detailed theme/control work, refer to the specialized planning documents:
- `docs/processes/planning_theme_controls.md` - for styling existing Avalonia controls
- `docs/processes/planning_custom_controls.md` - for creating new custom controls

For complex theme-related features, document:
- AXAML file structure and resource organization
- ControlTheme definitions and resource dictionary hierarchy
- Style setters, template bindings, and pseudo-class selectors
- Visual state management (normal, hover, pressed, disabled, focus, etc.)
- Color resource keys and DynamicResource vs StaticResource usage patterns
- Component architecture and how controls compose together
- Document technical alternatives considered and rationale for chosen approach


### Platform References & Design Systems

For theme work that references platform-native designs:
- Document target platform design system (AppKit for macOS, Yaru for Linux, DevExpress for WinForms)
- Include links to official design guidelines (e.g., Apple HIG, Ubuntu design docs)
- Reference existing theme code to copy/adapt from (Fluent theme or existing themed controls)
- Document Avalonia.Themes.Fluent fallback behavior expectations
- Plan ThemeRoot.axaml and GlobalStyles.axaml organization
- Consider cross-theme consistency (macOS/DevExpress/Linux themes should ideally use consistent naming)


### Theme Resources & Assets

For features requiring new theme resources:
- Document color resources (brushes, gradients) for both Light and Dark theme variants
- Plan geometry resources (paths, icons) in `Accents/Icons.axaml`
- Define dimension resources (thickness, corner radius, spacing, font sizes)
- Document resource key naming conventions to maintain consistency
- Plan organization in `Accents/ThemeResources.axaml` with ThemeDictionaries structure
- Consider which resources need DynamicResource (theme-variant-specific) vs StaticResource


### Visual Design Specification

For control styling and theming work:
- Gather reference screenshots/mockups from platform design guidelines or existing implementations
- Define all control states with visual descriptions:
  - Normal (default appearance)
  - Hover/PointerOver (mouse over state)
  - Pressed (active click/tap state)
  - Disabled (non-interactive state)
  - Focus (keyboard focus indicator)
  - Additional states (checked, selected, expanded, etc.)
- Document animations and transitions between states
- Specify spacing, padding, margins, and sizing for consistency
- Plan Design.PreviewWith sections for in-editor preview during development
- Document visual differences between Light and Dark theme variants


### Actions

- Break into numbered phases. Start with a really simple working v1, and gradually layer in complexity, ending each phase with passing tests and working code.
- List actions in the order that they should be tackled
- Use the format: `### Phase 1: Description of this phase`
- Don't renumber phases when moving them around - add new phases as needed
- Use `[ ]` and `[x]` checkboxes to indicate todo/done.
- Include subtasks with clear acceptance criteria
- Refer to specific docs, files/functions, examples, links, etc, so it's clear exactly what needs to be done
- If there are actions that the user needs to do, add those in too, so we can track progress and remind the user.
- Ask the user whether we should have an early action to create a `yyyy_MM_dd__Complex_Project` Git branch (and move over any changes). If so, then add a final action to merge that back into `main`.
- Add actions to update the planning doc with progress so far at the end of every phase
- Add actions to Git commit (perhaps at the end of every phase, perhaps use a subagent) - follow instructions in `.claude/docs/processes/git_commits.md`
- Add actions to stop & review with user where appropriate, e.g. when we get to a good stopping point, to manually check changes to the user interface, etc
- Add actions to search the web where appropriate, e.g. when debugging, determining best practices, making use of 3rd-party libraries, etc
- Add actions to update relevant `.claude/docs/reference/*.md` evergreen docs (see `.claude/docs/processes/evergreen_docs.md`).
- If you think we need a new evergreen-doc, ask the user
- Explicitly say to use subagents for encapsulated tasks or where the task will create a lot of verbose content, e.g. checking for errors or browser console output with Playwright MCP, doing research
- Add a final action to move the doc to `.claude/docs/planning/completed/` and commit.
- Ask me three clarifying questions that are not mentally taxing to help you understand the assumptions and my thinking.

Example action (no need to include the words `TODO` or `DONE` explicitly, since the `[ ]` todo-checkboxes capture that):

```
### Phase 1: High-level description of this phase
- [ ] This is a top-level action description line
  - [ ] It can have sub-points that get ticked off
    - You can add bulletpoint notes with extra detail/context to help plan & shape future actions

### Phase 2: Another major phase
- ✅ This phase has already been completed
  - ✅ This step has already been completed
    - 📔 You could journal about useful/unexpected discoveries when you update progress on completed tasks
  - ❌ This step has been skipped
```

# Appendix

Add any other important context here, including:

**Technical Details:**
- Complete AXAML resource definitions with explanations
- ControlTheme structure examples
- Resource dictionary organization patterns
- Visual state definitions and pseudo-class usage

**Implementation Context:**
- Summary of web searching and research
- Platform design guideline references
- Existing theme code analysis
- Rich background, quotes, and context from user conversations

**Decision Documentation:**
- Alternative approaches that were considered but discarded
- Technical trade-offs and rationale for chosen solutions
- Resource naming decisions for cross-theme consistency
- Visual design choices and their platform-native justifications
