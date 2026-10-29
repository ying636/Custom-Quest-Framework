param(
    [Parameter(Mandatory = $true)][string]$CoreAssembly,
    [Parameter(Mandatory = $true)][string]$EditorAssembly,
    [string]$GameDirectory = 'D:/RimWorld-Dev',
    [string]$RunRoot = '',
    [string]$Dotnet = 'C:/Users/24904/AppData/Local/HaiLuan/dotnet/dotnet.exe',
    [int]$TimeoutSeconds = 300,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$taskSetup = & (Join-Path $PSScriptRoot '../CinematicsRuntime/RunChecks.ps1') -CoreAssembly $CoreAssembly -EditorAssembly $EditorAssembly -GameDirectory $GameDirectory -Dotnet $Dotnet -RunRoot $RunRoot
$taskManifestLine = @($taskSetup | Where-Object { $_ -is [string] -and $_.StartsWith('{"RunDirectory":') })
if ($taskManifestLine.Count -ne 1) { throw 'The isolated dialogpresentation setup did not return exactly one manifest.' }
$taskManifest = $taskManifestLine[0] | ConvertFrom-Json
$taskPrefsPath = Join-Path $taskManifest.SaveData 'Config/Prefs.xml'
$taskPrefsText = Get-Content -LiteralPath $taskPrefsPath -Encoding utf8 -Raw
if (-not $taskPrefsText.Contains('<pauseOnLoad>False</pauseOnLoad>')) { throw 'The isolated pause-on-load setting was not found.' }
$taskPrefsText.Replace('<pauseOnLoad>False</pauseOnLoad>', '<pauseOnLoad>True</pauseOnLoad>') | Set-Content -LiteralPath $taskPrefsPath -Encoding utf8
$taskBuild = Join-Path $taskManifest.RunDirectory 'DialogPresentationBuild'
& $Dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet "-p:CQFCoreAssembly=$CoreAssembly" "-p:CQFEditorAssembly=$EditorAssembly" "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$GameDirectory/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:OutputPath=$taskBuild/" -p:AppendTargetFrameworkToOutputPath=false
if ($LASTEXITCODE -ne 0) { throw "DialogPresentation fixture compilation failed. Evidence: $($taskManifest.RunDirectory)" }
Copy-Item -LiteralPath (Join-Path $taskBuild 'CQF.CinematicsRuntimeChecks.dll') -Destination (Join-Path $taskManifest.GameSnapshot 'Mods/CQF_CinematicsChecks/Assemblies/CQF.CinematicsRuntimeChecks.dll') -Force
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
    $taskArguments = @('-NoWindow', '-batchmode', '-quicktest', '-cqfdialogpresentationchecks', "-savedatafolder=`"$($taskManifest.SaveData)`"")
    $taskProcess = Start-Process -FilePath (Join-Path $taskManifest.GameSnapshot 'RimWorld.exe') -WorkingDirectory $taskManifest.GameSnapshot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskManifest.RunDirectory 'stdout.log') -RedirectStandardError (Join-Path $taskManifest.RunDirectory 'stderr.log')
    $taskWatch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $taskProcess.HasExited) {
        if ($taskWatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "DialogPresentation checks timed out. Evidence: $($taskManifest.RunDirectory)" }
        Start-Sleep -Seconds 2
        $taskProcess.Refresh()
    }
    $taskProcess.WaitForExit()
    $taskResults = Join-Path $taskManifest.SaveData 'DialogPresentationChecks.txt'
    if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults -Encoding utf8 }
    if ($taskProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $taskResults) -or -not (Select-String -LiteralPath $taskResults -Pattern '^COMPLETE$' -Quiet)) { throw "DialogPresentation checks failed. Evidence: $($taskManifest.RunDirectory)" }
    $taskErrors = @(Select-String -LiteralPath (Join-Path $taskManifest.RunDirectory 'stdout.log') -Encoding utf8 -Pattern '^\d{2}:\d{2}:\d{2}\s+ERROR\b')
    if ($taskErrors.Count -ne 0) { throw "Unexpected dialogpresentation runtime errors. Evidence: $($taskManifest.RunDirectory)" }
} finally {
    if ($taskProcess -and -not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force; $taskProcess.WaitForExit() }
    $env:SteamAppId = $taskSteamApp
    $env:SteamGameId = $taskSteamGame
    $env:SteamOverlayGameId = $taskSteamOverlay
}
