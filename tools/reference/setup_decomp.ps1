# Fetch the pinned spidey-decomp commit as read-only reference data for the
# behavior ledger. Nothing in it is built, executed or copied into src/.
# The upstream repository has no license; it stays in the ignored third_party tree.
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$taskFolder = Join-Path $taskRoot 'tools\third_party\spidey-decomp'
$taskCommit = 'f8a039248908e085f8fba8106769aea7f8f9de67'
if (-not (Test-Path -LiteralPath (Join-Path $taskFolder '.git'))) {
    & git clone --quiet --no-checkout https://github.com/krystalgamer/spidey-decomp.git $taskFolder
    if ($LASTEXITCODE -ne 0) { throw 'Decomp clone failed' }
}
& git -C $taskFolder fetch --quiet --depth 1 origin $taskCommit
if ($LASTEXITCODE -ne 0) { throw 'Pinned decomp commit fetch failed' }
& git -C $taskFolder -c advice.detachedHead=false checkout --quiet --force $taskCommit
if ($LASTEXITCODE -ne 0) { throw 'Pinned decomp checkout failed' }
$taskHead = (& git -C $taskFolder rev-parse HEAD).Trim()
if ($taskHead -ne $taskCommit) { throw "Decomp HEAD $taskHead differs from pinned $taskCommit" }
Write-Output "Pinned decomp reference ready: $taskFolder @ $taskCommit"
