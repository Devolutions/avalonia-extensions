#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

function Normalize-FilterExpression {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Expression
    )

    if ($Expression -match '[~=!<>\|&()]') {
        return $Expression
    }

    return "DisplayName~$Expression"
}

$dotnetArgs = [System.Collections.Generic.List[string]]::new()
for ($index = 0; $index -lt $args.Count; ) {
    $current = $args[$index]

    if ($current -eq "--filter") {
        if ($index + 1 -ge $args.Count) {
            $dotnetArgs.Add($current)
            $index += 1
            continue
        }

        $dotnetArgs.Add("--filter")
        $dotnetArgs.Add((Normalize-FilterExpression -Expression $args[$index + 1]))
        $index += 2
        continue
    }

    if ($current.StartsWith("--filter=")) {
        $dotnetArgs.Add("--filter=$(Normalize-FilterExpression -Expression $current.Substring(9))")
        $index += 1
        continue
    }

    $dotnetArgs.Add($current)
    $index += 1
}

$summaryRows = [System.Collections.Generic.HashSet[string]]::new()
$fallbackLines = [System.Collections.Generic.List[string]]::new()
$resultLine = ""
$progressSeen = $false

& dotnet test @dotnetArgs 2>&1 | ForEach-Object {
    $line = "$_"
    $normalized = $line

    if ($normalized -match '^\[xUnit\.net\s+[^\]]+\]\s*(.*)$') {
        $normalized = $Matches[1]
    }

    $normalized = $normalized.TrimStart()

    if ($normalized -match '^Visual regression detected for \[([^\]]+)\] (.*) - ([^.]+)\.(?: DesiredH=([0-9]+(?:\.[0-9]+)?)\.)? Diff saved to (.*)$') {
        $cappedHeight = if ($Matches.Count -gt 4) { $Matches[4] } else { "" }
        $row = "Visual regression`t$($Matches[1])`t$($Matches[2])`t$($Matches[3])`t$($Matches[5])`t$cappedHeight"
        [void]$summaryRows.Add($row)
        return
    }

    if ($normalized -match '^No baseline found for \[([^\]]+)\] (.*) - ([^.]+)\.(?: DesiredH=([0-9]+(?:\.[0-9]+)?)\.)? Saved screenshot to (.*)$') {
        $cappedHeight = if ($Matches.Count -gt 4) { $Matches[4] } else { "" }
        $row = "No baseline`t$($Matches[1])`t$($Matches[2])`t$($Matches[3])`t$($Matches[5])`t$cappedHeight"
        [void]$summaryRows.Add($row)
        return
    }

    if ($normalized -match '\[(FAIL|PASS|SKIP)\]$') {
        if (-not $progressSeen) {
            Write-Host -NoNewline "Progress: "
            $progressSeen = $true
        }

        Write-Host -NoNewline ">"
        return
    }

    if ($normalized -match '^(Failed!|Passed!)\s+-\s+Failed:\s+\d+,\s+Passed:\s+\d+,\s+Skipped:\s+\d+,\s+Total:\s+\d+,') {
        $resultLine = $normalized
        return
    }

    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return
    }

    if ($normalized -match '^(Determining projects to restore|All projects are up-to-date for restore|Test run for |VSTest version|Starting test execution, please wait|A total of \d+ test files matched the specified pattern\.)') {
        return
    }

    if ($normalized -match '^at\s+') {
        return
    }

    if ($normalized -match '^(Stack Trace:|Error Message:)$') {
        return
    }

    if ($normalized -match '^[A-Za-z0-9._-]+\s+->\s+') {
        return
    }

    if ($normalized -match '^/.*:\d+:\d+:\s+warning\s+') {
        return
    }

    $fallbackLines.Add($line)
}

$dotnetExitCode = $LASTEXITCODE

if ($progressSeen) {
    Write-Host ""
}

if (-not [string]::IsNullOrWhiteSpace($resultLine)) {
    Write-Host $resultLine
}
elseif ($fallbackLines.Count -gt 0) {
    $fallbackLines | ForEach-Object { Write-Host $_ }
}

if ($summaryRows.Count -gt 0) {
    Write-Host ""
    Write-Host "________________________________________________________________________________"
    Write-Host "Visual regression summary" -ForegroundColor Yellow
    Write-Host ("{0,-18} {1,-14} {2,-34} {3,-10} {4,-8} {5}" -f "Status", "Theme", "Page", "Variant", "DesiredH", "Path") -ForegroundColor Yellow

    foreach ($row in $summaryRows) {
        $parts = $row -split "`t", 6
        Write-Host ("{0,-18} {1,-14} {2,-34} {3,-10} {4,-8} {5}" -f $parts[0], "[$($parts[1])]", $parts[2], $parts[3], $parts[5], $parts[4]) -ForegroundColor Yellow
    }

    Write-Host "________________________________________________________________________________"
    Write-Host ""
}

exit $dotnetExitCode
