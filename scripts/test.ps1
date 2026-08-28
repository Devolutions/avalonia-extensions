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

function Resolve-PresetFilter {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Preset
    )

    switch ($Preset.ToLowerInvariant()) {
        "visual" { return "" }
        "nonvisual" { return "" }
        "non-visual" { return "" }
        "functional" { return "" }
        "all" { return "" }
        default { return $null }
    }
}

function Resolve-PresetProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Preset
    )

    switch ($Preset.ToLowerInvariant()) {
        "visual" { return "tests/Devolutions.AvaloniaControls.VisualTests/Devolutions.AvaloniaControls.VisualTests.csproj" }
        "nonvisual" { return "tests/Devolutions.AvaloniaControls.Tests/Devolutions.AvaloniaControls.Tests.csproj" }
        "non-visual" { return "tests/Devolutions.AvaloniaControls.Tests/Devolutions.AvaloniaControls.Tests.csproj" }
        "functional" { return "tests/Devolutions.AvaloniaControls.Tests/Devolutions.AvaloniaControls.Tests.csproj" }
        default { return $null }
    }
}

$dotnetArgs = [System.Collections.Generic.List[string]]::new()
$hasLoggerArg = $false
$hasFilterArg = $false

$presetFilter = $null
$presetToken = $null
$presetProject = $null
$updateBaselines = $false
if ($args.Count -gt 0 -and $args[0] -eq "--update-baselines") {
    $updateBaselines = $true
    $args = if ($args.Count -gt 1) { $args[1..($args.Count - 1)] } else { @() }
}
if ($args.Count -gt 0) {
    $resolvedPresetFilter = Resolve-PresetFilter -Preset $args[0]
    if ($null -ne $resolvedPresetFilter) {
        $presetFilter = $resolvedPresetFilter
        $presetToken = $args[0]
        $presetProject = Resolve-PresetProject -Preset $presetToken
        if ($args.Count -gt 1) {
            $args = $args[1..($args.Count - 1)]
        }
        else {
            $args = @()
        }
    }
}

for ($index = 0; $index -lt $args.Count; ) {
    $current = $args[$index]

    if ($current -eq "--update-baselines") {
        $updateBaselines = $true
        $index += 1
        continue
    }

    if ($current -eq "--filter") {
        $hasFilterArg = $true
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
        $hasFilterArg = $true
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

if ($hasFilterArg -and ($null -ne $presetProject -or -not [string]::IsNullOrEmpty($presetFilter))) {
    throw "Cannot combine preset '$presetToken' with an explicit --filter. Use one or the other."
}

if (-not [string]::IsNullOrEmpty($presetFilter)) {
    $dotnetArgs.Add("--filter")
    $dotnetArgs.Add($presetFilter)
}

if (-not [string]::IsNullOrEmpty($presetProject)) {
    $dotnetArgs.Insert(0, $presetProject)
}

if (-not $hasLoggerArg) {
    $dotnetArgs.Add("--logger")
    $dotnetArgs.Add("console;verbosity=normal")
}

$summaryRows = [System.Collections.Generic.HashSet[string]]::new()
$fallbackLines = [System.Collections.Generic.List[string]]::new()
$functionalFailureRows = [System.Collections.Generic.List[string]]::new()
$resultLine = ""
$progressSeen = $false
$lastProgressTest = $null
$lastProgressStatus = $null
$testRunTarget = ""
$testRunStatus = ""
$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0
$totalDuration = ""
$currentFailedTest = $null
$currentFailureMessage = ""
$inErrorMessageBlock = $false

$previousUpdateBaselines = [Environment]::GetEnvironmentVariable("UPDATE_BASELINES", "Process")
if ($updateBaselines) {
    [Environment]::SetEnvironmentVariable("UPDATE_BASELINES", "true", "Process")
}

try {
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

    if ($normalized -match '^(Passed|Failed|Skipped)\s+(.+)\[[^\]]+\]$') {
        $status = $Matches[1]
        $testName = $Matches[2].TrimEnd()

        if ($null -ne $lastProgressTest -and $lastProgressTest -eq $testName -and $lastProgressStatus -eq $status) {
            $lastProgressTest = $null
            $lastProgressStatus = $null
            return
        }

        if (-not $progressSeen) {
            Write-Host -NoNewline "Progress: "
            $progressSeen = $true
        }

        switch ($status) {
            "Passed" { Write-Host -NoNewline "✅" }
            "Failed" { Write-Host -NoNewline "❌" }
            "Skipped" { Write-Host -NoNewline "s" }
        }
        $lastProgressTest = $testName
        $lastProgressStatus = $status

        if ($status -eq "Failed") {
            if ($testName -match 'VisualRegressionTests') {
                $currentFailedTest = $null
            }
            else {
                $currentFailedTest = $testName
            }
            $currentFailureMessage = ""
            $inErrorMessageBlock = $false
        }
        return
    }

    if ($normalized -match '^Error Message:$') {
        $inErrorMessageBlock = $true
        return
    }

    if ($inErrorMessageBlock) {
        if ($normalized -match '^Stack Trace:$') {
            if (-not [string]::IsNullOrWhiteSpace($currentFailedTest) -and -not [string]::IsNullOrWhiteSpace($currentFailureMessage)) {
                $functionalFailureRows.Add("$currentFailedTest`t$currentFailureMessage")
            }
            $currentFailedTest = $null
            $currentFailureMessage = ""
            $inErrorMessageBlock = $false
            return
        }

        $trimmed = $normalized.Trim()
        if (-not [string]::IsNullOrWhiteSpace($trimmed) -and -not $trimmed.StartsWith('at ')) {
            if ([string]::IsNullOrWhiteSpace($currentFailureMessage)) {
                $currentFailureMessage = $trimmed
            }
            else {
                $currentFailureMessage = "$currentFailureMessage | $trimmed"
            }
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
}
finally {
    if ($updateBaselines) {
        [Environment]::SetEnvironmentVariable("UPDATE_BASELINES", $previousUpdateBaselines, "Process")
    }
}

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

if ($functionalFailureRows.Count -gt 0) {
    Write-Host ""
    Write-Host "________________________________________________________________________________"
    Write-Host "Functional test failures" -ForegroundColor White

    foreach ($row in $functionalFailureRows) {
        $parts = $row -split "`t", 2
        Write-Host ("• {0}" -f $parts[0]) -ForegroundColor White
        Write-Host ("  {0}" -f $parts[1])
    }

    Write-Host "________________________________________________________________________________"
    Write-Host ""
}

exit $dotnetExitCode
