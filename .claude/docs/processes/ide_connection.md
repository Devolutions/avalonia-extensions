# IDE Connection Guide for Claude Code Commands

This document explains how Claude Code commands that rely on IDE integration work, and how to troubleshoot connection issues.

## How IDE Integration Works

Commands like `/explain` and `/simplify` rely on IDE integration to see code that you've selected in your IDE (JetBrains Rider, VS Code, etc.). When you select code and run these commands, Claude receives the selection automatically via system reminders.

## The Three Connection States

### 1. Connected with Selection
**What Claude sees:**
- System reminder about the file being opened
- System reminder with selected line numbers and actual code content

**What happens:**
- Claude proceeds with the command immediately

### 2. Connected without Selection
**What Claude sees:**
- System reminder about the file being opened
- No system reminder with selected code

**What happens:**
- Claude responds: "I can see you have `{filename}` open, but I don't see any code selected. Please select the code and run the command again."

### 3. No Connection
**What Claude sees:**
- No system reminders about files or selections at all

**What happens:**
- Claude provides the full troubleshooting steps below

## Troubleshooting: Setting Up the Connection

If Claude reports no connection, follow these steps:

1. **Start Claude Code plugin in your IDE** (Rider/VS Code)
   - Ensure the plugin is installed and enabled

2. **Exit Claude in the IDE's terminal**: Type `exit` or press Ctrl+D in the IDE's Claude terminal
   - DO NOT close the Claude Code tab (you can minimize it)
   - The plugin must remain running, but Claude itself must exit
   - This allows the external terminal to connect

3. **Enable "Auto-connect to IDE (external terminal)"**:
   - Type `/status` in your external Claude Code CLI
   - Press Tab to navigate to 'Config' section
   - Verify "Auto-connect to IDE (external terminal)" is enabled

4. **Select code in your IDE** before running the command

5. **Restart if needed**: If still not working, restart your IDE or the Claude Code CLI

## Key Points

- The IDE plugin must be **running** but Claude must **exit** from the IDE terminal
- The connection only works from an **external terminal** to the IDE
- You can **minimize** the Claude Code tab in the IDE, but don't close it
- Selection must be active when you run the command
