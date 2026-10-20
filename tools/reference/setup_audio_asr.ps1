param([string]$PythonPath = '')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if (-not $PythonPath) {
    $taskBundled = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
    $PythonPath = if (Test-Path -LiteralPath $taskBundled) { $taskBundled } else { 'python' }
}
$taskEnvironment = Join-Path $taskRoot 'tools\third_party\audio-python'
$taskPython = Join-Path $taskEnvironment 'Scripts\python.exe'
if (-not (Test-Path -LiteralPath $taskPython)) {
    & $PythonPath -m venv $taskEnvironment
    if ($LASTEXITCODE -ne 0) { throw 'ASR environment creation failed. Use a 64-bit Python with available CTranslate2 wheels.' }
}
& $taskPython -m pip install --only-binary=:all: -r (Join-Path $PSScriptRoot 'requirements-audio.txt')
if ($LASTEXITCODE -ne 0) { throw 'Pinned ASR dependency installation failed.' }
$taskModel = Join-Path $taskRoot 'tools\third_party\audio-models\local-small.en'
New-Item -ItemType Directory -Force -Path $taskModel | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'staging\logs') | Out-Null
$taskRevision = 'd1d751a5f8271d482d14ca55d9e2deeebbae577f'
$taskMembers = @(
    @{ Name='config.json'; Bytes=2657; Hash='666a9605530ac1f61fa8177f3702b4dacec9966749e42610839fcc32661d5fae' },
    @{ Name='tokenizer.json'; Bytes=2128466; Hash='929c5252409436dce1b38a75d1abbcb5e132d170d8e324e4e04ed915fa2d22df' },
    @{ Name='vocabulary.txt'; Bytes=422309; Hash='ff77588746d3a2595d32ab5b69ffd7b95ce2441ac57533cb66fc3eb575a115cf' },
    @{ Name='model.bin'; Bytes=483545366; Hash='62b2a45b05ee59acb4a5341b33ee35e041395d378d418a18acfe4c9e768ee37a' }
)
foreach ($taskMember in $taskMembers) {
    $taskTarget = Join-Path $taskModel $taskMember.Name
    if ((Test-Path -LiteralPath $taskTarget) -and
        (Get-Item -LiteralPath $taskTarget).Length -eq $taskMember.Bytes -and
        (Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash.ToLowerInvariant() -eq $taskMember.Hash) { continue }
    $taskPartial = $taskTarget + '.part'
    $taskUrl = "https://huggingface.co/Systran/faster-whisper-small.en/resolve/$taskRevision/$($taskMember.Name)"
    & curl.exe -L --fail --retry 10 --retry-all-errors --retry-delay 2 --speed-time 30 --speed-limit 1024 --connect-timeout 15 --max-time 1800 --continue-at - --output $taskPartial $taskUrl
    if ($LASTEXITCODE -ne 0) { throw "Model download failed; partial data retained: $taskPartial" }
    if ((Get-Item -LiteralPath $taskPartial).Length -ne $taskMember.Bytes -or
        (Get-FileHash -LiteralPath $taskPartial -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskMember.Hash) {
        throw "Model member failed length/SHA-256 verification: $taskPartial"
    }
    Move-Item -LiteralPath $taskPartial -Destination $taskTarget -Force
}
Write-Output "Verified local ASR model: $taskModel"
Write-Output "Run: & '$taskPython' tools/reference/batch_process_audio.py --model tools/third_party/audio-models/local-small.en --offline"
