param(
    [string]$RunRoot = 'D:/RimWorld-Dev/TestWorkspaces/CQFMapProjections',
    [ValidateSet('English','ChineseSimplified')][string]$Language = 'ChineseSimplified',
    [switch]$CameraPlus,
    [switch]$Profile,
    [switch]$CorePerformance,
    [switch]$DomainPerformance,
    [switch]$DomainGameplay,
    [ValidateSet('','MapLayouts','DialogPresentation')][string]$ContentChecks = '',
    [switch]$EnableEditor,
    [ValidateSet('None','Domain','MapProjections','Both')][string]$Modules = 'MapProjections',
    [string]$CoreAssemblyOverride = '',
    [string]$Dotnet = 'D:/Codex/tools/dotnet-sdk-10.0.402/dotnet.exe',
    [string]$RimDevTools = 'C:/Users/24904/Desktop/22的玄秘工坊/生图流程/工具/RimDevTools/RimDevTools/bin/Release/net8.0/RimDevTools.exe'
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath($RunRoot)
$taskGame = 'D:/RimWorld-Dev'
$taskSource = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$taskCandidate = Join-Path $taskRoot 'CQF'
$taskFixture = Join-Path $taskRoot 'Fixture'
$taskProject = Join-Path $taskRoot 'RimDevProject'
$taskOutput = Join-Path $taskRoot ('Evidence-' + [guid]::NewGuid().ToString('N'))
$taskFeatureEnabled = $Modules -in @('MapProjections','Both')
if ($DomainGameplay) { $DomainPerformance = $true }
if ($ContentChecks) { $EnableEditor = $true }
if ($ContentChecks -and $Modules -ne 'Domain') { throw 'Content checks require -Modules Domain.' }
if ($CorePerformance -and $Modules -ne 'None') { throw 'Core performance checks require -Modules None.' }
if ($CorePerformance -or $DomainPerformance) { $Profile = $true }
if ($DomainPerformance -and ($Modules -ne 'Domain' -or $CorePerformance)) { throw 'Domain performance checks require -Modules Domain without -CorePerformance.' }
if (Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'" | Where-Object { $_.ExecutablePath -eq 'D:\RimWorld-Dev\RimWorld.exe' }) { throw 'The offline development copy is already running.' }
if ((Get-Content -LiteralPath (Join-Path $taskGame 'steam_appid.txt') -Raw -Encoding utf8).Trim() -ne '0') { throw 'Offline AppId must be zero.' }
New-Item -ItemType Directory -Path $taskCandidate,(Join-Path $taskFixture 'About'),(Join-Path $taskProject 'About'),$taskOutput -Force | Out-Null
Get-ChildItem -LiteralPath $taskSource -Force | Where-Object { $_.Name -notin @('.git','.QuestEditor_Library','1.4','1.5','1.6') } | Copy-Item -Destination $taskCandidate -Recurse -Force
& $Dotnet build (Join-Path $PSScriptRoot '../../QuestEditor_Library/QuestEditor_Library.csproj') -c Release -v quiet "-p:RimWorldDir=$taskGame" "-p:HarmonyPath=$taskGame/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:OutputPath=$taskCandidate/1.7/Assemblies/net10.0/" -p:AppendTargetFrameworkToOutputPath=false *> (Join-Path $taskOutput 'build-core.log')
if ($LASTEXITCODE -ne 0) { throw ('Core compilation failed: ' + $taskOutput) }
if ($CoreAssemblyOverride) {
    Copy-Item -LiteralPath $CoreAssemblyOverride -Destination (Join-Path $taskCandidate '1.7/Assemblies/net10.0/QuestEditor_Library.dll') -Force
    Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($CoreAssemblyOverride, '.pdb')) -Destination (Join-Path $taskCandidate '1.7/Assemblies/net10.0/QuestEditor_Library.pdb') -Force
}
$taskProjection = Join-Path $taskCandidate '1.7/Optional/MapProjections'
& $Dotnet build (Join-Path $PSScriptRoot '../../CQF.MapProjections/CQF.MapProjections.csproj') -c Release -v quiet "-p:RimWorldDir=$taskGame" "-p:HarmonyPath=$taskGame/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:CoreAssembly=$taskCandidate/1.7/Assemblies/net10.0/QuestEditor_Library.dll" "-p:OutputPath=$taskProjection/" *> (Join-Path $taskOutput 'build-projections.log')
if ($LASTEXITCODE -ne 0) { throw ('Projection module compilation failed: ' + $taskOutput) }
$taskDomain = Join-Path $taskCandidate '1.7/Optional/Domain'
if ($DomainPerformance) {
    & $Dotnet build (Join-Path $PSScriptRoot '../../CQF.Domain/CQF.Domain.csproj') -c Release -v quiet "-p:RimWorldDir=$taskGame" "-p:HarmonyPath=$taskGame/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:CoreAssembly=$taskCandidate/1.7/Assemblies/net10.0/QuestEditor_Library.dll" "-p:OutputPath=$taskDomain/" *> (Join-Path $taskOutput 'build-domain.log')
    if ($LASTEXITCODE -ne 0) { throw ('Domain module compilation failed: ' + $taskOutput) }
}
$taskFixtureProject = if($ContentChecks -eq 'MapLayouts') { Join-Path $PSScriptRoot '../MapLayoutsRuntime/Checks.csproj' } elseif($ContentChecks -eq 'DialogPresentation') { Join-Path $PSScriptRoot '../DialogPresentationRuntime/Checks.csproj' } elseif($DomainPerformance) { Join-Path $PSScriptRoot '../DomainPerformanceRuntime/Checks.csproj' } elseif($CorePerformance) { Join-Path $PSScriptRoot '../CorePerformanceRuntime/Checks.csproj' } elseif($taskFeatureEnabled) { Join-Path $PSScriptRoot 'Checks.csproj' } else { Join-Path $PSScriptRoot '../ProjectionModulesRuntime/Checks.csproj' }
$taskFixtureFolder = if($taskFeatureEnabled -or $DomainPerformance -or $ContentChecks) { 'Checks' } else { 'Assemblies' }
& $Dotnet build $taskFixtureProject -c Release -v quiet "-p:RimWorldDir=$taskGame" "-p:HarmonyPath=$taskGame/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:CoreAssembly=$taskCandidate/1.7/Assemblies/net10.0/QuestEditor_Library.dll" "-p:CQFCoreAssembly=$taskCandidate/1.7/Assemblies/net10.0/QuestEditor_Library.dll" "-p:CQFEditorAssembly=$taskCandidate/1.7/Optional/Editor/CQF.Editor.dll" "-p:ProjectionAssembly=$taskProjection/CQF.MapProjections.dll" "-p:DomainAssembly=$taskDomain/CQF.Domain.dll" "-p:OutputPath=$taskFixture/$taskFixtureFolder/" *> (Join-Path $taskOutput 'build-fixture.log')
if ($LASTEXITCODE -ne 0) { throw ('Fixture compilation failed: ' + $taskOutput) }
'<ModMetaData><name>CQF_MapProjectionChecks</name><author>24904</author><packageId>local.cqfprojectionchecks</packageId><supportedVersions><li>1.7</li></supportedVersions></ModMetaData>' | Set-Content -LiteralPath (Join-Path $taskFixture 'About/About.xml') -Encoding utf8
$taskRequest = switch($Modules) { 'MapProjections' { 'MapProjections' }; 'Both' { 'Domain,MapProjections' }; 'Domain' { 'Domain' }; default { '' } }
$taskDeclaration = if($taskFeatureEnabled -or $DomainPerformance -or $ContentChecks) { '<li RequiresCQFModule="' + $taskRequest + '">Checks</li>' } elseif($taskRequest) { New-Item -ItemType Directory -Path (Join-Path $taskFixture 'Request') -Force | Out-Null; '<li RequiresCQFModule="' + $taskRequest + '">Request</li>' } else { '' }
('<loadFolders><v1.7><li>/</li>' + $taskDeclaration + '</v1.7></loadFolders>') | Set-Content -LiteralPath (Join-Path $taskFixture 'LoadFolders.xml') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $taskCandidate 'About/About.xml') -Destination (Join-Path $taskProject 'About/About.xml') -Force
$taskMods = @('brrainz.harmony','ludeon.rimworld','ludeon.rimworld.royalty','ludeon.rimworld.ideology','ludeon.rimworld.biotech','ludeon.rimworld.anomaly','ludeon.rimworld.odyssey','hailuan.customquestframework','local.cqfprojectionchecks','trigger.logsyncmod')
if ($CameraPlus) { $taskMods += 'brrainz.cameraplus' }
$taskArguments = @('-savedatafolder=D:/RimWorld-Dev/SaveData','-NoFileLog','-quicktest','-cqfprojectionchecks',('-cqfprojectionmodules=' + $Modules),('-cqfprojectionoutput=' + $taskOutput))
if ($Profile) { $taskArguments += '-cqfprojectionprofile' }
if ($CorePerformance) { $taskArguments += '-cqfcoreperformancechecks' }
if ($DomainPerformance) { $taskArguments += '-cqfdomainperformancechecks' }
if ($DomainGameplay) { $taskArguments += '-cqfdomaingameplaychecks' }
if ($ContentChecks -eq 'MapLayouts') { $taskArguments += '-cqfmaplayoutchecks' }
if ($ContentChecks -eq 'DialogPresentation') { $taskArguments += '-cqfdialogpresentationchecks' }
@{
    config_version = 2; mod_name = 'Custom-Quest-Framework'; game_version = '1.7'; rimworld_dir = $taskGame
    mods_config_path = 'D:/RimWorld-Dev/SaveData/Config/ModsConfig.xml'
    game_arguments = $taskArguments
    offline = $true; workshop_paths = @('D:/Steam/steamapps/workshop/content/294100'); csproj_paths = @(); sync_items = @()
    log_keywords = @('CQF','MapProjectionChecks'); min_active_mods = $taskMods; isolated_desktop = $true
    timeouts = @{ max_wait = 480; existing_game_exit = 5; post_exit_delay = 2; steam_launch_detect = 30 }
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskProject 'mod_config.jsonc') -Encoding utf8
$taskPrefsPath = Join-Path $taskGame 'SaveData/Config/Prefs.xml'
$taskSetting = Join-Path $taskGame 'SaveData/Config/Mod_Custom-Quest-Framework_CustomQuestFramework_Mod.xml'
$taskBackups = @{}
$taskProfilerPath = Join-Path $taskGame 'SaveData/Config/Profiler.xml'
$taskProfilerExisted = Test-Path -LiteralPath $taskProfilerPath
foreach ($taskFile in @($taskPrefsPath,$taskSetting)) {
    if (-not (Test-Path -LiteralPath $taskFile)) { throw ('Required test configuration missing: ' + $taskFile) }
    $taskBackup = Join-Path $taskOutput ([IO.Path]::GetFileName($taskFile) + '.original')
    Copy-Item -LiteralPath $taskFile -Destination $taskBackup
    $taskBackups[$taskFile] = @{ Path = $taskBackup; SHA256 = (Get-FileHash -LiteralPath $taskFile).Hash }
}
if ($Profile -and $taskProfilerExisted) {
    $taskBackup = Join-Path $taskOutput 'Profiler.xml.original'
    Copy-Item -LiteralPath $taskProfilerPath -Destination $taskBackup
    $taskBackups[$taskProfilerPath] = @{ Path = $taskBackup; SHA256 = (Get-FileHash -LiteralPath $taskProfilerPath).Hash }
}
$taskTool = Join-Path $taskRoot 'LogSyncMod'
New-Item -ItemType Directory -Path $taskTool -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $taskGame 'Mods/LogSyncMod') -Force | Copy-Item -Destination $taskTool -Recurse -Force
$taskClean = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $RimDevTools) '../../../../1.7/Assemblies'))
foreach ($taskExtension in @('dll','pdb')) { Copy-Item -LiteralPath (Join-Path $taskClean ('AutoTestRunner.' + $taskExtension)) -Destination (Join-Path $taskTool '1.7/Assemblies') -Force }
$taskMirrors = @{ 'Custom-Quest-Framework' = $taskCandidate; 'LogSyncMod' = $taskTool }
$taskOriginal = @{}
foreach ($taskName in $taskMirrors.Keys) {
    $taskLink = Get-Item -LiteralPath (Join-Path $taskGame ('Mods/' + $taskName))
    if ($taskLink.LinkType -ne 'Junction') { throw ('Expected development junction: ' + $taskLink.FullName) }
    $taskOriginal[$taskName] = $taskLink.Target
}
$taskFixtureLink = Join-Path $taskGame 'Mods/CQF_MapProjectionChecks'
if (Test-Path -LiteralPath $taskFixtureLink) { throw 'Projection fixture junction already exists.' }
@{ Original = $taskOriginal; Mirrors = $taskMirrors; Backups = $taskBackups; Output = $taskOutput; ProfilerExisted = $taskProfilerExisted } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskOutput 'Recovery.json') -Encoding utf8
$taskSwitched = @()
if (Test-Path -LiteralPath 'D:/RimWorld-Dev/SaveData/Config/RimDevTools.lock.json') { throw 'Another RimDevTools transaction owns the development environment.' }
if (Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'" | Where-Object { $_.ExecutablePath -eq 'D:\RimWorld-Dev\RimWorld.exe' }) { throw 'The offline development copy became busy during setup.' }
Push-Location $taskProject
try {
    [xml]$taskPrefs = Get-Content -LiteralPath $taskPrefsPath -Raw -Encoding utf8
    $taskLanguageNode = $taskPrefs.SelectSingleNode('/PrefsData/langFolderName')
    if (-not $taskLanguageNode) { $taskLanguageNode = $taskPrefs.CreateElement('langFolderName'); [void]$taskPrefs.DocumentElement.AppendChild($taskLanguageNode) }
    $taskLanguageNode.InnerText = if ($Language -eq 'ChineseSimplified') { 'ChineseSimplified (简体中文)' } else { 'English' }
    $taskPrefs.Save($taskPrefsPath)
    ('<SettingsBlock><ModSettings Class="QuestEditor_Library.CustomQuestFramework_ModSetting"><enableEditor>' + $EnableEditor.IsPresent + '</enableEditor></ModSettings></SettingsBlock>') | Set-Content -LiteralPath $taskSetting -Encoding utf8
    foreach ($taskName in $taskMirrors.Keys) {
        $taskLink = Join-Path $taskGame ('Mods/' + $taskName)
        [IO.Directory]::Delete($taskLink)
        New-Item -ItemType Junction -Path $taskLink -Target $taskMirrors[$taskName] | Out-Null
        $taskSwitched += $taskName
    }
    New-Item -ItemType Junction -Path $taskFixtureLink -Target $taskFixture | Out-Null
    $taskDoctor = & $RimDevTools doctor
    $taskDoctor | Set-Content -LiteralPath (Join-Path $taskOutput 'doctor.json') -Encoding utf8
    if ($LASTEXITCODE -ne 0 -or -not ($taskDoctor | ConvertFrom-Json).result.ready) { throw ('RimDevTools doctor failed: ' + $taskOutput) }
    $taskReceipt = & $RimDevTools test run --mode manual --isolated-desktop --timeout-seconds 480
    $taskReceipt | Set-Content -LiteralPath (Join-Path $taskOutput 'receipt.json') -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw ('RimDevTools session failed: ' + $taskOutput) }
    $taskProcess = Get-Content -LiteralPath (Join-Path $taskOutput 'Process.json') -Raw -Encoding utf8 | ConvertFrom-Json
    if ([IO.Path]::GetFullPath($taskProcess.ExecutablePath) -ne 'D:\RimWorld-Dev\RimWorld.exe' -or [IO.Path]::GetFullPath($taskProcess.SaveData).TrimEnd('/','\') -ne 'D:\RimWorld-Dev\SaveData' -or -not ($taskProcess.CommandLine -contains '-savedatafolder=D:/RimWorld-Dev/SaveData')) { throw 'Runtime isolation mismatch.' }
    if ($taskProcess.SteamAppId -ne '0' -or $taskProcess.SteamGameId -ne '0' -or $taskProcess.SteamOverlayGameId -ne '0') { throw 'Runtime offline environment mismatch.' }
    if ($ContentChecks) {
        $taskContentResult = if ($ContentChecks -eq 'MapLayouts') { 'MapLayoutChecks.txt' } else { 'DialogPresentationChecks.txt' }
        Copy-Item -LiteralPath (Join-Path $taskOutput $taskContentResult) -Destination (Join-Path $taskOutput 'Checks.txt')
    }
    $taskResults = Get-Content -LiteralPath (Join-Path $taskOutput 'Checks.txt') -Encoding utf8
    if ($taskResults -notcontains 'COMPLETE' -or $taskResults -match '^FAIL ') { throw ('Runtime checks incomplete: ' + $taskOutput) }
    $taskLog = ($taskReceipt | ConvertFrom-Json).result.consoleLogPath
    if (-not $taskLog -or -not (Test-Path -LiteralPath $taskLog) -or (Get-Item -LiteralPath $taskLog).Length -eq 0) { throw 'Full native console log missing.' }
    Copy-Item -LiteralPath $taskLog -Destination (Join-Path $taskOutput 'GameConsole.log')
    if (Select-String -LiteralPath $taskLog -Encoding utf8 -Pattern '^\d{2}:\d{2}:\d{2}\s+ERROR\b' -Quiet) { throw ('Native runtime errors: ' + $taskLog) }
    $taskResults | Select-Object -Last 5
    Write-Output $taskOutput
} finally {
    Pop-Location
    $taskRecoveryDeadline = [DateTime]::UtcNow.AddMinutes(8)
    do {
        $taskOtherGame = @(Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'" | Where-Object { $_.ExecutablePath -eq 'D:\RimWorld-Dev\RimWorld.exe' })
        $taskOtherLock = Test-Path -LiteralPath 'D:/RimWorld-Dev/SaveData/Config/RimDevTools.lock.json'
        $taskForeignLinks = @($taskSwitched | Where-Object {
            $taskItem = Get-Item -LiteralPath (Join-Path $taskGame ('Mods/' + $_))
            $taskItem.LinkType -ne 'Junction' -or $taskItem.Target -notin @($taskMirrors[$_], $taskOriginal[$_])
        })
        if ($taskOtherGame.Count -eq 0 -and -not $taskOtherLock -and $taskForeignLinks.Count -eq 0) { break }
        if ([DateTime]::UtcNow -ge $taskRecoveryDeadline) { throw ('Shared test environment remains occupied; recovery preserved: ' + $taskOutput) }
        Start-Sleep -Seconds 2
    } while ($true)
    foreach ($taskName in $taskSwitched) {
        $taskLink = Join-Path $taskGame ('Mods/' + $taskName)
        $taskItem = Get-Item -LiteralPath $taskLink
        if ($taskItem.LinkType -ne 'Junction' -or $taskItem.Target -notin @($taskMirrors[$taskName], $taskOriginal[$taskName])) { throw ('Junction identity changed: ' + $taskLink) }
        if ($taskItem.Target -eq $taskMirrors[$taskName]) {
            [IO.Directory]::Delete($taskLink)
            New-Item -ItemType Junction -Path $taskLink -Target $taskOriginal[$taskName] | Out-Null
        }
    }
    if (Test-Path -LiteralPath $taskFixtureLink) {
        $taskItem = Get-Item -LiteralPath $taskFixtureLink
        if ($taskItem.LinkType -ne 'Junction' -or $taskItem.Target -ne $taskFixture) { throw 'Fixture identity changed.' }
        [IO.Directory]::Delete($taskFixtureLink)
    }
    foreach ($taskFile in $taskBackups.Keys) {
        Copy-Item -LiteralPath $taskBackups[$taskFile].Path -Destination $taskFile -Force
        if ((Get-FileHash -LiteralPath $taskFile).Hash -ne $taskBackups[$taskFile].SHA256) { throw ('Configuration restoration failed: ' + $taskFile) }
    }
    if ($Profile -and -not $taskProfilerExisted -and (Test-Path -LiteralPath $taskProfilerPath)) {
        Copy-Item -LiteralPath $taskProfilerPath -Destination (Join-Path $taskOutput 'Profiler.generated.xml')
        Remove-Item -LiteralPath $taskProfilerPath
    }
    @{ JunctionsRestored = $true; ConfigRestored = $true } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOutput 'Cleanup.json') -Encoding utf8
}
