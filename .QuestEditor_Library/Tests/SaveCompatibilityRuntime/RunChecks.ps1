param(
    [Parameter(Mandatory = $true)][string]$CoreAssembly,
    [Parameter(Mandatory = $true)][string]$EditorAssembly,
    [Parameter(Mandatory = $true)][string]$LegacyCoreAssembly,
    [Parameter(Mandatory = $true)][string]$LegacyEditorAssembly,
    [string]$GameDirectory = 'D:/RimWorld-Dev',
    [string]$RunRoot = 'C:/Users/24904/AppData/Local/Temp/CQF_SaveCompatibilityRuntimeRuns',
    [string]$Dotnet = 'C:/Users/24904/AppData/Local/HaiLuan/dotnet/dotnet.exe',
    [int]$TimeoutSeconds = 360,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$taskNewCore = (Resolve-Path -LiteralPath $CoreAssembly).Path
$taskNewEditor = (Resolve-Path -LiteralPath $EditorAssembly).Path
$taskOldCore = (Resolve-Path -LiteralPath $LegacyCoreAssembly).Path
$taskOldEditor = (Resolve-Path -LiteralPath $LegacyEditorAssembly).Path
$taskSetup = & (Join-Path $PSScriptRoot '../CinematicsRuntime/RunChecks.ps1') -CoreAssembly $taskOldCore -EditorAssembly $taskOldEditor -GameDirectory $GameDirectory -Dotnet $Dotnet -RunRoot $RunRoot
$taskManifestLine = @($taskSetup | Where-Object { $_ -is [string] -and $_.StartsWith('{"RunDirectory":') })
if ($taskManifestLine.Count -ne 1) { throw 'The isolated save compatibility setup did not return exactly one manifest.' }
$taskManifest = $taskManifestLine[0] | ConvertFrom-Json
$taskPatchPath = Join-Path $taskManifest.GameSnapshot 'Mods/Custom-Quest-Framework/1.7/Optional/Editor/Patches/CQF_EditorDesignators.xml'
[xml]$taskPatch = Get-Content -LiteralPath $taskPatchPath -Encoding utf8 -Raw
foreach ($taskEntry in @($taskPatch.SelectNodes('//li[text()="QuestEditor_Library.Designator_CQFEntityBook"]'))) { $taskEntry.ParentNode.RemoveChild($taskEntry) | Out-Null }
$taskXmlSettings = [Xml.XmlWriterSettings]::new()
$taskXmlSettings.Encoding = [Text.UTF8Encoding]::new($false)
$taskXmlSettings.Indent = $true
$taskWriter = [Xml.XmlWriter]::Create($taskPatchPath, $taskXmlSettings)
try { $taskPatch.Save($taskWriter) } finally { $taskWriter.Dispose() }
foreach ($taskPhase in @('LegacyProducer', 'CurrentConsumer')) {
    $taskBuild = Join-Path $taskManifest.RunDirectory $taskPhase
    $taskCore = if ($taskPhase -eq 'LegacyProducer') { $taskOldCore } else { $taskNewCore }
    $taskEditor = if ($taskPhase -eq 'LegacyProducer') { $taskOldEditor } else { $taskNewEditor }
    $taskLegacy = if ($taskPhase -eq 'LegacyProducer') { 'true' } else { 'false' }
    & $Dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet "-p:CQFCoreAssembly=$taskCore" "-p:CQFEditorAssembly=$taskEditor" "-p:LegacyProducer=$taskLegacy" "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$GameDirectory/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:OutputPath=$taskBuild/" "-p:BaseIntermediateOutputPath=$taskBuild/obj/" -p:AppendTargetFrameworkToOutputPath=false
    if ($LASTEXITCODE -ne 0) { throw "$taskPhase fixture compilation failed. Evidence: $($taskManifest.RunDirectory)" }
}
$taskManifest | Add-Member -NotePropertyName LegacyCoreHash -NotePropertyValue (Get-FileHash -LiteralPath $taskOldCore -Algorithm SHA256).Hash
$taskManifest | Add-Member -NotePropertyName LegacyEditorHash -NotePropertyValue (Get-FileHash -LiteralPath $taskOldEditor -Algorithm SHA256).Hash
$taskManifest | Add-Member -NotePropertyName CandidateCoreHash -NotePropertyValue (Get-FileHash -LiteralPath $taskNewCore -Algorithm SHA256).Hash
$taskManifest | Add-Member -NotePropertyName CandidateEditorHash -NotePropertyValue (Get-FileHash -LiteralPath $taskNewEditor -Algorithm SHA256).Hash
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
    foreach ($taskPhase in @('LegacyProducer', 'CurrentConsumer')) {
        $taskCore = if ($taskPhase -eq 'LegacyProducer') { $taskOldCore } else { $taskNewCore }
        $taskEditor = if ($taskPhase -eq 'LegacyProducer') { $taskOldEditor } else { $taskNewEditor }
        Copy-Item -LiteralPath $taskCore -Destination (Join-Path $taskManifest.GameSnapshot 'Mods/Custom-Quest-Framework/1.7/Assemblies/net10.0/QuestEditor_Library.dll') -Force
        Copy-Item -LiteralPath $taskEditor -Destination (Join-Path $taskManifest.GameSnapshot 'Mods/Custom-Quest-Framework/1.7/Optional/Editor/CQF.Editor.dll') -Force
        Copy-Item -LiteralPath (Join-Path $taskManifest.RunDirectory "$taskPhase/CQF.CinematicsRuntimeChecks.dll") -Destination (Join-Path $taskManifest.GameSnapshot 'Mods/CQF_CinematicsChecks/Assemblies/CQF.CinematicsRuntimeChecks.dll') -Force
        $taskOutput = Join-Path $taskManifest.RunDirectory "$taskPhase.stdout.log"
        $taskErrors = Join-Path $taskManifest.RunDirectory "$taskPhase.stderr.log"
        $taskArguments = @('-NoWindow', '-batchmode', '-quicktest', '-cqfsavecompatibility', "-savedatafolder=`"$($taskManifest.SaveData)`"")
        $taskProcess = Start-Process -FilePath (Join-Path $taskManifest.GameSnapshot 'RimWorld.exe') -WorkingDirectory $taskManifest.GameSnapshot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $taskOutput -RedirectStandardError $taskErrors
        $taskWatch = [Diagnostics.Stopwatch]::StartNew()
        while (-not $taskProcess.HasExited) {
            if ($taskWatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "$taskPhase checks timed out. Evidence: $($taskManifest.RunDirectory)" }
            Start-Sleep -Seconds 2
            $taskProcess.Refresh()
        }
        $taskProcess.WaitForExit()
        $taskResults = Join-Path $taskManifest.SaveData $(if ($taskPhase -eq 'LegacyProducer') { 'LegacyProducerChecks.txt' } else { 'SaveCompatibilityChecks.txt' })
        if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults -Encoding utf8 }
        if ($taskProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $taskResults) -or -not (Select-String -LiteralPath $taskResults -Pattern '^COMPLETE$' -Quiet)) { throw "$taskPhase failed. Evidence: $($taskManifest.RunDirectory)" }
        $taskRuntimeErrors = @(Select-String -LiteralPath $taskOutput -Encoding utf8 -Pattern '^\d{2}:\d{2}:\d{2}\s+ERROR\b')
        if ($taskRuntimeErrors.Count -ne 0) { throw "$taskPhase logged unexpected runtime errors. Evidence: $($taskManifest.RunDirectory)" }
    }
} finally {
    if ($taskProcess -and -not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force; $taskProcess.WaitForExit() }
    $env:SteamAppId = $taskSteamApp
    $env:SteamGameId = $taskSteamGame
    $env:SteamOverlayGameId = $taskSteamOverlay
}
