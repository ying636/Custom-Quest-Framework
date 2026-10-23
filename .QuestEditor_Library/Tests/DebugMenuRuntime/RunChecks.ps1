param(
    [Parameter(Mandatory = $true)][string]$CoreAssembly,
    [Parameter(Mandatory = $true)][string]$EditorAssembly,
    [string]$GameDirectory = 'D:/RimWorld-Dev',
    [string]$RunRoot = 'C:/Users/24904/AppData/Local/Temp/CQF_DebugSettingsFix_20261023/Runs',
    [string]$Dotnet = 'C:/Users/24904/AppData/Local/HaiLuan/dotnet/dotnet.exe',
    [ValidateSet('MainMenu', 'Playing')][string]$Mode = 'MainMenu',
    [int]$TimeoutSeconds = 240,
    [switch]$ExpectBroken,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$taskSetup = & (Join-Path $PSScriptRoot '../CinematicsRuntime/RunChecks.ps1') -CoreAssembly $CoreAssembly -EditorAssembly $EditorAssembly -GameDirectory $GameDirectory -Dotnet $Dotnet -RunRoot $RunRoot
$taskManifestLine = @($taskSetup | Where-Object { $_ -is [string] -and $_.StartsWith('{"RunDirectory":') })
if ($taskManifestLine.Count -ne 1) { throw 'The isolated debug menu setup did not return exactly one manifest.' }
$taskManifest = $taskManifestLine[0] | ConvertFrom-Json
'0' | Set-Content -LiteralPath (Join-Path $taskManifest.GameSnapshot 'steam_appid.txt') -Encoding ascii
$taskBuild = Join-Path $taskManifest.RunDirectory 'DebugMenuBuild'
& $Dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet "-p:CQFCoreAssembly=$CoreAssembly" "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$GameDirectory/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:OutputPath=$taskBuild/" "-p:BaseIntermediateOutputPath=$taskBuild/obj/" -p:AppendTargetFrameworkToOutputPath=false
if ($LASTEXITCODE -ne 0) { throw "Debug menu fixture compilation failed. Evidence: $($taskManifest.RunDirectory)" }
Copy-Item -LiteralPath (Join-Path $taskBuild 'CQF.CinematicsRuntimeChecks.dll') -Destination (Join-Path $taskManifest.GameSnapshot 'Mods/CQF_CinematicsChecks/Assemblies/CQF.CinematicsRuntimeChecks.dll') -Force
$taskManifest | Add-Member -NotePropertyName Mode -NotePropertyValue $Mode
$taskManifest | Add-Member -NotePropertyName ExpectBroken -NotePropertyValue ([bool]$ExpectBroken)
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
    $taskArguments = @('-NoWindow', '-batchmode', '-cqfdebugmenuchecks', "-savedatafolder=`"$($taskManifest.SaveData)`"")
    if ($Mode -eq 'Playing') { $taskArguments += '-quicktest' }
    if ($ExpectBroken) { $taskArguments += '-cqfexpectbroken' }
    $taskProcess = Start-Process -FilePath (Join-Path $taskManifest.GameSnapshot 'RimWorld.exe') -WorkingDirectory $taskManifest.GameSnapshot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskManifest.RunDirectory 'stdout.log') -RedirectStandardError (Join-Path $taskManifest.RunDirectory 'stderr.log')
    $taskWatch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $taskProcess.HasExited) {
        if ($taskWatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "Debug menu checks timed out. Evidence: $($taskManifest.RunDirectory)" }
        Start-Sleep -Seconds 2
        $taskProcess.Refresh()
    }
    $taskProcess.WaitForExit()
    $taskResults = Join-Path $taskManifest.SaveData 'DebugMenuChecks.txt'
    if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults -Encoding utf8 }
    if ($taskProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $taskResults) -or -not (Select-String -LiteralPath $taskResults -Pattern '^COMPLETE$' -Quiet)) { throw "Debug menu checks failed. Evidence: $($taskManifest.RunDirectory)" }
    if (Select-String -LiteralPath (Join-Path $taskManifest.RunDirectory 'stdout.log') -Encoding utf8 -Pattern '^\d{2}:\d{2}:\d{2}\s+ERROR\b' -Quiet) { throw "Debug menu checks logged runtime errors. Evidence: $($taskManifest.RunDirectory)" }
    if (Select-String -LiteralPath (Join-Path $taskManifest.RunDirectory 'stderr.log') -Encoding utf8 -Pattern '\bERROR\b|Exception' -Quiet) { throw "Debug menu checks logged native runtime errors. Evidence: $($taskManifest.RunDirectory)" }
} finally {
    if ($taskProcess -and -not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force; $taskProcess.WaitForExit() }
    $env:SteamAppId = $taskSteamApp
    $env:SteamGameId = $taskSteamGame
    $env:SteamOverlayGameId = $taskSteamOverlay
}
