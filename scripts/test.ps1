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

function ConvertTo-NativeProcessArgument {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Argument
    )

    if ($Argument.Length -gt 0 -and $Argument -notmatch '[\s"]') {
        return $Argument
    }

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append('"')
    $backslashCount = 0

    foreach ($character in $Argument.ToCharArray()) {
        if ($character -eq '\') {
            $backslashCount += 1
            continue
        }

        if ($character -eq '"') {
            [void]$builder.Append(('\' * ($backslashCount * 2 + 1)))
            [void]$builder.Append('"')
            $backslashCount = 0
            continue
        }

        if ($backslashCount -gt 0) {
            [void]$builder.Append(('\' * $backslashCount))
            $backslashCount = 0
        }
        [void]$builder.Append($character)
    }

    if ($backslashCount -gt 0) {
        [void]$builder.Append(('\' * ($backslashCount * 2)))
    }
    [void]$builder.Append('"')
    return $builder.ToString()
}

function New-DotnetTestProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = "dotnet"
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true

    $nativeArguments = @("test") + $Arguments
    if ($startInfo.PSObject.Properties.Name -contains "ArgumentList") {
        foreach ($argument in $nativeArguments) {
            $startInfo.ArgumentList.Add($argument)
        }
    }
    else {
        $startInfo.Arguments = ($nativeArguments | ForEach-Object { ConvertTo-NativeProcessArgument -Argument $_ }) -join " "
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    return $process
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
$resultLines = [System.Collections.Generic.List[string]]::new()
$projectTotals = [System.Collections.Generic.List[hashtable]]::new()
$currentProjectTotals = $null
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
$progressCount = 0
$progressPassed = 0
$progressFailed = 0
$progressSkipped = 0
$progressBarWidth = 30
$progressNumberWidth = 6
$progressTotal = "?"
$discoveredTestCount = 0
$initializationDoneText = "Initializing test run... done!"
$flowerFrames = @("✻", "✽", "✶", "✳", "✢")
$flowerIndex = 0
$flowerColumn = $initializationDoneText.Length + 2
$flowerTick = "$([char]30)devtest-flower-tick"
$escape = [char]27
$cursorHidden = $false
$dotnetExitCode = 1

function Write-TestProgress {
    $completed = 0
    if ($script:discoveredTestCount -gt 0) {
        $completed = [Math]::Floor($script:progressCount * $script:progressBarWidth / $script:discoveredTestCount)
    }
    $remaining = $script:progressBarWidth - $completed
    $currentText = $script:progressCount.ToString().PadLeft($script:progressNumberWidth)
    $totalText = $script:progressTotal.PadRight($script:progressNumberWidth)
    $bar = ("#" * $completed) + ("-" * $remaining)
    $passedText = $script:progressPassed.ToString().PadLeft(4)
    $failedText = $script:progressFailed.ToString().PadLeft(3)
    $skippedText = $script:progressSkipped.ToString().PadLeft(3)

    Write-Host -NoNewline ("{0}[1GProgress: {1}/{2} [{3}] ok:{4} - fail:{5} - skip:{6}" -f $script:escape, $currentText, $totalText, $bar, $passedText, $failedText, $skippedText)
}

function Write-FlowerFrame {
    $script:flowerIndex = ($script:flowerIndex + 1) % $script:flowerFrames.Count
    Write-Host -NoNewline ("{0}[1A{0}[{1}G{2}{0}[1B" -f $script:escape, $script:flowerColumn, $script:flowerFrames[$script:flowerIndex])
}

$previousUpdateBaselines = [Environment]::GetEnvironmentVariable("UPDATE_BASELINES", "Process")
if ($updateBaselines) {
    [Environment]::SetEnvironmentVariable("UPDATE_BASELINES", "true", "Process")
}

try {
Write-Host -NoNewline ("{0}[?25l" -f $escape)
$cursorHidden = $true
$spinnerFrames = @("⠋", "⠙", "⠸", "⠴", "⠦", "⠇")
$spinnerIndex = 0
Write-Host "Initializing test run... $($spinnerFrames[$spinnerIndex])"

$discoveryArgs = @($dotnetArgs.ToArray()) + @("--list-tests")
$discoveryProcess = New-DotnetTestProcess -Arguments $discoveryArgs
[void]$discoveryProcess.Start()
$discoveryOutputTask = $discoveryProcess.StandardOutput.ReadToEndAsync()
$discoveryErrorTask = $discoveryProcess.StandardError.ReadToEndAsync()

while (-not $discoveryProcess.WaitForExit(100)) {
    $spinnerIndex = ($spinnerIndex + 1) % $spinnerFrames.Count
    Write-Host -NoNewline ("{0}[1A{0}[1GInitializing test run... {1}{0}[K{0}[1B{0}[1G" -f $escape, $spinnerFrames[$spinnerIndex])
}
$discoveryProcess.WaitForExit()
$discoveryOutput = $discoveryOutputTask.GetAwaiter().GetResult() + "`n" + $discoveryErrorTask.GetAwaiter().GetResult()
$discoveredTestCount = [regex]::Matches($discoveryOutput, '(?m)^    \S').Count
Write-Host -NoNewline ("{0}[1A{0}[1G{0}[2K{1} {2}{0}[1B{0}[1G" -f $escape, $initializationDoneText, $flowerFrames[$flowerIndex])

if ($discoveredTestCount -gt 0) {
    $progressTotal = $discoveredTestCount.ToString()
    $progressNumberWidth = $progressTotal.Length
}
$progressSeen = $true
Write-TestProgress

$testProcess = New-DotnetTestProcess -Arguments $dotnetArgs.ToArray()
[void]$testProcess.Start()
$stdoutRead = $testProcess.StandardOutput.ReadLineAsync()
$stderrRead = $testProcess.StandardError.ReadLineAsync()
$stdoutClosed = $false
$stderrClosed = $false
$nextFlowerTick = [DateTime]::UtcNow.AddMilliseconds(350)

& {
    while (-not $testProcess.HasExited -or -not $stdoutClosed -or -not $stderrClosed) {
        $emittedEvent = $false

        if (-not $stdoutClosed -and $stdoutRead.IsCompleted) {
            $stdoutLine = $stdoutRead.GetAwaiter().GetResult()
            if ($null -eq $stdoutLine) {
                $stdoutClosed = $true
            }
            else {
                Write-Output $stdoutLine
                $stdoutRead = $testProcess.StandardOutput.ReadLineAsync()
            }
            $emittedEvent = $true
        }

        if (-not $stderrClosed -and $stderrRead.IsCompleted) {
            $stderrLine = $stderrRead.GetAwaiter().GetResult()
            if ($null -eq $stderrLine) {
                $stderrClosed = $true
            }
            else {
                Write-Output $stderrLine
                $stderrRead = $testProcess.StandardError.ReadLineAsync()
            }
            $emittedEvent = $true
        }

        $now = [DateTime]::UtcNow
        if ($now -ge $nextFlowerTick -and -not $testProcess.HasExited) {
            Write-Output -NoEnumerate $flowerTick
            $nextFlowerTick = $now.AddMilliseconds(350)
            $emittedEvent = $true
        }

        if (-not $emittedEvent) {
            Start-Sleep -Milliseconds 10
        }
    }
} | ForEach-Object {
    if ($_ -eq $flowerTick) {
        Write-FlowerFrame
        return
    }

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

        $progressCount += 1
        switch ($status) {
            "Passed" { $progressPassed += 1 }
            "Failed" { $progressFailed += 1 }
            "Skipped" { $progressSkipped += 1 }
        }
        Write-TestProgress
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
        $resultLines.Add($normalized)
        return
    }

    if ($normalized -match '^Test run for (.+) \((.+)\)$') {
        $testRunTarget = "$($Matches[1]) $($Matches[2])"
        return
    }

    if ($normalized -match '^Test Run (Successful|Failed)\.$') {
        $testRunStatus = $Matches[1]
        $currentProjectTotals = @{ Status = $Matches[1]; Total = 0; Passed = 0; Failed = 0; Skipped = 0; Duration = "" }
        return
    }

    if ($normalized -match '^Total tests:\s+(\d+)$') {
        $totalTests = [int]$Matches[1]
        if ($null -ne $currentProjectTotals) { $currentProjectTotals.Total = $totalTests }
        return
    }

    if ($normalized -match '^Passed:\s+(\d+)$') {
        $passedTests = [int]$Matches[1]
        if ($null -ne $currentProjectTotals) { $currentProjectTotals.Passed = $passedTests }
        return
    }

    if ($normalized -match '^Failed:\s+(\d+)$') {
        $failedTests = [int]$Matches[1]
        if ($null -ne $currentProjectTotals) { $currentProjectTotals.Failed = $failedTests }
        return
    }

    if ($normalized -match '^Skipped:\s+(\d+)$') {
        $skippedTests = [int]$Matches[1]
        if ($null -ne $currentProjectTotals) { $currentProjectTotals.Skipped = $skippedTests }
        return
    }

    if ($normalized -match '^Total time:\s+(.+)$') {
        $totalDuration = $Matches[1]
        if ($null -ne $currentProjectTotals) {
            $currentProjectTotals.Duration = $totalDuration
            $projectTotals.Add($currentProjectTotals)
            $currentProjectTotals = $null
        }
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

$testProcess.WaitForExit()
$dotnetExitCode = $testProcess.ExitCode
}
finally {
    if ($cursorHidden) {
        Write-Host -NoNewline ("{0}[?25h" -f $escape)
        $cursorHidden = $false
    }
    if ($updateBaselines) {
        [Environment]::SetEnvironmentVariable("UPDATE_BASELINES", $previousUpdateBaselines, "Process")
    }
}

if ($progressSeen) {
    Write-Host -NoNewline ("{0}[1G" -f $escape)
    Write-Host ""
}

if ($resultLines.Count -eq 1) {
    Write-Host $resultLines[0]
}
elseif ($resultLines.Count -gt 1) {
    $aggregateFailed = 0
    $aggregatePassed = 0
    $aggregateSkipped = 0
    $aggregateTotal = 0
    $aggregateDurationSeconds = 0.0
    $aggregateParseSucceeded = $true

    foreach ($line in $resultLines) {
        if ($line -notmatch 'Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+),\s+Duration:\s+([0-9.]+)\s+(Seconds|Minutes|Hours)') {
            $aggregateParseSucceeded = $false
            break
        }

        $aggregateFailed += [int]$Matches[1]
        $aggregatePassed += [int]$Matches[2]
        $aggregateSkipped += [int]$Matches[3]
        $aggregateTotal += [int]$Matches[4]
        $durationSeconds = [double]$Matches[5]
        switch ($Matches[6]) {
            "Hours" { $durationSeconds *= 3600 }
            "Minutes" { $durationSeconds *= 60 }
        }
        $aggregateDurationSeconds += $durationSeconds
    }

    if ($aggregateParseSucceeded) {
        $aggregateStatus = if ($aggregateFailed -eq 0) { "Passed!" } else { "Failed!" }
        Write-Host ("{0}  - Failed: {1,5}, Passed: {2,5}, Skipped: {3,5}, Total: {4,5}, Duration: {5:F4} Seconds ({6} projects) - dotnet test" -f $aggregateStatus, $aggregateFailed, $aggregatePassed, $aggregateSkipped, $aggregateTotal, $aggregateDurationSeconds, $resultLines.Count)
    }
    else {
        $resultLines | ForEach-Object { Write-Host $_ }
    }
}
elseif ($projectTotals.Count -gt 1) {
    $aggregateFailed = ($projectTotals | Measure-Object -Property Failed -Sum).Sum
    $aggregatePassed = ($projectTotals | Measure-Object -Property Passed -Sum).Sum
    $aggregateSkipped = ($projectTotals | Measure-Object -Property Skipped -Sum).Sum
    $aggregateTotal = ($projectTotals | Measure-Object -Property Total -Sum).Sum
    $aggregateStatus = if ($aggregateFailed -eq 0) { "Passed!" } else { "Failed!" }
    Write-Host ("{0}  - Failed: {1,5}, Passed: {2,5}, Skipped: {3,5}, Total: {4,5}, Projects: {5} - dotnet test" -f $aggregateStatus, $aggregateFailed, $aggregatePassed, $aggregateSkipped, $aggregateTotal, $projectTotals.Count)
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
