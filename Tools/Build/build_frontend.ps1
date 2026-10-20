param(
    [string]$EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe',
    [switch]$ExtractAudio,
    [switch]$RunTests,
    [switch]$SmokeTest
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'Logs'), (Join-Path $taskRoot 'TestResults') | Out-Null
Push-Location $taskRoot
try {
    if ($ExtractAudio) {
        & python Tools/Reference/extract_audio_manifest.py LocalReference/PC/app/data.pkr LocalReference/PC/app/media.pkr --output Game/Assets/_LocalReference/Audio --converter Tools/ThirdParty/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe
        if ($LASTEXITCODE -ne 0) { throw 'Audio extraction failed; inspect its evidence report.' }
    }
    & python Tools/Reference/prepare_frontend_assets.py
    if ($LASTEXITCODE -ne 0) { throw 'Original menu art preparation failed.' }
    function Invoke-FrontendEditor([string[]]$TaskArguments, [string]$TaskLog) {
        $taskArgs = @('-batchmode', '-nographics', '-projectPath', ('"' + $taskRoot + '\Game"'),
            '-logFile', ('"' + $taskRoot + '\Logs\' + $TaskLog + '"')) + $TaskArguments
        $taskProcess = Start-Process $EditorPath -ArgumentList $taskArgs -WindowStyle Hidden -Wait -PassThru
        if ($taskProcess.ExitCode -ne 0) { throw "Unity failed ($($taskProcess.ExitCode)): Logs/$TaskLog" }
    }
    Invoke-FrontendEditor @('-quit','-executeMethod','SpiderManRebuild.Editor.DialogueAssetBuilder.BuildDatabase') 'frontend-audio.log'
    Invoke-FrontendEditor @('-quit','-executeMethod','SpiderManRebuild.Editor.MainMenuSceneBuilder.Create') 'frontend-scene.log'
    if ($RunTests) {
        foreach ($taskPlatform in @('EditMode','PlayMode')) {
            $taskResult = Join-Path $taskRoot ('TestResults\frontend-' + $taskPlatform.ToLowerInvariant() + '.xml')
            if (Test-Path -LiteralPath $taskResult) { Remove-Item -LiteralPath $taskResult -Force }
            Invoke-FrontendEditor @('-runTests','-testPlatform',$taskPlatform,'-testResults',('"' + $taskResult + '"')) ('frontend-' + $taskPlatform.ToLowerInvariant() + '.log')
            [xml]$taskXml = Get-Content -LiteralPath $taskResult
            if ($taskXml.'test-run'.result -ne 'Passed') { throw "Tests did not pass: $taskResult" }
        }
    }
    Invoke-FrontendEditor @('-quit','-executeMethod','SpiderManRebuild.Editor.MainMenuSceneBuilder.Build') 'frontend-build.log'
    $taskExe = Join-Path $taskRoot 'Builds\Frontend\SpiderManRevamp.exe'
    if ($SmokeTest) {
        $taskReport = Join-Path $taskRoot 'TestResults\menu-smoke.json'
        foreach ($taskOutput in @($taskReport, (Join-Path $taskRoot 'TestResults\menu-smoke-save.json'), (Join-Path $taskRoot 'TestResults\menu-private.png'))) {
            if (Test-Path -LiteralPath $taskOutput) { Remove-Item -LiteralPath $taskOutput -Force }
        }
        $taskArgs = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720',
            '-logFile',('"' + $taskRoot + '\Logs\menu-smoke.log"'), '--menu-smoke',
            '--menu-output',('"' + $taskReport + '"'), '--menu-preview',('"' + $taskRoot + '\TestResults\menu-private.png"'))
        $taskProcess = Start-Process $taskExe -ArgumentList $taskArgs -WindowStyle Normal -Wait -PassThru
        if ($taskProcess.ExitCode -ne 0) { throw 'Menu smoke failed: TestResults/menu-smoke.json' }
        $taskSmoke = Get-Content -LiteralPath $taskReport -Raw | ConvertFrom-Json
        if (-not $taskSmoke.passed) { throw 'Menu smoke report did not pass.' }
    }
    Write-Output "Menu + Mission 1 build: $taskExe"
}
finally { Pop-Location }
