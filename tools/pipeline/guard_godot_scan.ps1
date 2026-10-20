# Godot imports every resource under the project root. Raw extraction backups and
# archived source art live in ignored folders, so recreate their .gdignore markers
# before any editor, import or runtime launch.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$guarded = @(Join-Path $projectRoot 'staging')
$assets = Join-Path $projectRoot 'assets'
if (Test-Path -LiteralPath $assets) {
    $guarded += @(Get-ChildItem -LiteralPath $assets -Directory | ForEach-Object { Join-Path $_.FullName 'source_archive' } | Where-Object { Test-Path -LiteralPath $_ })
}
New-Item -ItemType Directory -Force -Path $guarded[0] | Out-Null
foreach ($folder in $guarded) {
    $marker = Join-Path $folder '.gdignore'
    if (-not (Test-Path -LiteralPath $marker)) { New-Item -ItemType File -Path $marker | Out-Null }
}
