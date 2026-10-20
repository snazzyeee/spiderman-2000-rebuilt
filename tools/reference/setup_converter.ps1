$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$taskFolder = Join-Path $taskRoot 'tools\third_party\neversoft-multitool-1.3.4'
$taskExe = Join-Path $taskFolder 'NeversoftMultitool-Windows-CLI-x64\NeversoftMultitool.exe'
$taskExeDigest = 'B83B49333DC70A58A7FF730548287B0A719800EB03408823255F68F3E330B95A'
if (Test-Path -LiteralPath $taskExe) {
    if ((Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash -ne $taskExeDigest) {
        throw 'Existing converter differs from the pinned release. Inspect it before continuing.'
    }
    Write-Output "Pinned converter already verified: $taskExe"
    return
}
New-Item -ItemType Directory -Force -Path $taskFolder | Out-Null
$taskZip = Join-Path $taskFolder 'NeversoftMultitool-Windows-CLI-x64.zip'
Invoke-WebRequest -Uri 'https://github.com/slfx77/neversoft-multitool/releases/download/v1.3.4/NeversoftMultitool-Windows-CLI-x64.zip' -OutFile $taskZip
if ((Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash -ne '062C89E00382E7B7C4B73015F228C88E7BE34E50A4C48654D39E3142BD633D35') {
    throw 'Official release archive hash mismatch. Nothing was executed.'
}
Expand-Archive -LiteralPath $taskZip -DestinationPath $taskFolder
if ((Get-FileHash -LiteralPath $taskExe -Algorithm SHA256).Hash -ne $taskExeDigest) {
    throw 'Extracted converter hash mismatch. Nothing was executed.'
}
Write-Output "Verified converter: $taskExe"
