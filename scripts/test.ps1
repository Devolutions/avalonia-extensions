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
$hasLoggerArg = $false
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

    if ($current -eq "--logger" -or $current -eq "-l") {
        $hasLoggerArg = $true
        $dotnetArgs.Add($current)

        if ($index + 1 -lt $args.Count) {
            $dotnetArgs.Add($args[$index + 1])
            $index += 2
        }
        else {
            $index += 1
        }

        continue
    }

    if ($current.StartsWith("--logger=") -or $current.StartsWith("-l:")) {
        $hasLoggerArg = $true
        $dotnetArgs.Add($current)
        $index += 1
        continue
    }

    $dotnetArgs.Add($current)
    $index += 1
}

if (-not $hasLoggerArg) {
    $dotnetArgs.Add("--logger")
    $dotnetArgs.Add("console;verbosity=normal")
}

$summaryRows = [System.Collections.Generic.HashSet[string]]::new()
$fallbackLines = [System.Collections.Generic.List[string]]::new()
$resultLine = ""
$progressSeen = $false
$testRunTarget = ""
$testRunStatus = ""
$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0
$totalDuration = ""

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

    if ($normalized -match '^(Passed|Failed|Skipped)\s+.*\[[^\]]+\]$') {
        if (-not $progressSeen) {
            Write-Host -NoNewline "Progress: "
            $progressSeen = $true
        }

        switch ($Matches[1]) {
            "Passed" { Write-Host -NoNewline "✅" }
            "Failed" { Write-Host -NoNewline "❌" }
            "Skipped" { Write-Host -NoNewline "s" }
        }
        return
    }

    if ($normalized -match '\[(FAIL|PASS|SKIP)\]$') {
        if (-not $progressSeen) {
            Write-Host -NoNewline "Progress: "
            $progressSeen = $true
        }

        switch ($Matches[1]) {
            "PASS" { Write-Host -NoNewline "✅" }
            "FAIL" { Write-Host -NoNewline "❌" }
            "SKIP" { Write-Host -NoNewline "s" }
        }
        return
    }

    if ($normalized -match '^(Failed!|Passed!)\s+-\s+Failed:\s+\d+,\s+Passed:\s+\d+,\s+Skipped:\s+\d+,\s+Total:\s+\d+,') {
        $resultLine = $normalized
        return
    }

    if ($normalized -match '^Test run for (.+) \((.+)\)$') {
        $testRunTarget = "$($Matches[1]) $($Matches[2])"
        return
    }

    if ($normalized -match '^Test Run (Successful|Failed)\.$') {
        $testRunStatus = $Matches[1]
        return
    }

    if ($normalized -match '^Total tests:\s+(\d+)$') {
        $totalTests = [int]$Matches[1]
        return
    }

    if ($normalized -match '^Passed:\s+(\d+)$') {
        $passedTests = [int]$Matches[1]
        return
    }

    if ($normalized -match '^Failed:\s+(\d+)$') {
        $failedTests = [int]$Matches[1]
        return
    }

    if ($normalized -match '^Skipped:\s+(\d+)$') {
        $skippedTests = [int]$Matches[1]
        return
    }

    if ($normalized -match '^Total time:\s+(.+)$') {
        $totalDuration = $Matches[1]
        return
    }

    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return
    }

    if ($normalized -match '^(Determining projects to restore|All projects are up-to-date for restore|Test run for |VSTest version|Starting test execution, please wait|A total of \d+ test files matched the specified pattern\.)') {
        return
    }

    if ($normalized -match '^\[xUnit\.net\s+') {
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

    if ($normalized -match 'warning\s+[A-Z]{2,}\d+:') {
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
elseif (-not [string]::IsNullOrWhiteSpace($testRunStatus) -and -not [string]::IsNullOrWhiteSpace($totalDuration)) {
    $statusLabel = if ($testRunStatus -eq "Successful") { "Passed!" } else { "Failed!" }
    $target = if (-not [string]::IsNullOrWhiteSpace($testRunTarget)) { $testRunTarget } else { "dotnet test" }
    Write-Host ("{0}  - Failed: {1,5}, Passed: {2,5}, Skipped: {3,5}, Total: {4,5}, Duration: {5} - {6}" -f $statusLabel, $failedTests, $passedTests, $skippedTests, $totalTests, $totalDuration, $target)
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
