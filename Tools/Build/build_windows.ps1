param(
    [string]$EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe',
    [switch]$RunTests,
    [switch]$SmokeTest
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$taskGame = Join-Path $taskRoot 'Game'
$taskLogs = Join-Path $taskRoot 'Logs'
$taskResults = Join-Path $taskRoot 'TestResults'
if (-not (Test-Path -LiteralPath $EditorPath)) { throw "Pinned Unity editor not found: $EditorPath" }
if ((Get-Content (Join-Path $taskGame 'ProjectSettings\ProjectVersion.txt') -First 1) -ne 'm_EditorVersion: 6000.6.6f1') {
    throw 'Project editor pin changed. Review this build script before proceeding.'
}
New-Item -ItemType Directory -Force -Path $taskLogs, $taskResults | Out-Null
function Invoke-LabEditor([string[]]$TaskArguments, [string]$TaskLogName) {
    $taskLog = Join-Path $taskLogs $TaskLogName
    $taskArgumentsAll = @('-batchmode', '-nographics', '-projectPath', ('"' + $taskGame + '"'),
        '-logFile', ('"' + $taskLog + '"')) + $TaskArguments
    $taskRun = Start-Process -FilePath $EditorPath -ArgumentList $taskArgumentsAll -WindowStyle Hidden -PassThru -Wait
    if ($taskRun.ExitCode -ne 0) { throw "Unity exited $($taskRun.ExitCode). See $taskLog" }
}
Invoke-LabEditor @('-quit', '-executeMethod', 'SpiderManRebuild.Editor.LabSceneBuilder.Create') 'unity-scene.log'
if ($RunTests) {
    foreach ($taskPlatform in @('EditMode', 'PlayMode')) {
        $taskReport = Join-Path $taskResults ($taskPlatform.ToLowerInvariant() + '.xml')
        Invoke-LabEditor @('-runTests', '-testPlatform', $taskPlatform, '-testResults', ('"' + $taskReport + '"')) ('unity-' + $taskPlatform.ToLowerInvariant() + '.log')
        [xml]$taskXml = Get-Content -LiteralPath $taskReport
        if ($taskXml.'test-run'.result -ne 'Passed') { throw "Tests did not pass: $taskReport" }
    }
}
Invoke-LabEditor @('-quit', '-executeMethod', 'SpiderManRebuild.Editor.BuildWindows.Build') 'unity-build.log'
$taskExe = Join-Path $taskRoot 'Builds\Windows\TraversalLab.exe'
if ($SmokeTest) {
    $taskOutput = Join-Path $taskResults 'standalone-smoke.json'
    $taskPreview = Join-Path $taskResults 'traversal-lab.png'
    $taskPlayerLog = Join-Path $taskLogs 'standalone-smoke.log'
    # ScreenCapture requires a rendered window; the interactive laboratory closes after this route.
    $taskPlayerArgs = @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720',
        '-logFile', ('"' + $taskPlayerLog + '"'), '--smoke-test',
        '--smoke-output', ('"' + $taskOutput + '"'), '--capture-preview', ('"' + $taskPreview + '"'))
    $taskRun = Start-Process -FilePath $taskExe -ArgumentList $taskPlayerArgs -WindowStyle Normal -Wait -PassThru
    if ($taskRun.ExitCode -ne 0) { throw "Standalone smoke test failed. See $taskOutput and $taskPlayerLog" }
}
Write-Output "Windows laboratory: $taskExe"
