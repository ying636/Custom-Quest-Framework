param(
    [string]$GameDirectory = 'D:/工作间/AI/RimWorldStandalone17',
    [string]$RunRoot = 'D:/工作间/AI/CQFOptimizationRuns',
    [int]$TimeoutSeconds = 300,
    [switch]$CoreOnly
)

$ErrorActionPreference = 'Stop'
$taskWorkspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$taskRun = Join-Path $RunRoot ('CQF_Optimization_' + [guid]::NewGuid().ToString('N'))
$taskGame = Join-Path $taskRun 'Game'
$taskSave = Join-Path $taskRun 'SaveData'
$taskCore = Join-Path $taskWorkspace '1.7/Assemblies/net10.0/QuestEditor_Library.dll'
$taskEditor = Join-Path $taskWorkspace '1.7/Optional/Editor/CQF.Editor.dll'
$taskDomain = Join-Path $taskWorkspace '1.7/Optional/Domain/CQF.Domain.dll'
$taskHarmony = Join-Path $GameDirectory 'Mods/Harmony/1.7/Assemblies/0Harmony.dll'
$taskMarker = Get-Content -LiteralPath (Join-Path $GameDirectory 'standalone-runtime.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ((Get-FileHash -LiteralPath (Join-Path $GameDirectory 'Assembly-CSharp.dll')).Hash -ne $taskMarker.assemblySha256) { throw 'Standalone assembly hash does not match its manifest.' }
New-Item -ItemType Directory -Path $taskRun, $taskGame, (Join-Path $taskGame 'Mods'), (Join-Path $taskSave 'Config') -Force | Out-Null
dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release --nologo -v:q -p:WarningLevel=0 "-p:CoreAssembly=$taskCore" "-p:EditorAssembly=$taskEditor" "-p:DomainAssembly=$taskDomain" "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$taskHarmony" "-p:OutputPath=$taskRun/Build/"
if ($LASTEXITCODE -ne 0) { throw 'Optimization fixture compilation failed.' }
foreach ($taskFile in Get-ChildItem -LiteralPath $GameDirectory -File) {
    if ([IO.Path]::GetPathRoot($taskGame) -eq [IO.Path]::GetPathRoot($GameDirectory)) {
        New-Item -ItemType HardLink -Path (Join-Path $taskGame $taskFile.Name) -Target $taskFile.FullName | Out-Null
    } else { Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskGame $taskFile.Name) }
}
foreach ($taskDirectory in @('Data', 'Rendering', 'vulkan', 'OfflineTesting')) {
    New-Item -ItemType Junction -Path (Join-Path $taskGame $taskDirectory) -Target (Join-Path $GameDirectory $taskDirectory) | Out-Null
}
$taskMod = Join-Path $taskGame 'Mods/CQF'
New-Item -ItemType Directory -Path $taskMod -Force | Out-Null
foreach ($taskContent in @('About', 'Languages', 'Patches', 'Quests', 'Textures', '1.7', 'LoadFolders.xml')) {
    Copy-Item -LiteralPath (Join-Path $taskWorkspace $taskContent) -Destination $taskMod -Recurse
}
Copy-Item -LiteralPath (Join-Path $taskWorkspace 'About') -Destination $taskRun -Recurse
foreach ($taskDependency in @('Harmony', 'RimDevTools17')) {
    New-Item -ItemType Junction -Path (Join-Path $taskGame ('Mods/' + $taskDependency)) -Target (Join-Path $GameDirectory ('Mods/' + $taskDependency)) | Out-Null
}
$taskFixture = Join-Path $taskGame 'Mods/CQF_OptimizationChecks'
New-Item -ItemType Directory -Path (Join-Path $taskFixture 'About'), (Join-Path $taskFixture 'Domain') -Force | Out-Null
'<ModMetaData><name>CQF.OptimizationChecks</name><author>HaiLuan</author><packageId>CQF.OptimizationChecks</packageId><supportedVersions><li>1.7</li></supportedVersions></ModMetaData>' | Set-Content -LiteralPath (Join-Path $taskFixture 'About/About.xml') -Encoding UTF8
'<loadFolders><v1.7><li>/</li><li RequiresCQFModule="Domain">Domain</li></v1.7></loadFolders>' | Set-Content -LiteralPath (Join-Path $taskFixture 'LoadFolders.xml') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $taskRun 'Build/CQF.OptimizationChecks.dll') -Destination (Join-Path $taskFixture 'Domain')
$taskExpansion = if ($CoreOnly) { '' } else { '<li>ludeon.rimworld.biotech</li>' }
('<ModsConfigData><version>' + $taskMarker.version.Split(' ')[0] + '</version><activeMods><li>brrainz.harmony</li><li>ludeon.rimworld</li>' + $taskExpansion + '<li>hailuan.customquestframework</li><li>cqf.optimizationchecks</li><li>trigger.logsyncmod</li></activeMods><knownExpansions><li>ludeon.rimworld.royalty</li><li>ludeon.rimworld.ideology</li><li>ludeon.rimworld.biotech</li><li>ludeon.rimworld.anomaly</li><li>ludeon.rimworld.odyssey</li></knownExpansions></ModsConfigData>') | Set-Content -LiteralPath (Join-Path $taskSave 'Config/ModsConfig.xml') -Encoding UTF8
'<SettingsBlock><ModSettings Class="QuestEditor_Library.CustomQuestFramework_ModSetting"><enableEditor>True</enableEditor><dialogAIEnabled>False</dialogAIEnabled></ModSettings></SettingsBlock>' | Set-Content -LiteralPath (Join-Path $taskSave 'Config/Mod_CQF_CustomQuestFramework_Mod.xml') -Encoding UTF8
'<PrefsData><screenWidth>1280</screenWidth><screenHeight>800</screenHeight><fullscreen>False</fullscreen><uiScale>1</uiScale><runInBackground>True</runInBackground><volumeMaster>0</volumeMaster><pauseOnError>False</pauseOnError><pauseOnLoad>False</pauseOnLoad><devMode>True</devMode></PrefsData>' | Set-Content -LiteralPath (Join-Path $taskSave 'Config/Prefs.xml') -Encoding UTF8
$taskConfig = @{
    config_version = 2; mod_name = 'CQF'; game_version = '1.7'; rimworld_dir = $taskGame
    mods_config_path = (Join-Path $taskSave 'Config/ModsConfig.xml'); csproj_paths = @(); sync_items = @()
    workshop_paths = @(); log_keywords = @('CQF', 'QuestEditor_Library'); min_active_mods = @('brrainz.harmony', 'ludeon.rimworld', 'hailuan.customquestframework', 'trigger.logsyncmod')
}
$taskConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskRun 'mod_config.jsonc') -Encoding UTF8
Push-Location $taskRun
try {
    $taskDoctor = & 'D:/工作间/AI/RimDevTools/artifacts/RimDevTools17/.tools/RimDevTools.exe' doctor --verbose
    $taskDoctor | Set-Content -LiteralPath (Join-Path $taskRun 'Doctor.json') -Encoding UTF8
    if (-not (($taskDoctor | ConvertFrom-Json).result.ready)) { throw ('RimDevTools doctor is not ready: ' + $taskRun) }
} finally { Pop-Location }
Write-Output ('RUN_DIRECTORY=' + $taskRun)
foreach ($taskPhase in @('initial', 'restart')) {
    $taskArguments = @('-quicktest', '-cqfoptimizationchecks')
    if ($taskPhase -eq 'restart') { $taskArguments += '-cqfoptimizationreload' }
    $taskProcess = & (Join-Path $taskGame 'Start-OfflineTest.ps1') -SaveDataPath $taskSave -GameArguments $taskArguments
    try {
        $taskTimer = [Diagnostics.Stopwatch]::StartNew()
        while (-not $taskProcess.HasExited) {
            if ($taskTimer.Elapsed.TotalSeconds -gt $TimeoutSeconds) { throw ('Optimization runtime timed out: ' + $taskRun) }
            Start-Sleep -Seconds 1
            $taskProcess.Refresh()
        }
        $taskProcess.WaitForExit()
        Copy-Item -LiteralPath (Join-Path $taskSave 'offline-stdout.log') -Destination (Join-Path $taskRun ($taskPhase + '-stdout.log'))
        Copy-Item -LiteralPath (Join-Path $taskSave 'Process.json') -Destination (Join-Path $taskRun ($taskPhase + '-process.json'))
        foreach ($taskError in Select-String -LiteralPath (Join-Path $taskRun ($taskPhase + '-stdout.log')) -Pattern '^\d\d:\d\d:\d\d ERROR ') {
            if ($taskError.Line -match 'loader_get_json: Failed to open JSON file .*EOSOverlayVkLayer-') { Write-Warning $taskError.Line }
            else { throw ('Unexpected native game error: ' + $taskError.Line) }
        }
        $taskResults = Get-Content -LiteralPath (Join-Path $taskSave 'OptimizationChecks.txt') -Encoding UTF8
        $taskExpectedCompletions = if ($taskPhase -eq 'initial') { 1 } else { 2 }
        if ($taskProcess.ExitCode -ne 0 -or @($taskResults | Where-Object { $_ -eq 'COMPLETE' }).Count -ne $taskExpectedCompletions -or $taskResults -match '^FAIL ') { throw ('Optimization runtime failed: ' + $taskRun) }
        Write-Output ($taskPhase + ': COMPLETE')
    } finally {
        if (-not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force; $taskProcess.WaitForExit() }
    }
}
Get-Content -LiteralPath (Join-Path $taskSave 'OptimizationChecks.txt') -Encoding UTF8
