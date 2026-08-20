---
name: avalonia-devtools-mcp
description: "Use Avalonia DevTools MCP to inspect and interact with running Avalonia apps (attach, tree, search, props, styles, screenshots, input) and handle known license/auth pitfalls."
---

# Avalonia DevTools MCP

Use this skill when the task involves validating or debugging runtime UI behavior in a running Avalonia app, especially when visual inspection, tree traversal, property/style inspection, or screenshots are needed.

## When to use

- "Show me the visual tree"
- "Find this control in the running app"
- "What styles/properties are applied at runtime?"
- "Take a screenshot of this panel/window"
- "Simulate click/text input and verify result"

## Preconditions

1. DevTools MCP server configured in user MCP config (for VS Code in this environment):
   - `~/Library/Application Support/Code/User/mcp.json`
2. Tool installed:
   - `dotnet tool install --global AvaloniaUI.DeveloperTools`
3. App instrumentation in target app:
   - `AvaloniaUI.DiagnosticsSupport` package
   - `.WithDeveloperTools()` or `this.AttachDeveloperTools()` at startup

## Standard workflow

1. `attach-to-app` with no `id`
2. If multiple/any clients returned, select one and call `attach-to-app` with that `id`
3. Inspect with:
   - `tree` (roots/subtree)
   - `search` (type/x:Name)
   - `props`, `styles`, `resources`
   - `screenshot`
4. Interact with:
   - `input`, `action`, `set-prop`, `pseudo-class`
5. `detach` when done

## Important pitfalls

- Do **not** tell users to press F12 for MCP connectivity. F12 opens standalone tools and does not establish MCP attach by itself.
- `attach-to-app` commonly returns an app list first; a second call with selected `id` is expected.
- Node IDs are ephemeral; reacquire via `tree/search` after UI changes.

## License/auth caveat observed in this repo

In this environment, forcing `AVALONIA_TOOLS_LICENSE_KEY` through MCP server `env` could produce:

`Authenticated session does not have access to required product: avalonia-developer-tools-console`

Working behavior was restored by **removing** explicit MCP `env` override and relying on an already-authenticated DevTools session.

If license/product errors appear:

1. Remove explicit MCP `env` override for `AVALONIA_TOOLS_LICENSE_KEY`
2. Ensure `AvaloniaUI.DeveloperTools` is current
3. Retry attach
4. If prompted, complete Avalonia portal sign-in dialog (auth may be cached for later sessions)
