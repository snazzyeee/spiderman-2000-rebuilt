param([ValidateSet('mobile', 'forward_plus')][string]$Renderer = 'forward_plus', [switch]$Editor, [switch]$SmokeTest, [switch]$Headless, [string]$Script, [switch]$Import, [switch]$TraversalTests)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$isWindowsHost = $env:OS -eq 'Windows_NT'
if ($isWindowsHost) {
    $executable = Join-Path $projectRoot 'tools/third_party/godot-4.7.2/Godot_v4.7.2-stable_win64_console.exe'
    if (-not (Test-Path -LiteralPath $executable)) { & (Join-Path $PSScriptRoot 'setup_godot.ps1') }
} else {
    $executable = Join-Path $projectRoot 'tools/third_party/godot-4.7.2-linux/Godot_v4.7.2-stable_linux.x86_64'
    if (-not (Test-Path -LiteralPath $executable)) { throw 'Run python tools/pipeline/setup_godot_linux.py first' }
}
# Keep editor paths writable inside the ignored tool installation, including
# imports launched from a sandbox without access to the host editor settings.
New-Item -ItemType File -Force -Path (Join-Path (Split-Path $executable) '_sc_') | Out-Null
& (Join-Path $PSScriptRoot 'guard_godot_scan.ps1')
$godotArgs = @('--path', $projectRoot, '--rendering-method', $Renderer)
if ($Editor) { $godotArgs += '--editor' }
if ($Headless) { $godotArgs += '--headless' }
if ($Import) { $godotArgs += @('--headless', '--editor', '--import', '--quit') }
if ($Script) { $godotArgs += @('--script', $Script) }
if ($TraversalTests) { $godotArgs += @('--script', 'res://src/diagnostics/traversal_runner.gd', '--quit-after', '2400', '--max-fps', '60', '--', '--traversal-tests') }
if ($SmokeTest) {
    & (Join-Path $projectRoot 'tools/third_party/audio-python/Scripts/python.exe') (Join-Path $projectRoot 'tools/migration/smoke_outputs.py') --check-output-paths
    if ($LASTEXITCODE -ne 0) { throw 'Smoke paths overlap preserved targets' }
    $godotArgs += @('--quit-after', '600', '--max-fps', '60', '--', '--revamp-smoke')
}
$startedUtc = [DateTime]::UtcNow
& $executable @godotArgs
if ($LASTEXITCODE -ne 0) { throw "Godot exited with code $LASTEXITCODE" }
if ($SmokeTest) {
    $reportPath = Join-Path $projectRoot 'staging/validation/smoke/godot-smoke.json'
    if (-not (Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $startedUtc) { throw 'Godot exited without a fresh smoke report' }
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if (-not $report.passed -or $report.checks.Count -lt 44) { throw 'Godot smoke report failed or was incomplete' }
}
if ($TraversalTests) {
    $reportPath = Join-Path $projectRoot 'staging/validation/traversal-tests.json'
    if (-not (Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $startedUtc) { throw 'Godot exited without a fresh traversal report' }
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if (-not $report.passed -or $report.checks.Count -lt 150 -or @($report.suites.PSObject.Properties).Count -ne 5) { throw 'Traversal tests failed or were incomplete' }
}
