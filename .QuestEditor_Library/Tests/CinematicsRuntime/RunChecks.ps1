param(
    [Parameter(Mandatory = $true)][string]$CoreAssembly,
    [Parameter(Mandatory = $true)][string]$EditorAssembly,
    [string]$GameDirectory = 'D:/RimWorld-Dev',
    [string]$RunRoot = '',
    [string]$Dotnet = 'C:/Users/24904/AppData/Local/HaiLuan/dotnet/dotnet.exe',
    [int]$TimeoutSeconds = 360,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$taskGameRoot = [IO.Path]::GetFullPath($GameDirectory)
$taskWorkspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$taskCore = (Resolve-Path -LiteralPath $CoreAssembly).Path
$taskEditor = (Resolve-Path -LiteralPath $EditorAssembly).Path
$taskRunsDirectory = if ([string]::IsNullOrWhiteSpace($RunRoot)) { Join-Path $taskGameRoot 'SaveData/TestRuns' } else { [IO.Path]::GetFullPath($RunRoot) }
$taskRunRoot = Join-Path $taskRunsDirectory ('CQF_Cinematics_' + [guid]::NewGuid().ToString('N'))
$taskSnapshot = Join-Path $taskRunRoot 'GameSnapshot'
$taskSaveRoot = Join-Path $taskRunRoot 'SaveData'
$taskProvider = Join-Path $taskSnapshot 'Mods/Custom-Quest-Framework'
$taskTestMod = Join-Path $taskSnapshot 'Mods/CQF_CinematicsChecks'
$taskBuild = Join-Path $taskRunRoot 'Build'
$taskHarmony = Join-Path $taskGameRoot 'Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll'
$taskProcess = $null
if ($Launch) {
    $taskRunningGames = @(Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'")
    if ($taskRunningGames.ExecutablePath -contains (Join-Path $taskSnapshot 'RimWorld.exe')) { throw 'The target snapshot already has a running RimWorld process.' }
}
foreach ($taskRequired in @((Join-Path $taskGameRoot 'RimWorld.exe'), $taskHarmony, $Dotnet)) {
    if (-not (Test-Path -LiteralPath $taskRequired)) { throw "Required runtime check input is missing: $taskRequired" }
}
New-Item -ItemType Directory -Path $taskSnapshot, $taskBuild, (Join-Path $taskSaveRoot 'Config'), (Join-Path $taskSnapshot 'Mods') -Force | Out-Null
& $Dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet -p:WarningLevel=0 "-p:CQFCoreAssembly=$taskCore" "-p:RimWorldDir=$taskGameRoot" "-p:HarmonyPath=$taskHarmony" "-p:OutputPath=$taskBuild/" -p:AppendTargetFrameworkToOutputPath=false
if ($LASTEXITCODE -ne 0) { throw "Cinematic runtime fixture compilation failed. Run directory: $taskRunRoot" }
foreach ($taskFile in Get-ChildItem -LiteralPath $taskGameRoot -File) {
    if ($taskFile.Name -eq 'Launch-RimWorldDev.cmd') { continue }
    if ([IO.Path]::GetPathRoot($taskSnapshot) -eq [IO.Path]::GetPathRoot($taskGameRoot)) {
        New-Item -ItemType HardLink -Path (Join-Path $taskSnapshot $taskFile.Name) -Target $taskFile.FullName | Out-Null
    } else {
        Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskSnapshot $taskFile.Name)
    }
}
foreach ($taskDirectory in @('Data', 'Rendering', 'vulkan')) {
    New-Item -ItemType Junction -Path (Join-Path $taskSnapshot $taskDirectory) -Target (Join-Path $taskGameRoot $taskDirectory) | Out-Null
}
New-Item -ItemType Directory -Path $taskProvider, (Join-Path $taskTestMod 'About'), (Join-Path $taskTestMod 'Assemblies') -Force | Out-Null
foreach ($taskContent in @('About', 'Languages', 'Quests', 'Textures', '1.7', 'LoadFolders.xml')) {
    Copy-Item -LiteralPath (Join-Path $taskWorkspace $taskContent) -Destination $taskProvider -Recurse
}
Copy-Item -LiteralPath (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $taskHarmony))) -Destination (Join-Path $taskSnapshot 'Mods/Harmony') -Recurse
$taskProviderAssemblies = Join-Path $taskProvider '1.7/Assemblies'
if (Test-Path -LiteralPath $taskProviderAssemblies) {
    foreach ($taskExistingAssembly in Get-ChildItem -LiteralPath $taskProviderAssemblies -File -Recurse -Filter '*.dll') {
        $taskResolvedAssembly = [IO.Path]::GetFullPath($taskExistingAssembly.FullName)
        if (-not $taskResolvedAssembly.StartsWith([IO.Path]::GetFullPath($taskProviderAssemblies) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected provider snapshot assembly path.' }
        Remove-Item -LiteralPath $taskResolvedAssembly
    }
}
New-Item -ItemType Directory -Path (Join-Path $taskProvider '1.7/Assemblies/net10.0'), (Join-Path $taskProvider '1.7/Optional/Editor') -Force | Out-Null
Copy-Item -LiteralPath $taskCore -Destination (Join-Path $taskProvider '1.7/Assemblies/net10.0/QuestEditor_Library.dll') -Force
Copy-Item -LiteralPath $taskEditor -Destination (Join-Path $taskProvider '1.7/Optional/Editor/CQF.Editor.dll') -Force
Copy-Item -LiteralPath (Join-Path $taskBuild 'CQF.CinematicsRuntimeChecks.dll') -Destination (Join-Path $taskTestMod 'Assemblies')
'<ModMetaData><name>CQF.CinematicsChecks</name><author>HaiLuan</author><packageId>CQF.CinematicsChecks</packageId><supportedVersions><li>1.7</li></supportedVersions><modDependencies><li><packageId>HaiLuan.CustomQuestFramework</packageId><displayName>Custom Quest Framework</displayName></li></modDependencies></ModMetaData>' |
    Set-Content -LiteralPath (Join-Path $taskTestMod 'About/About.xml') -Encoding utf8
$taskVersion = (Get-Content -LiteralPath (Join-Path $taskGameRoot 'Version.txt') -Encoding utf8 -TotalCount 1).Split(' ')[0]
('<ModsConfigData><version>' + $taskVersion + '</version><activeMods><li>brrainz.harmony</li><li>ludeon.rimworld</li><li>hailuan.customquestframework</li><li>cqf.cinematicschecks</li></activeMods><knownExpansions /></ModsConfigData>') |
    Set-Content -LiteralPath (Join-Path $taskSaveRoot 'Config/ModsConfig.xml') -Encoding utf8
'<SettingsBlock><ModSettings Class="QuestEditor_Library.CustomQuestFramework_ModSetting"><enableEditor>True</enableEditor><dialogAIEnabled>False</dialogAIEnabled></ModSettings></SettingsBlock>' |
    Set-Content -LiteralPath (Join-Path $taskSaveRoot 'Config/Mod_Custom-Quest-Framework_CustomQuestFramework_Mod.xml') -Encoding utf8
'<PrefsData><screenWidth>1920</screenWidth><screenHeight>1080</screenHeight><fullscreen>False</fullscreen><uiScale>1</uiScale><runInBackground>True</runInBackground><volumeMaster>0</volumeMaster><volumeGame>0</volumeGame><volumeMusic>0</volumeMusic><volumeAmbient>0</volumeAmbient><volumeUI>0</volumeUI><pauseOnError>False</pauseOnError><pauseOnLoad>False</pauseOnLoad><devMode>True</devMode></PrefsData>' |
    Set-Content -LiteralPath (Join-Path $taskSaveRoot 'Config/Prefs.xml') -Encoding utf8
$taskManifest = [ordered]@{
    RunDirectory = $taskRunRoot
    GameSnapshot = $taskSnapshot
    SaveData = $taskSaveRoot
    CoreSource = $taskCore
    EditorSource = $taskEditor
    CoreHash = (Get-FileHash -LiteralPath $taskCore -Algorithm SHA256).Hash
    EditorHash = (Get-FileHash -LiteralPath $taskEditor -Algorithm SHA256).Hash
    GameVersion = $taskVersion
}
$taskManifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRunRoot 'RunManifest.json') -Encoding utf8
$taskManifest | ConvertTo-Json -Compress
if (-not $Launch) { return }
$taskPreviousSteamApp = $env:SteamAppId
$taskPreviousSteamGame = $env:SteamGameId
$taskPreviousSteamOverlay = $env:SteamOverlayGameId
try {
    $env:SteamAppId = '0'
    $env:SteamGameId = '0'
    $env:SteamOverlayGameId = '0'
    $taskArguments = @('-NoWindow', '-batchmode', '-quicktest', '-cqfcinematicchecks', "-savedatafolder=`"$taskSaveRoot`"")
    $taskProcess = Start-Process -FilePath (Join-Path $taskSnapshot 'RimWorld.exe') -WorkingDirectory $taskSnapshot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $taskRunRoot 'stdout.log') -RedirectStandardError (Join-Path $taskRunRoot 'stderr.log')
    $taskWatch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $taskProcess.HasExited) {
        if ($taskWatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
            Stop-Process -Id $taskProcess.Id -Force
            throw "Cinematic runtime checks timed out. Evidence retained: $taskRunRoot"
        }
        Start-Sleep -Seconds 2
        $taskProcess.Refresh()
    }
    $taskProcess.WaitForExit()
    $taskResults = Join-Path $taskSaveRoot 'CinematicsChecks.txt'
    if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults -Encoding utf8 }
    if ($taskProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $taskResults) -or -not (Select-String -LiteralPath $taskResults -Pattern '^COMPLETE$' -Quiet)) {
        throw "Cinematic runtime checks failed with exit code $($taskProcess.ExitCode). Evidence retained: $taskRunRoot"
    }
    $taskLoggedErrors = @(Select-String -LiteralPath (Join-Path $taskRunRoot 'stdout.log') -Encoding utf8 -Pattern '^\d{2}:\d{2}:\d{2}\s+ERROR\b')
    $taskUnexpectedErrors = @($taskLoggedErrors | Where-Object { $_.Line -notmatch 'ERROR \[CQF\] Cinematic playback failed: CQF_Runtime_InstantFailure, step=0$' })
    if ($taskUnexpectedErrors.Count -gt 0 -or $taskLoggedErrors.Count -ne 1) {
        throw "Unexpected runtime errors or missing expected negative-test error. Evidence retained: $taskRunRoot"
    }
} finally {
    if ($taskProcess -and -not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force; $taskProcess.WaitForExit() }
    $env:SteamAppId = $taskPreviousSteamApp
    $env:SteamGameId = $taskPreviousSteamGame
    $env:SteamOverlayGameId = $taskPreviousSteamOverlay
}
