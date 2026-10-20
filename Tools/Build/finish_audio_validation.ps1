param([int]$ExpectedClips = 991, [int]$TimeoutMinutes = 180)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$taskProgress = Join-Path $taskRoot 'LocalReference\Dialogue\ASR\progress.json'
$taskResult = Join-Path $taskRoot 'TestResults\audio-final-validation.json'
$taskPython = Join-Path $taskRoot 'Tools\ThirdParty\audio-python\Scripts\python.exe'
New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'Logs'), (Join-Path $taskRoot 'TestResults') | Out-Null
$taskLock = [IO.File]::Open((Join-Path $taskRoot 'Logs\finish-audio.lock'), [IO.FileMode]::OpenOrCreate,
    [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$taskReport = [ordered]@{ passed=$false; phase='waiting_for_asr'; error=''; startedUtc=[DateTime]::UtcNow.ToString('o') }
function Save-ValidationStatus {
    $taskReport | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $taskResult -Encoding utf8
}
Push-Location $taskRoot
try {
    Save-ValidationStatus
    $taskDeadline = [DateTime]::UtcNow.AddMinutes($TimeoutMinutes)
    while ($true) {
        if ([DateTime]::UtcNow -gt $taskDeadline) { throw 'Timed out waiting for the existing ASR run; it was not terminated.' }
        if (Test-Path -LiteralPath $taskProgress) {
            $taskRun = Get-Content -LiteralPath $taskProgress -Raw | ConvertFrom-Json
            if ($taskRun.finished_unix -and $taskRun.selected -eq $ExpectedClips) {
                if ($taskRun.failures.Count -ne 0 -or $taskRun.completed -ne $ExpectedClips) {
                    throw 'ASR finished with failures or an incomplete corpus; inspect its private progress report.'
                }
                break
            }
        }
        Start-Sleep -Seconds 5
    }
    Copy-Item -LiteralPath $taskProgress -Destination (Join-Path $taskRoot 'TestResults\asr-inference-run.json') -Force
    $taskReport.phase='applying_cached_trigger_context'; Save-ValidationStatus
    & $taskPython Tools/Reference/batch_process_audio.py --model Tools/ThirdParty/audio-models/local-small.en --offline *> Logs/batch-audio-final-join.log
    if ($LASTEXITCODE -ne 0) { throw 'Cached trigger join failed; inspect Logs/batch-audio-final-join.log.' }
    $taskJoined = Get-Content -LiteralPath $taskProgress -Raw | ConvertFrom-Json
    if ($taskJoined.completed -ne $ExpectedClips -or $taskJoined.reused -ne $ExpectedClips) {
        throw 'The final join did not reuse the complete verified ASR cache.'
    }
    $taskManifest = Get-Content Game/Assets/_LocalReference/Audio/dialogue_manifest.json -Raw | ConvertFrom-Json
    $taskDialogue = @($taskManifest.clips | Where-Object { $_.category -eq 'Dialogue' })
    if ($taskDialogue.Count -ne $ExpectedClips) { throw 'Unexpected dialogue entry count after joining.' }
    foreach ($taskEntry in $taskDialogue) {
        foreach ($taskField in @('clip_id','speaker','level_id','transcript','trigger_condition','level_trigger_names','asr')) {
            if ($taskEntry.PSObject.Properties.Match($taskField).Count -ne 1) { throw "Missing $taskField on $($taskEntry.clip_id)" }
        }
    }
    $taskReport.asrCompleted=$taskJoined.completed
    $taskReport.asrCached=$taskJoined.reused
    $taskReport.transcriptsWithSpeech=@($taskDialogue | Where-Object { $_.transcript }).Count
    $taskReport.noSpeechDetected=@($taskDialogue | Where-Object { $_.transcript_status -eq 'no_speech_detected' }).Count
    $taskReport.unknownSpeakers=@($taskDialogue | Where-Object { $_.speaker -eq 'Unknown' }).Count
    $taskReport.unresolvedTriggerConditions=@($taskDialogue | Where-Object { -not $_.trigger_condition }).Count
    $taskReport.namedTrgNodes=$taskJoined.trg_context.named_nodes
    $taskReport.phase='python_tests'; Save-ValidationStatus
    & $taskPython -m unittest discover -s Tools/Reference/tests -v *> Logs/batch-audio-tests.log
    if ($LASTEXITCODE -ne 0) { throw 'Python audio tests failed; inspect Logs/batch-audio-tests.log.' }
    $taskReport.pythonTestsPassed=$true
    $taskReport.phase='unity_build_and_tests'; Save-ValidationStatus
    & (Join-Path $PSScriptRoot 'build_frontend.ps1') -RunTests -SmokeTest
    foreach ($taskPlatform in @('editmode','playmode')) {
        [xml]$taskXml = Get-Content (Join-Path $taskRoot "TestResults\frontend-$taskPlatform.xml") -Raw
        if ($taskXml.'test-run'.result -ne 'Passed') { throw "Unity $taskPlatform suite did not pass." }
        $taskReport[$taskPlatform + 'Passed']=[int]$taskXml.'test-run'.passed
    }
    $taskMenu = Get-Content TestResults/menu-smoke.json -Raw | ConvertFrom-Json
    if (-not $taskMenu.passed) { throw 'Final standalone menu check did not pass.' }
    $taskReport.menuChecks=$taskMenu.checks.Count
    $taskReport.buildBytes=(Get-ChildItem Builds/Frontend -Recurse -File | Measure-Object Length -Sum).Sum
    $taskReport.phase='complete'; $taskReport.passed=$true
    $taskReport.finishedUtc=[DateTime]::UtcNow.ToString('o'); Save-ValidationStatus
    Write-Output "Final audio/build validation: $taskResult"
}
catch {
    $taskReport.phase='failed'; $taskReport.error=$_.Exception.Message; Save-ValidationStatus
    throw
}
finally { Pop-Location; $taskLock.Dispose() }
