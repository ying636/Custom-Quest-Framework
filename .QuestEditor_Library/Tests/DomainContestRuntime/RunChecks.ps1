param([int]$TimeoutSeconds = 180, [switch]$LoadCheck, [switch]$BloodSea, [switch]$Fusion, [switch]$Layouts, [switch]$Startup, [switch]$SkipPreload, [switch]$Trapping, [switch]$UseDevMods, [switch]$DetailedStartup, [switch]$AssetCheck, [switch]$BattleRules, [switch]$Occupation, [switch]$ReviewFixes, [switch]$Entry, [switch]$CounterOpening, [switch]$Muted, [int]$ProcessorCount = 1, [string]$GameDirectory = 'D:\RimWorld-Dev', [string]$BuildOutputDirectory)

$ErrorActionPreference = 'Stop'
if ($Trapping) { $LoadCheck = $true }
if ($BattleRules -or $Occupation -or $ReviewFixes -or $Entry -or $CounterOpening) { $BloodSea = $true }
$gameDirectory = [IO.Path]::GetFullPath($GameDirectory)
$modsDirectory = [IO.Path]::GetFullPath((Join-Path $gameDirectory 'Mods'))
$testModDirectory = [IO.Path]::GetFullPath((Join-Path $modsDirectory 'CQF_ContestChecks'))
$runDirectory = Join-Path ([IO.Path]::GetTempPath()) ('CQF_ContestChecks_' + [guid]::NewGuid().ToString('N'))
$testProcess = $null
if (Test-Path -LiteralPath $testModDirectory) { throw "Test mod directory already exists: $testModDirectory" }
$runningGames = Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'"
if ($runningGames -and (!$BuildOutputDirectory -or $runningGames.ExecutablePath -contains (Join-Path $gameDirectory 'RimWorld.exe'))) {
    throw 'A game is already running. Parallel profiling requires a separate game directory and temporary build output.'
}
$harmonyAssembly = Join-Path $modsDirectory 'Workshop-2009463077\1.7\Assemblies\0Harmony.dll'
$buildOptions = @()
$checkAssembly = Join-Path $PSScriptRoot 'bin\Release\net10.0\CQF.DomainContestRuntimeChecks.dll'
if ($BuildOutputDirectory) {
    $temporaryOutput = [IO.Path]::GetFullPath($BuildOutputDirectory)
    $liveWorkspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
    if ($temporaryOutput.StartsWith($liveWorkspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Temporary profile build output must be outside the live workspace.'
    }
    $buildOptions += "-p:OutputPath=$temporaryOutput/"
    $buildOptions += '-p:AppendTargetFrameworkToOutputPath=false'
    $checkAssembly = Join-Path $temporaryOutput 'CQF.DomainContestRuntimeChecks.dll'
}
dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet -p:WarningLevel=0 "-p:RimWorldDir=$gameDirectory" "-p:HarmonyPath=$harmonyAssembly" @buildOptions
if ($LASTEXITCODE -ne 0) { throw 'Failed to build contest checks.' }
try {
    New-Item -ItemType Directory -Path (Join-Path $runDirectory 'Config') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $testModDirectory 'Domain') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $testModDirectory 'About') -Force | Out-Null
    '<ModMetaData><name>CQF.ContestChecks</name><author>HaiLuan</author><packageId>CQF.ContestChecks</packageId><supportedVersions><li>1.7</li></supportedVersions></ModMetaData>' |
        Set-Content -LiteralPath (Join-Path $testModDirectory 'About\About.xml') -Encoding UTF8
    '<loadFolders><v1.7><li>/</li><li RequiresCQFModule="Domain">Domain</li></v1.7></loadFolders>' |
        Set-Content -LiteralPath (Join-Path $testModDirectory 'LoadFolders.xml') -Encoding UTF8
    Copy-Item -LiteralPath $checkAssembly -Destination (Join-Path $testModDirectory 'Domain')
    if ($Layouts) {
        New-Item -ItemType Directory -Path (Join-Path $testModDirectory 'Domain\Defs') -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PlainDomainDefs.xml') -Destination (Join-Path $testModDirectory 'Domain\Defs')
    }
    $bloodSeaEntry = if ($BloodSea -or $Fusion) { '<li>hailuan.ritesofthesilenthouse</li>' } else { '' }
    $gameVersion = (Get-Content -LiteralPath (Join-Path $gameDirectory 'Version.txt') -Encoding UTF8 -TotalCount 1).Split(' ')[0]
    ('<ModsConfigData><version>' + $gameVersion + '</version><activeMods><li>brrainz.harmony</li><li>ludeon.rimworld</li><li>hailuan.customquestframework</li>' + $bloodSeaEntry + '<li>cqf.contestchecks</li></activeMods><knownExpansions /></ModsConfigData>') |
        Set-Content -LiteralPath (Join-Path $runDirectory 'Config\ModsConfig.xml') -Encoding UTF8
    if ($UseDevMods) {
        if (!$Startup) { throw 'Developer mod configuration is supported only for startup profiling.' }
        $devConfig = Join-Path $gameDirectory 'SaveData\Config'
        Copy-Item -Path (Join-Path $devConfig '*') -Destination (Join-Path $runDirectory 'Config') -Recurse -Force
        $profilePath = Join-Path $runDirectory 'Config\ModsConfig.xml'
        $profile = [xml](Get-Content -LiteralPath $profilePath -Encoding UTF8 -Raw)
        $testEntry = $profile.CreateElement('li')
        $testEntry.InnerText = 'cqf.contestchecks'
        [void]$profile.ModsConfigData.activeMods.AppendChild($testEntry)
        $profile.Save($profilePath)
    }
    if ($DetailedStartup) {
        if (!$Startup) { throw 'Detailed profiling requires startup checks.' }
        $prefsPath = Join-Path $runDirectory 'Config\Prefs.xml'
        $prefs = if (Test-Path -LiteralPath $prefsPath) { [xml](Get-Content -LiteralPath $prefsPath -Encoding UTF8 -Raw) } else { [xml]'<PrefsData />' }
        $verbose = $prefs.SelectSingleNode('/PrefsData/logVerbose')
        if (!$verbose) { $verbose=$prefs.CreateElement('logVerbose'); [void]$prefs.DocumentElement.AppendChild($verbose) }
        $verbose.InnerText='True'
        $prefs.Save($prefsPath)
    }
    Write-Output "Run directory: $runDirectory"
    $prefsPath = Join-Path $runDirectory 'Config\Prefs.xml'
    $prefs = if (Test-Path -LiteralPath $prefsPath) { [xml](Get-Content -LiteralPath $prefsPath -Encoding UTF8 -Raw) } else { [xml]'<PrefsData />' }
    $background = $prefs.SelectSingleNode('/PrefsData/runInBackground')
    if (!$background) { $background = $prefs.CreateElement('runInBackground'); [void]$prefs.DocumentElement.AppendChild($background) }
    $background.InnerText = 'True'
    if ($Muted) {
        foreach ($setting in @('volumeGame', 'volumeMusic', 'volumeAmbient')) {
            $node = $prefs.SelectSingleNode('/PrefsData/' + $setting)
            if (!$node) { $node = $prefs.CreateElement($setting); [void]$prefs.DocumentElement.AppendChild($node) }
            $node.InnerText = '0'
        }
    }
    $prefs.Save($prefsPath)
    $arguments = @('-batchmode', '-quicktest', '-cqfcontestchecks', "-savedatafolder=`"$runDirectory`"")
    if ($LoadCheck) { $arguments += '-cqfcontestloadchecks' }
    if ($BloodSea) { $arguments += '-cqfcontestbloodsea' }
    if ($Fusion) { $arguments += '-cqffusionchecks' }
    if ($Layouts) { $arguments += '-cqflayoutchecks' }
    if ($Startup) { $arguments += '-cqfstartupchecks' }
    if ($AssetCheck) {
        if (!$Startup -or !$UseDevMods) { throw 'Asset checks require startup checks with the developer mod configuration.' }
        $arguments += '-cqfassetchecks'
    }
    if ($SkipPreload) { $arguments += '-cqfskippreload' }
    if ($Trapping) { $arguments += '-cqftrappingchecks' }
    if ($BattleRules) { $arguments += '-cqfbattlerulechecks' }
    if ($Occupation) { $arguments += '-cqfoccupationchecks' }
    if ($ReviewFixes) { $arguments += '-cqfreviewfixchecks' }
    if ($Entry) { $arguments += '-cqfentrychecks' }
    if ($CounterOpening) { $arguments += '-cqfcounteropeningchecks' }
    $env:SteamAppId = '0'
    $env:SteamGameId = '0'
    $env:SteamOverlayGameId = '0'
    if ($ProcessorCount -lt 0) { throw 'ProcessorCount cannot be negative.' }
    if ($ProcessorCount -eq 0) { Remove-Item -LiteralPath 'Env:DOTNET_PROCESSOR_COUNT' -ErrorAction SilentlyContinue }
    else { $env:DOTNET_PROCESSOR_COUNT = $ProcessorCount.ToString() }
    $testProcess = Start-Process -FilePath (Join-Path $gameDirectory 'RimWorld.exe') -WorkingDirectory $gameDirectory -ArgumentList $arguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $runDirectory 'stdout.log') -RedirectStandardError (Join-Path $runDirectory 'stderr.log')
    if (!$testProcess.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $testProcess.Id -Force
        throw "Contest runtime checks timed out. Logs: $runDirectory"
    }
    $resultsPath = Join-Path $runDirectory 'DomainContestChecks.txt'
    if (Test-Path -LiteralPath $resultsPath) { Get-Content -LiteralPath $resultsPath -Encoding UTF8 }
    if ($testProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $resultsPath) -or
        !(Select-String -LiteralPath $resultsPath -Pattern '^COMPLETE$' -Quiet)) {
        throw "Contest runtime checks failed. Exit code: $($testProcess.ExitCode). Logs: $runDirectory"
    }
} finally {
    if ($testProcess -and !$testProcess.HasExited) { Stop-Process -Id $testProcess.Id -Force; $testProcess.WaitForExit() }
    if ($testProcess) { $testProcess.WaitForExit() }
    $resolvedTestDirectory = [IO.Path]::GetFullPath($testModDirectory)
    $testArchiveRoot = [IO.Path]::GetFullPath((Join-Path $gameDirectory 'SaveData\TestModArchives'))
    $testModArchive = [IO.Path]::GetFullPath((Join-Path $testArchiveRoot ([IO.Path]::GetFileName($runDirectory))))
    if ([IO.Path]::GetDirectoryName($resolvedTestDirectory) -ne $modsDirectory -or
        [IO.Path]::GetFileName($resolvedTestDirectory) -ne 'CQF_ContestChecks' -or
        [IO.Path]::GetDirectoryName($testModArchive) -ne $testArchiveRoot -or
        !$testArchiveRoot.StartsWith($gameDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected contest test archive paths.' }
    New-Item -ItemType Directory -Path $testArchiveRoot -Force | Out-Null
    if (Test-Path -LiteralPath $resolvedTestDirectory) { Move-Item -LiteralPath $resolvedTestDirectory -Destination $testModArchive }
}
