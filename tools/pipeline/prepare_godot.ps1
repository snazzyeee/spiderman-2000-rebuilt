param([string]$PythonPath, [string]$Manifest, [switch]$Offline, [switch]$SmokeTest)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $projectRoot
try {
    if (-not $PythonPath) { $PythonPath = Join-Path $projectRoot 'tools/third_party/audio-python/Scripts/python.exe' }
    if (-not $Manifest) { $Manifest = Join-Path $projectRoot 'staging/raw_extracted/audio_source/dialogue_manifest.json' }
    if (-not (Test-Path -LiteralPath $Manifest)) { throw 'Extract audio first with tools/reference/extract_audio_manifest.py; pass its JSON path with -Manifest.' }
    & $PythonPath -c 'import requests, bs4'
    if ($LASTEXITCODE -ne 0) {
        & $PythonPath -m pip install --disable-pip-version-check -r tools/pipeline/requirements.txt
        if ($LASTEXITCODE -ne 0) { throw 'Scraper dependency setup failed' }
    }
    & tools/reference/setup_converter.ps1
    & tools/pipeline/setup_godot.ps1
    & $PythonPath tools/pipeline/convert_characters.py
    if ($LASTEXITCODE -ne 0) { throw 'Character conversion failed' }
    & $PythonPath tools/pipeline/prepare_godot_ui.py
    if ($LASTEXITCODE -ne 0) { throw 'Menu texture conversion failed' }
    & $PythonPath tools/reference/extract_native_voice_catalog.py
    if ($LASTEXITCODE -ne 0) { throw 'Canonical XA identity catalog failed' }
    & $PythonPath tools/reference/decode_sfx_ids.py
    if ($LASTEXITCODE -ne 0) { throw 'SFX/KAT identity catalog failed' }
    $manifestArgs = @('tools/pipeline/build_dialogue_manifest.py', '--input', $Manifest)
    if ($Offline) { $manifestArgs += '--offline' }
    & $PythonPath @manifestArgs
    if ($LASTEXITCODE -ne 0) { throw 'Dialogue registry generation failed' }
    & tools/pipeline/run_godot.ps1 -Import
    & tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/generate_layout.gd
    if ($SmokeTest) { & tools/pipeline/run_godot.ps1 -Headless -SmokeTest }
} finally { Pop-Location }
