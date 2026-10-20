param([ValidateSet('mobile', 'forward_plus')][string]$Renderer = 'mobile', [switch]$Editor, [switch]$SmokeTest, [switch]$Headless)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$executable = Join-Path $projectRoot 'Tools/ThirdParty/godot-4.7.2/Godot_v4.7.2-stable_win64_console.exe'
if (-not (Test-Path -LiteralPath $executable)) { & (Join-Path $PSScriptRoot 'setup_godot.ps1') }
$godotArgs = @('--path', (Join-Path $projectRoot 'Godot'), '--rendering-method', $Renderer)
if ($Editor) { $godotArgs += '--editor' }
if ($Headless) { $godotArgs += '--headless' }
if ($SmokeTest) { $godotArgs += @('--', '--revamp-smoke') }
& $executable @godotArgs
if ($LASTEXITCODE -ne 0) { throw "Godot exited with code $LASTEXITCODE" }
