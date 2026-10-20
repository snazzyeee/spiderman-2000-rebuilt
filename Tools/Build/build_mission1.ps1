param(
    [string]$EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe',
    [switch]$RunTests,
    [switch]$SmokeTest
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'Logs'), (Join-Path $taskRoot 'TestResults') | Out-Null
function Invoke-MissionEditor([string[]]$TaskArguments, [string]$TaskLog) {
    $taskArgs = @('-batchmode', '-nographics', '-projectPath', ('"' + $taskRoot + '\Game"'),
        '-logFile', ('"' + $taskRoot + '\Logs\' + $TaskLog + '"')) + $TaskArguments
    $taskProcess = Start-Process $EditorPath -ArgumentList $taskArgs -WindowStyle Hidden -Wait -PassThru
    if ($taskProcess.ExitCode -ne 0) { throw "Unity failed ($($taskProcess.ExitCode)): Logs/$TaskLog" }
}
Invoke-MissionEditor @('-quit', '-executeMethod', 'SpiderManRebuild.Editor.Mission1SceneBuilder.Create') 'mission-scene.log'
if ($RunTests) {
    foreach ($taskPlatform in @('EditMode','PlayMode')) {
        $taskResult = Join-Path $taskRoot ('TestResults\mission-' + $taskPlatform.ToLowerInvariant() + '.xml')
        if (Test-Path -LiteralPath $taskResult) { Remove-Item -LiteralPath $taskResult -Force }
        Invoke-MissionEditor @('-runTests', '-testPlatform', $taskPlatform, '-testResults', ('"' + $taskResult + '"')) ('mission-' + $taskPlatform.ToLowerInvariant() + '.log')
        [xml]$taskXml = Get-Content -LiteralPath $taskResult
        if ($taskXml.'test-run'.result -ne 'Passed') { throw "Tests did not pass: $taskResult" }
    }
}
Invoke-MissionEditor @('-quit', '-executeMethod', 'SpiderManRebuild.Editor.Mission1SceneBuilder.Build') 'mission-build.log'
$taskExe = Join-Path $taskRoot 'Builds\Mission1\SpiderManMission1.exe'
if ($SmokeTest) {
    $taskSmokeReport = Join-Path $taskRoot 'TestResults\mission-smoke.json'
    if (Test-Path -LiteralPath $taskSmokeReport) { Remove-Item -LiteralPath $taskSmokeReport -Force }
    $taskArgs = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720',
        '-logFile',('"' + $taskRoot + '\Logs\mission-smoke.log"'),
        '--mission-smoke','--smoke-output',('"' + $taskRoot + '\TestResults\mission-smoke.json"'),
        '--capture-preview',('"' + $taskRoot + '\TestResults\mission-private.png"'))
    $taskProcess = Start-Process $taskExe -ArgumentList $taskArgs -WindowStyle Normal -Wait -PassThru
    if ($taskProcess.ExitCode -ne 0) { throw 'Mission smoke failed: TestResults/mission-smoke.json' }
    $taskSmokeResult = Get-Content -LiteralPath $taskSmokeReport -Raw | ConvertFrom-Json
    if (-not $taskSmokeResult.passed) { throw 'Mission smoke report did not pass.' }
}
Write-Output "Mission 1 build: $taskExe"
