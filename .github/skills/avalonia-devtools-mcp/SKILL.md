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
   - Keep this instrumentation development-only (Debug), since MCP enables live inspection, mutation, and input simulation.

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

## Multiple instances and parallel agents

Assume other developers or agents may be running the same Avalonia app from other worktrees. It is
safe for separate agents to inspect separate instances, but each agent must select its client by
process ID instead of attaching to whichever app happens to appear first.

1. When starting an app, record the new process ID and its worktree/output path. On Windows,
   `Get-CimInstance Win32_Process` can correlate a `dotnet` PID with its command line; the window
   title alone is not unique.
2. Call `attach-to-app` without an `id` only to enumerate `availableClients`.
3. Match the recorded PID to `availableClients[].processId`, then call `attach-to-app` with that
   exact `id`.
4. Verify the response's `connectedClient.process.processId` and `appBaseDirectory`. The latter
   should point into the expected worktree/output directory.
5. After attaching or switching clients, discard every cached node ID and reacquire the tree/search
   results. One MCP connection targets one client at a time, and a new attach invalidates nodes from
   the previous client.
6. If the app restarts, enumerate again: process IDs and node IDs are ephemeral.

Two simultaneously running SampleApp processes were verified to appear as two distinct
`availableClients` entries, and attaching by each `processId` selected the expected process. This is
the required workflow whenever parallel work is possible.

Parallel-work safety:

- Never stop processes by name. Stop only the specific PID that this agent launched.
- Do not kill another SampleApp merely because it locks the standard build output. Build/test to an
  isolated `OutputPath` instead.
- Do not reuse a PID from an earlier run without re-enumerating clients; the process may have exited
  or been replaced.

## Important pitfalls

- Do **not** tell users to press F12 for MCP connectivity. F12 opens standalone tools and does not establish MCP attach by itself.
- `attach-to-app` commonly returns an app list first; a second call with selected `id` is expected.
- Never select the first returned client merely because it is first; identify it by process ID.
- Node IDs are ephemeral; reacquire via `tree/search` after UI changes.

## License/auth caveat observed in this repo

In this environment, forcing `AVALONIA_TOOLS_LICENSE_KEY` through MCP server `env` could produce:

`Authenticated session does not have access to required product: avalonia-developer-tools-console`

Working behavior was restored by **removing** explicit MCP `env` override and relying on an already-authenticated DevTools session.

If license/product errors appear:

1. Ensure `AvaloniaUI.DeveloperTools` is current:
   - `dotnet tool update --global AvaloniaUI.DeveloperTools`
   - If not installed yet: `dotnet tool install --global AvaloniaUI.DeveloperTools`
2. Verify license key and entitlement for DevTools MCP.
3. For the exact observed error above (and when a cached authenticated session is available), try removing explicit MCP `env` override for `AVALONIA_TOOLS_LICENSE_KEY`, then retry attach.
4. If a portal sign-in dialog appears, ask the user to complete it (do not enter credentials from the agent).
