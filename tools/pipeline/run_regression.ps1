param([Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]+$')][string]$Milestone)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$evidence = Join-Path $projectRoot "staging/validation/regression/$Milestone"
New-Item -ItemType Directory -Force $evidence | Out-Null
$env:TEMP = Join-Path $projectRoot 'staging/test_tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Force $env:TEMP | Out-Null
$python = Join-Path $projectRoot 'tools/third_party/audio-python/Scripts/python.exe'
$summary = [ordered]@{ milestone=$Milestone; python=@(); targets=@(); passed=$false }
foreach ($suite in @('reference','pipeline')) {
    $log = Join-Path $evidence "python-$suite.log"
    & $python -m unittest discover -s (Join-Path $projectRoot "tools/$suite/tests") *> $log
    if ($LASTEXITCODE -ne 0) { throw "Python $suite tests failed: $log" }
    $match = [regex]::Match((Get-Content $log -Raw), 'Ran (\d+) tests')
    if (-not $match.Success) { throw "Missing Python result: $log" }
    $summary.python += @{ suite=$suite; checks=[int]$match.Groups[1].Value; passed=$true }
}
& (Join-Path $PSScriptRoot 'run_godot.ps1') -Import *> (Join-Path $evidence 'import.log')
foreach ($target in @('headless','forward_plus','mobile')) {
    $arguments = @{ Renderer= $(if ($target -eq 'headless') { 'forward_plus' } else { $target }); Headless=($target -eq 'headless') }
    foreach ($suite in @('smoke','traversal','fonts','menu-audio')) {
        $log = Join-Path $evidence "$suite-$target.log"
        if ($suite -eq 'smoke') {
            & (Join-Path $PSScriptRoot 'run_godot.ps1') @arguments -SmokeTest *> $log
            $reportPath = Join-Path $projectRoot 'staging/validation/smoke/godot-smoke.json'
        } elseif ($suite -eq 'traversal') {
            & (Join-Path $PSScriptRoot 'run_godot.ps1') @arguments -TraversalTests *> $log
            $reportPath = Join-Path $projectRoot 'staging/validation/traversal-tests.json'
        } elseif ($suite -eq 'fonts') {
            $startedFontTest = [DateTime]::UtcNow
            & (Join-Path $PSScriptRoot 'run_godot.ps1') @arguments -Script 'res://src/diagnostics/font_tests.gd' *> $log
            $reportPath = Join-Path $projectRoot 'staging/validation/font-tests.json'
            if (-not (Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $startedFontTest) { throw 'Missing fresh font test report' }
            $fontReport = Get-Content $reportPath -Raw | ConvertFrom-Json
            if (-not $fontReport.passed -or $fontReport.checks.Count -lt 10) { throw 'Font tests failed or incomplete' }
        } else {
            $startedMenuTest = [DateTime]::UtcNow
            & (Join-Path $PSScriptRoot 'run_godot.ps1') @arguments -Script 'res://src/diagnostics/menu_audio_tests.gd' *> $log
            $reportPath = Join-Path $projectRoot 'staging/validation/menu-audio-tests.json'
            if (-not (Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $startedMenuTest) { throw 'Missing fresh menu audio test report' }
            $menuReport = Get-Content $reportPath -Raw | ConvertFrom-Json
            if (-not $menuReport.passed -or $menuReport.checks.Count -lt 17) { throw 'Menu audio tests failed or incomplete' }
        }
        $report = Get-Content $reportPath -Raw | ConvertFrom-Json
        if ($report.renderer -ne $arguments.Renderer -or ($target -ne 'headless' -and $report.display_driver -eq 'headless')) { throw "Wrong renderer/display for $target" }
        if ($target -ne 'headless' -and -not (Select-String -LiteralPath $log -Pattern '^Vulkan .*Using Device')) { throw "No real Vulkan evidence for $target" }
        Copy-Item -LiteralPath $reportPath -Destination (Join-Path $evidence "$suite-$target.json")
        $summary.targets += @{ suite=$suite; target=$target; checks=$report.checks.Count; passed=$report.passed }
    }
}
$errors = @(Get-ChildItem -LiteralPath $evidence -Filter '*.log' | Select-String -Pattern '^ERROR:|^SCRIPT ERROR:|^Shader ERROR:')
if ($errors.Count) { throw "Engine/script errors in $evidence : $errors" }
$summary.passed = $true
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence 'summary.json') -Encoding utf8
$summary | ConvertTo-Json -Depth 6
