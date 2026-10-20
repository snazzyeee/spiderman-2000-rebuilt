param([string]$PythonPath, [string]$Manifest, [switch]$Offline, [switch]$SmokeTest)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $projectRoot
try {
    if (-not $PythonPath) { $PythonPath = Join-Path $projectRoot 'Tools/ThirdParty/audio-python/Scripts/python.exe' }
    if (-not $Manifest) { $Manifest = Join-Path $projectRoot 'Game/Assets/_LocalReference/Audio/dialogue_manifest.json' }
    if (-not (Test-Path -LiteralPath $Manifest)) { throw 'Extract audio first with Tools/Reference/extract_audio_manifest.py; pass its JSON path with -Manifest.' }
    & $PythonPath -c 'import requests, bs4'
    if ($LASTEXITCODE -ne 0) {
        & $PythonPath -m pip install --disable-pip-version-check -r Tools/pipeline/requirements.txt
        if ($LASTEXITCODE -ne 0) { throw 'Scraper dependency setup failed' }
    }
    & Tools/Reference/setup_converter.ps1
    & Tools/pipeline/setup_godot.ps1
    & $PythonPath Tools/pipeline/convert_characters.py
    if ($LASTEXITCODE -ne 0) { throw 'Character conversion failed' }
    & $PythonPath Tools/pipeline/prepare_godot_ui.py
    if ($LASTEXITCODE -ne 0) { throw 'Menu texture conversion failed' }
    $manifestArgs = @('Tools/pipeline/build_dialogue_manifest.py', '--input', $Manifest)
    if ($Offline) { $manifestArgs += '--offline' }
    & $PythonPath @manifestArgs
    if ($LASTEXITCODE -ne 0) { throw 'Dialogue registry generation failed' }
    $godot = Join-Path $projectRoot 'Tools/ThirdParty/godot-4.7.2/Godot_v4.7.2-stable_win64_console.exe'
    & $godot --headless --editor --path Godot --import
    if ($LASTEXITCODE -ne 0) { throw 'Godot import failed' }
    & $godot --headless --path Godot --script res://scripts/diagnostics/generate_layout.gd
    if ($LASTEXITCODE -ne 0) { throw 'Godot route generation failed' }
    if ($SmokeTest) { & Tools/pipeline/run_godot.ps1 -Headless -SmokeTest }
} finally { Pop-Location }
