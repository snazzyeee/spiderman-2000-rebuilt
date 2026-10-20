param([ValidateSet('mobile', 'forward_plus')][string]$Renderer = 'mobile', [switch]$Editor, [switch]$SmokeTest, [switch]$Headless)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$executable = Join-Path $projectRoot 'Tools/ThirdParty/godot-4.7.2/Godot_v4.7.2-stable_win64_console.exe'
if (-not (Test-Path -LiteralPath $executable)) { & (Join-Path $PSScriptRoot 'setup_godot.ps1') }
$godotArgs = @('--path', (Join-Path $projectRoot 'Godot'), '--rendering-method', $Renderer)
if ($Editor) { $godotArgs += '--editor' }
if ($Headless) { $godotArgs += '--headless' }
if ($SmokeTest) { $godotArgs += @('--quit-after', '600', '--max-fps', '60', '--', '--revamp-smoke') }
$startedUtc = [DateTime]::UtcNow
& $executable @godotArgs
if ($LASTEXITCODE -ne 0) { throw "Godot exited with code $LASTEXITCODE" }
if ($SmokeTest) {
    $reportPath = Join-Path $projectRoot 'TestResults/godot-smoke.json'
    if (-not (Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $startedUtc) { throw 'Godot exited without a fresh smoke report' }
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if (-not $report.passed -or $report.checks.Count -lt 40) { throw 'Godot smoke report failed or was incomplete' }
}
