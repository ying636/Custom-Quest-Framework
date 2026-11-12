param(
    [Parameter(Mandatory = $true)][string]$RunRoot,
    [string]$Dotnet = 'D:/Codex/tools/dotnet-sdk-10.0.402/dotnet.exe',
    [string]$RimDevTools = 'C:/Users/24904/Desktop/22的玄秘工坊/生图流程/工具/RimDevTools/RimDevTools/bin/Release/net8.0/RimDevTools.exe',
    [switch]$Launch
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath($RunRoot)
$taskGame = 'D:/RimWorld-Dev'
$taskProject = Join-Path $taskRoot 'RimDevProject'
$taskFixture = Join-Path $taskRoot 'Fixture'
$taskOutput = Join-Path $taskRoot 'Evidence'
$taskTool = Join-Path $taskRoot 'LogSyncMod'
$taskCore = Join-Path $taskRoot 'CQF/1.7/Assemblies/net10.0/QuestEditor_Library.dll'
$taskPool = Join-Path $taskRoot 'Poolcore/1.7/Assemblies/Poolcore.dll'
if ($Launch -and (Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'" | Where-Object { $_.ExecutablePath -eq 'D:\RimWorld-Dev\RimWorld.exe' })) { throw 'The offline development copy is already running.' }
if ((Get-Content -LiteralPath (Join-Path $taskGame 'steam_appid.txt') -Raw -Encoding utf8).Trim() -ne '0') { throw 'Offline steam_appid.txt is not zero.' }
New-Item -ItemType Directory -Path (Join-Path $taskProject 'About'),(Join-Path $taskFixture 'About'),(Join-Path $taskFixture 'Defs'),(Join-Path $taskFixture 'Assemblies'),$taskOutput -Force | Out-Null
& $Dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet "-p:CoreAssembly=$taskCore" "-p:PoolcoreAssembly=$taskPool" "-p:OutputPath=$taskFixture/Assemblies/" *> (Join-Path $taskRoot 'build-fixture.log')
if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath (Join-Path $taskRoot 'build-fixture.log') -Encoding utf8; throw 'Panorama fixture compilation failed.' }
Copy-Item -LiteralPath (Join-Path $taskRoot 'CQF/About/About.xml') -Destination (Join-Path $taskProject 'About/About.xml')
'<ModMetaData><name>CQF_PanoramaLightingChecks</name><author>24904</author><packageId>local.cqfpanoramalightingchecks</packageId><supportedVersions><li>1.7</li></supportedVersions><loadAfter><li>local.poolcore</li></loadAfter></ModMetaData>' | Set-Content -LiteralPath (Join-Path $taskFixture 'About/About.xml') -Encoding utf8
$taskStar = Get-Content -LiteralPath (Join-Path $taskRoot 'CQF/Quests/Map/Star.xml') -Raw -Encoding utf8
$taskStar.Replace('<defName>Start</defName>', '<defName>CQF_PL_Star</defName>') | Set-Content -LiteralPath (Join-Path $taskFixture 'Defs/Star.xml') -Encoding utf8
@{
    config_version = 2
    mod_name = 'Custom-Quest-Framework'
    game_version = '1.7'
    rimworld_dir = $taskGame
    mods_config_path = 'D:/RimWorld-Dev/SaveData/Config/ModsConfig.xml'
    game_arguments = @('-savedatafolder=D:/RimWorld-Dev/SaveData','-NoFileLog','-quicktest','-cqfpanoramachecks',('-cqfpanoramaoutput=' + $taskOutput))
    offline = $true
    workshop_paths = @('D:/Steam/steamapps/workshop/content/294100')
    csproj_paths = @()
    sync_items = @()
    log_keywords = @('CQF','PanoramaLightingChecks','PC_')
    min_active_mods = @('brrainz.harmony','ludeon.rimworld','ludeon.rimworld.royalty','ludeon.rimworld.ideology','ludeon.rimworld.biotech','ludeon.rimworld.anomaly','ludeon.rimworld.odyssey','hailuan.customquestframework','local.poolcore','local.cqfpanoramalightingchecks','trigger.logsyncmod')
    timeouts = @{ max_wait = 1200; existing_game_exit = 5; post_exit_delay = 2; steam_launch_detect = 30 }
    isolated_desktop = $true
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskProject 'mod_config.jsonc') -Encoding utf8
@{ CoreSHA256 = (Get-FileHash -LiteralPath $taskCore).Hash; PoolcoreSHA256 = (Get-FileHash -LiteralPath $taskPool).Hash; RunRoot = $taskRoot } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'manifest.json') -Encoding utf8
if (-not $Launch) { Write-Output $taskRoot; return }
$taskPrefs = Join-Path $taskGame 'SaveData/Config/Prefs.xml'
$taskProfiler = Join-Path $taskGame 'SaveData/Config/Profiler.xml'
Copy-Item -LiteralPath $taskPrefs -Destination (Join-Path $taskRoot 'Prefs.original.xml')
$taskPrefsHash = (Get-FileHash -LiteralPath $taskPrefs).Hash
$taskProfilerExists = Test-Path -LiteralPath $taskProfiler
if ($taskProfilerExists) { Copy-Item -LiteralPath $taskProfiler -Destination (Join-Path $taskRoot 'Profiler.original.xml') }
$taskFixtureLink = Join-Path $taskGame 'Mods/CQF_PanoramaLightingChecks'
if (Test-Path -LiteralPath $taskFixtureLink) { throw 'Panorama fixture link already exists.' }
$taskTargets = @{}
foreach ($taskName in @('Custom-Quest-Framework','Poolcore','LogSyncMod')) {
    $taskItem = Get-Item -LiteralPath (Join-Path $taskGame ('Mods/' + $taskName))
    if ($taskItem.LinkType -ne 'Junction') { throw ('Expected development junction: ' + $taskItem.FullName) }
    $taskTargets[$taskName] = $taskItem.Target
}
New-Item -ItemType Directory -Path $taskTool -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $taskGame 'Mods/LogSyncMod') -Force | Copy-Item -Destination $taskTool -Recurse -Force
$taskCleanAssemblies = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $RimDevTools) '../../../../1.7/Assemblies'))
foreach ($taskExtension in @('dll','pdb')) { Copy-Item -LiteralPath (Join-Path $taskCleanAssemblies ('AutoTestRunner.' + $taskExtension)) -Destination (Join-Path $taskTool '1.7/Assemblies') -Force }
$taskMirrors = @{ 'Custom-Quest-Framework' = (Join-Path $taskRoot 'CQF'); 'Poolcore' = (Join-Path $taskRoot 'Poolcore'); 'LogSyncMod' = $taskTool }
@{ Original = $taskTargets; Test = $taskMirrors; PrefsSHA256 = $taskPrefsHash; ProfilerExisted = $taskProfilerExists } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskRoot 'Recovery.json') -Encoding utf8
$taskSwitched = @()
Push-Location $taskProject
try {
    foreach ($taskName in $taskMirrors.Keys) {
        $taskLink = Join-Path $taskGame ('Mods/' + $taskName)
        [IO.Directory]::Delete($taskLink)
        New-Item -ItemType Junction -Path $taskLink -Target $taskMirrors[$taskName] | Out-Null
        $taskSwitched += $taskName
    }
    New-Item -ItemType Junction -Path $taskFixtureLink -Target $taskFixture | Out-Null
    $taskDoctor = & $RimDevTools doctor
    $taskDoctor | Set-Content -LiteralPath (Join-Path $taskRoot 'doctor.json') -Encoding utf8
    if ($LASTEXITCODE -ne 0 -or -not ($taskDoctor | ConvertFrom-Json).result.ready) { throw ('RimDevTools doctor failed: ' + $taskRoot) }
    $taskReceipt = & $RimDevTools test run --mode manual --isolated-desktop --timeout-seconds 1200
    $taskReceipt | Set-Content -LiteralPath (Join-Path $taskRoot 'receipt.json') -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw ('RimDevTools session failed: ' + $taskRoot) }
    $taskProcess = Get-Content -LiteralPath (Join-Path $taskOutput 'Process.json') -Raw -Encoding utf8 | ConvertFrom-Json
    if ([IO.Path]::GetFullPath($taskProcess.ExecutablePath) -ne 'D:\RimWorld-Dev\RimWorld.exe' -or [IO.Path]::GetFullPath($taskProcess.SaveData).TrimEnd('\','/') -ne 'D:\RimWorld-Dev\SaveData') { throw 'Runtime isolation mismatch.' }
    if ($taskProcess.SteamAppId -ne '0' -or $taskProcess.SteamGameId -ne '0' -or $taskProcess.SteamOverlayGameId -ne '0') { throw 'Runtime offline environment mismatch.' }
    $taskResults = Get-Content -LiteralPath (Join-Path $taskOutput 'Checks.txt') -Encoding utf8
    if ($taskResults -notcontains 'COMPLETE' -or $taskResults -match '^FAIL ') { throw ('Checks incomplete: ' + $taskOutput) }
    $taskNativeLog = ($taskReceipt | ConvertFrom-Json).result.consoleLogPath
    if (-not $taskNativeLog -or -not (Test-Path -LiteralPath $taskNativeLog) -or (Get-Item -LiteralPath $taskNativeLog).Length -eq 0) { throw 'Native console log missing or empty.' }
    if (Select-String -LiteralPath $taskNativeLog -Pattern '\bERROR\b' -CaseSensitive -Quiet) { throw ('Native game errors: ' + $taskNativeLog) }
    Write-Output ($taskResults | Select-Object -Last 5)
} finally {
    Pop-Location
    $taskRunning = Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'" | Where-Object { $_.ExecutablePath -eq 'D:\RimWorld-Dev\RimWorld.exe' }
    if ($taskRunning) { throw ('Test game remains active; recovery records retained: ' + $taskRoot) }
    foreach ($taskName in $taskSwitched) {
        $taskLink = Join-Path $taskGame ('Mods/' + $taskName)
        $taskItem = Get-Item -LiteralPath $taskLink
        if ($taskItem.LinkType -ne 'Junction' -or $taskItem.Target -ne $taskMirrors[$taskName]) { throw ('Junction identity changed: ' + $taskLink) }
        [IO.Directory]::Delete($taskLink)
        New-Item -ItemType Junction -Path $taskLink -Target $taskTargets[$taskName] | Out-Null
    }
    if (Test-Path -LiteralPath $taskFixtureLink) {
        $taskItem = Get-Item -LiteralPath $taskFixtureLink
        if ($taskItem.LinkType -ne 'Junction' -or $taskItem.Target -ne $taskFixture) { throw 'Fixture identity changed.' }
        [IO.Directory]::Delete($taskFixtureLink)
    }
    Copy-Item -LiteralPath (Join-Path $taskRoot 'Prefs.original.xml') -Destination $taskPrefs -Force
    if ((Get-FileHash -LiteralPath $taskPrefs).Hash -ne $taskPrefsHash) { throw 'Prefs restoration hash mismatch.' }
    if ($taskProfilerExists) { Copy-Item -LiteralPath (Join-Path $taskRoot 'Profiler.original.xml') -Destination $taskProfiler -Force }
    elseif (Test-Path -LiteralPath $taskProfiler) { Move-Item -LiteralPath $taskProfiler -Destination (Join-Path $taskRoot 'Profiler.generated.xml') }
    $taskConsole = @()
    if (Test-Path -LiteralPath (Join-Path $taskProject '.tools/log')) {
        $taskConsole = @(Get-ChildItem -LiteralPath (Join-Path $taskProject '.tools/log') -Filter 'GameConsole_*.log' -File)
    }
    foreach ($taskLog in $taskConsole) { Copy-Item -LiteralPath $taskLog.FullName -Destination (Join-Path $taskRoot $taskLog.Name) }
    @{ PrefsRestored = $true; JunctionsRestored = $taskSwitched; ProfilerRestored = $true } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'Cleanup.json') -Encoding utf8
}
