param(
    [Parameter(Mandatory = $true)][string]$CoreAssembly,
    [Parameter(Mandatory = $true)][string]$EditorAssembly,
    [string]$GameDirectory = 'D:/RimWorld-Dev',
    [string]$RunRoot = 'C:/Users/24904/AppData/Local/Temp/CQF_EntityBookRuntimeRuns',
    [string]$Dotnet = 'C:/Users/24904/AppData/Local/HaiLuan/dotnet/dotnet.exe',
    [string]$DevtoolsRuntimeDirectory = 'C:/Users/24904/AppData/Local/Temp/CW-RimDevTools-20261020/CleanLogSyncMod/1.7/Assemblies',
    [int]$TimeoutSeconds = 360,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
foreach ($taskRequired in @('LogSyncMod.dll', 'AutoTestRunner.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $DevtoolsRuntimeDirectory $taskRequired))) { throw "RimDevTools runtime is missing: $taskRequired" }
}
$taskSetup = & (Join-Path $PSScriptRoot '../CinematicsRuntime/RunChecks.ps1') -CoreAssembly $CoreAssembly -EditorAssembly $EditorAssembly -GameDirectory $GameDirectory -Dotnet $Dotnet -RunRoot $RunRoot
$taskManifestLine = @($taskSetup | Where-Object { $_ -is [string] -and $_.StartsWith('{"RunDirectory":') })
if ($taskManifestLine.Count -ne 1) { throw 'The isolated entity-book setup did not return exactly one manifest.' }
$taskManifest = $taskManifestLine[0] | ConvertFrom-Json
$taskBuild = Join-Path $taskManifest.RunDirectory 'EntityBookBuild'
& $Dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet "-p:CQFCoreAssembly=$CoreAssembly" "-p:CQFEditorAssembly=$EditorAssembly" "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$GameDirectory/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:OutputPath=$taskBuild/" "-p:BaseIntermediateOutputPath=$taskBuild/obj/" -p:AppendTargetFrameworkToOutputPath=false
if ($LASTEXITCODE -ne 0) { throw "Entity-book fixture compilation failed. Evidence: $($taskManifest.RunDirectory)" }
Copy-Item -LiteralPath (Join-Path $taskBuild 'CQF.CinematicsRuntimeChecks.dll') -Destination (Join-Path $taskManifest.GameSnapshot 'Mods/CQF_CinematicsChecks/Assemblies/CQF.CinematicsRuntimeChecks.dll') -Force
$taskDevtoolsMod = Join-Path $taskManifest.GameSnapshot 'Mods/CQF_EntityBookDevtools'
New-Item -ItemType Directory -Path (Join-Path $taskDevtoolsMod 'About'), (Join-Path $taskDevtoolsMod 'Assemblies') -Force | Out-Null
foreach ($taskRuntime in @('LogSyncMod.dll', 'AutoTestRunner.dll')) {
    Copy-Item -LiteralPath (Join-Path $DevtoolsRuntimeDirectory $taskRuntime) -Destination (Join-Path $taskDevtoolsMod 'Assemblies')
}
'<ModMetaData><name>RimDevTools isolated runtime</name><author>trigger</author><packageId>trigger.logsyncmod</packageId><supportedVersions><li>1.7</li></supportedVersions></ModMetaData>' |
    Set-Content -LiteralPath (Join-Path $taskDevtoolsMod 'About/About.xml') -Encoding utf8
$taskModsPath = Join-Path $taskManifest.SaveData 'Config/ModsConfig.xml'
[xml]$taskMods = Get-Content -LiteralPath $taskModsPath -Encoding utf8 -Raw
$taskMod = $taskMods.CreateElement('li')
$taskMod.InnerText = 'trigger.logsyncmod'
$taskMods.ModsConfigData.activeMods.AppendChild($taskMod) | Out-Null
$taskXmlSettings = [Xml.XmlWriterSettings]::new()
$taskXmlSettings.Encoding = [Text.UTF8Encoding]::new($false)
$taskXmlSettings.Indent = $true
$taskWriter = [Xml.XmlWriter]::Create($taskModsPath, $taskXmlSettings)
try { $taskMods.Save($taskWriter) } finally { $taskWriter.Dispose() }
$taskManifest | Add-Member -NotePropertyName DevtoolsRuntimeDirectory -NotePropertyValue $DevtoolsRuntimeDirectory
$taskManifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskManifest.RunDirectory 'RunManifest.json') -Encoding utf8
$taskManifest | ConvertTo-Json -Compress
if (-not $Launch) { return }
$taskProcess = $null
$taskSteamApp = $env:SteamAppId
$taskSteamGame = $env:SteamGameId
$taskSteamOverlay = $env:SteamOverlayGameId
try {
    $env:SteamAppId = '0'
    $env:SteamGameId = '0'
    $env:SteamOverlayGameId = '0'
    $taskArguments = @('-NoWindow', '-batchmode', '-quicktest', '-cqfentitybookchecks', "-savedatafolder=`"$($taskManifest.SaveData)`"")
    $taskProcess = Start-Process -FilePath (Join-Path $taskManifest.GameSnapshot 'RimWorld.exe') -WorkingDirectory $taskManifest.GameSnapshot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskManifest.RunDirectory 'stdout.log') -RedirectStandardError (Join-Path $taskManifest.RunDirectory 'stderr.log')
    $taskWatch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $taskProcess.HasExited) {
        if ($taskWatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "Entity-book checks timed out. Evidence: $($taskManifest.RunDirectory)" }
        Start-Sleep -Seconds 2
        $taskProcess.Refresh()
    }
    $taskProcess.WaitForExit()
    $taskResults = Join-Path $taskManifest.SaveData 'EntityBookChecks.txt'
    if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults -Encoding utf8 }
    if ($taskProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $taskResults) -or -not (Select-String -LiteralPath $taskResults -Pattern '^COMPLETE$' -Quiet)) { throw "Entity-book checks failed. Evidence: $($taskManifest.RunDirectory)" }
    $taskErrors = @(Select-String -LiteralPath (Join-Path $taskManifest.RunDirectory 'stdout.log') -Encoding utf8 -Pattern '^\d{2}:\d{2}:\d{2}\s+ERROR\b')
    if ($taskErrors.Count -ne 0) { throw "Unexpected entity-book runtime errors. Evidence: $($taskManifest.RunDirectory)" }
} finally {
    if ($taskProcess -and -not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force; $taskProcess.WaitForExit() }
    $env:SteamAppId = $taskSteamApp
    $env:SteamGameId = $taskSteamGame
    $env:SteamOverlayGameId = $taskSteamOverlay
}
