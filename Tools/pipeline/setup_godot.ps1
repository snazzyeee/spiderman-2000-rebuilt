param([switch]$Launch)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$destination = Join-Path $projectRoot 'Tools/ThirdParty/godot-4.7.2'
$zip = Join-Path $destination 'godot.zip'
$editor = Join-Path $destination 'Godot_v4.7.2-stable_win64.exe'
New-Item -ItemType Directory -Force $destination | Out-Null
$expected = '731980f9608d61333e5baf54a2ef17210acc7a538446c0cb9969f002aca1e953'
if (-not (Test-Path -LiteralPath $zip) -or (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant() -ne $expected) {
    & curl.exe --fail --location --retry 4 --connect-timeout 20 --output $zip 'https://github.com/godotengine/godot/releases/download/4.7.2-stable/Godot_v4.7.2-stable_win64.exe.zip'
    if ($LASTEXITCODE -ne 0) { throw 'Official Godot download failed' }
}
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant() -ne $expected) { throw 'Godot ZIP digest mismatch' }
if (-not (Test-Path -LiteralPath $editor)) { Expand-Archive -LiteralPath $zip -DestinationPath $destination -Force }
& (Join-Path $destination 'Godot_v4.7.2-stable_win64_console.exe') --version
if ($LASTEXITCODE -ne 0) { throw 'Godot version check failed' }
if ($Launch) { Start-Process -FilePath $editor -ArgumentList @('--editor', '--path', ('"' + (Join-Path $projectRoot 'Godot') + '"')) -WindowStyle Hidden }
