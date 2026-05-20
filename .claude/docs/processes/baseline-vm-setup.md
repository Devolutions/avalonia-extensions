# Baseline VM SSH Setup

This document configures the SSH connections needed for the `/updateBaselines` command to run visual regression tests on the Windows and Linux VMs.

**Update this file** when VM details change. The `/updateBaselines` command reads the repo paths from here.

---

## SSH Aliases Required

The `/updateBaselines` command expects two SSH aliases in `~/.ssh/config`:
- `baseline-win` → Windows VM
- `baseline-linux` → Linux VM

---

## VM Configuration

### Windows VM

| Field | Value |
|-------|-------|
| SSH alias | `baseline-win` |
| Hostname/IP | `10.211.55.3` (Parallels Ethernet adapter) |
| Username | `amalchowperryman` |
| Repo path | `C:\git\avalonia-extensions` |

**Windows repo path**: `C:\git\avalonia-extensions`

### Linux VM

| Field | Value |
|-------|-------|
| SSH alias | `baseline-linux` |
| Hostname/IP | `10.211.55.4` (Parallels Ethernet adapter) |
| Username | `parallels` |
| Repo path | `/home/parallels/git/avalonia-extensions` |

**Linux repo path**: `/home/parallels/git/avalonia-extensions`

---

## Setup Steps

### 1. Set up SSH keys (if not already done)

If you don't have an SSH key yet:
```bash
ssh-keygen -t ed25519 -C "baseline-updates" -f ~/.ssh/id_baseline
```

Copy the public key to each VM:
```bash
# For Linux VM:
ssh-copy-id -i ~/.ssh/id_baseline.pub username@linux-vm-host

# For Windows VM (run from PowerShell on the Windows VM):
# Add the contents of ~/.ssh/id_baseline.pub to C:\Users\username\.ssh\authorized_keys
```

### 2. Configure `~/.ssh/config`

Add entries like these to `~/.ssh/config` (create the file if it doesn't exist):

```
Host baseline-win
    HostName 10.211.55.3
    User amalchowperryman
    IdentityFile ~/.ssh/id_rsa

Host baseline-linux
    HostName 10.211.55.4
    User parallels
    IdentityFile ~/.ssh/id_rsa
```

✅ These entries have already been added to `~/.ssh/config`.

### 3. Enable SSH server on each VM

**Windows (OpenSSH):**
- Run in PowerShell as Administrator:
  ```powershell
  Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
  Start-Service sshd
  Set-Service -Name sshd -StartupType Automatic
  ```
- Windows Firewall rule is created automatically.

**Linux:**
- Usually SSH is already running. If not:
  ```bash
  sudo systemctl enable --now ssh
  ```

### 4. Update the repo paths in this file

Replace the `PLACEHOLDER_*` values above with the actual paths where the `avalonia-extensions` repo is cloned on each VM.

### 5. Test connectivity

```bash
ssh baseline-win "echo 'Windows VM connected'"
ssh baseline-linux "echo 'Linux VM connected'"
```

### 6. Test a git command on each VM

```bash
ssh baseline-win "cd 'C:\\path\\to\\repo' && git status"
ssh baseline-linux "cd /path/to/repo && git status"
```

### 7. Verify .NET is available on each VM

```bash
ssh baseline-win "dotnet --version"
ssh baseline-linux "dotnet --version"
```

---

## Quick Connectivity Check

Run this to verify everything is ready before using `/updateBaselines`:

```bash
echo "Testing Windows VM..." && ssh baseline-win "echo OK" && \
echo "Testing Linux VM..." && ssh baseline-linux "echo OK" && \
echo "All VMs reachable ✅"
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `Connection refused` | SSH server is not running — start it (see Step 3 above) |
| `Permission denied` | SSH key not authorized — re-run `ssh-copy-id` or check `authorized_keys` |
| `Host not found` | VM hostname/IP changed — update `~/.ssh/config` |
| `Operation timed out` | **Most likely: WARP is connected on the VM.** Disconnect WARP before running `/updateBaselines`. The Parallels subnet (`10.211.55.0/24`) is not in the managed WARP exclusion list and WARP will silently block inbound SSH. |
| `git: command not found` on VM | Git not on PATH for SSH sessions; try full path `/usr/bin/git` or configure `.bashrc` |
| `git: detected dubious ownership` on Windows | Run: `git config --global --add safe.directory C:/git/avalonia-extensions` (one-time fix) |
| `unable to resolve user` on Windows | The local Windows account is disabled. Run: `net user amalchowperryman /active:yes` |
| Windows PowerShell not default shell | Set PowerShell as default: `New-ItemProperty HKLM:\SOFTWARE\OpenSSH -Name DefaultShell -Value "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -PropertyType String -Force` |
