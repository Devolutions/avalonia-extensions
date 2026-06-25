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

& dotnet test @dotnetArgs 2>&1 | ForEach-Object {
    $line = "$_"
    $normalized = $line

    if ($normalized -match '^\[xUnit\.net\s+[^\]]+\]\s*(.*)$') {
        $normalized = $Matches[1]
    }

    $normalized = $normalized.TrimStart()

    if ($normalized -match '^Visual regression detected for \[([^\]]+)\] (.*) - ([^.]+)\. Diff saved to (.*)$') {
        $row = "Visual regression`t$($Matches[1])`t$($Matches[2])`t$($Matches[3])`t$($Matches[4])"
        [void]$summaryRows.Add($row)
        return
    }

    if ($normalized -match '^No baseline found for \[([^\]]+)\] (.*) - ([^.]+)\. Saved screenshot to (.*)$') {
        $row = "No baseline`t$($Matches[1])`t$($Matches[2])`t$($Matches[3])`t$($Matches[4])"
        [void]$summaryRows.Add($row)
        return
    }

    Write-Output $line
}

$dotnetExitCode = $LASTEXITCODE

if ($summaryRows.Count -gt 0) {
    Write-Host ""
    Write-Host "________________________________________________________________________________"
    Write-Host "Visual regression summary" -ForegroundColor Yellow
    Write-Host ("{0,-18} {1,-14} {2,-34} {3,-10} {4}" -f "Status", "Theme", "Page", "Variant", "Path") -ForegroundColor Yellow

    foreach ($row in $summaryRows) {
        $parts = $row -split "`t", 5
        Write-Host ("{0,-18} {1,-14} {2,-34} {3,-10} {4}" -f $parts[0], "[$($parts[1])]", $parts[2], $parts[3], $parts[4]) -ForegroundColor Yellow
    }

    Write-Host "________________________________________________________________________________"
    Write-Host ""
}

exit $dotnetExitCode
