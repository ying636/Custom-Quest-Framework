param(
    [Parameter(Mandatory = $true)][string]$CoreAssembly,
    [Parameter(Mandatory = $true)][string]$EditorAssembly,
    [string]$GameDirectory = 'D:/RimWorld-Dev',
    [string]$RunRoot = 'C:/Users/24904/AppData/Local/Temp/CQF-RegionLayouts/Runtime',
    [string]$Dotnet = 'C:/Users/24904/AppData/Local/HaiLuan/dotnet/dotnet.exe',
    [ValidateSet('English', 'ChineseSimplified')][string]$Language = 'English',
    [int]$TimeoutSeconds = 360,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$taskSetup = & (Join-Path $PSScriptRoot '../CinematicsRuntime/RunChecks.ps1') -CoreAssembly $CoreAssembly -EditorAssembly $EditorAssembly -GameDirectory $GameDirectory -Dotnet $Dotnet -RunRoot $RunRoot
$taskManifestLine = @($taskSetup | Where-Object { $_ -is [string] -and $_.StartsWith('{"RunDirectory":') })
if ($taskManifestLine.Count -ne 1) { throw 'Map layout setup did not return one manifest.' }
$taskManifest = $taskManifestLine[0] | ConvertFrom-Json
$taskBuild = Join-Path $taskManifest.RunDirectory 'MapLayoutBuild'
& $Dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet "-p:CQFCoreAssembly=$CoreAssembly" "-p:CQFEditorAssembly=$EditorAssembly" "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$GameDirectory/Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll" "-p:OutputPath=$taskBuild/" -p:AppendTargetFrameworkToOutputPath=false
if ($LASTEXITCODE -ne 0) { throw "Map layout fixture compilation failed: $($taskManifest.RunDirectory)" }
Copy-Item -LiteralPath (Join-Path $taskBuild 'CQF.CinematicsRuntimeChecks.dll') -Destination (Join-Path $taskManifest.GameSnapshot 'Mods/CQF_CinematicsChecks/Assemblies/CQF.CinematicsRuntimeChecks.dll') -Force
[IO.File]::WriteAllText((Join-Path $taskManifest.GameSnapshot 'steam_appid.txt'), '0')
$taskPrefsPath = Join-Path $taskManifest.SaveData 'Config/Prefs.xml'
[xml]$taskPrefs = Get-Content -LiteralPath $taskPrefsPath -Raw -Encoding UTF8
$taskLanguageNode = $taskPrefs.CreateElement('langFolderName')
$taskLanguageNode.InnerText = $Language
[void]$taskPrefs.DocumentElement.AppendChild($taskLanguageNode)
$taskPrefs.Save($taskPrefsPath)
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
    $taskArguments = @('-NoWindow', '-batchmode', '-quicktest', '-cqfmaplayoutchecks', "-savedatafolder=`"$($taskManifest.SaveData)`"")
    $taskProcess = Start-Process -FilePath (Join-Path $taskManifest.GameSnapshot 'RimWorld.exe') -WorkingDirectory $taskManifest.GameSnapshot -ArgumentList $taskArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskManifest.RunDirectory 'stdout.log') -RedirectStandardError (Join-Path $taskManifest.RunDirectory 'stderr.log')
    $taskWatch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $taskProcess.HasExited) {
        if ($taskWatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "Map layout checks timed out: $($taskManifest.RunDirectory)" }
        Start-Sleep -Seconds 2
        $taskProcess.Refresh()
    }
    $taskProcess.WaitForExit()
    $taskResults = Join-Path $taskManifest.SaveData 'MapLayoutChecks.txt'
    if (Test-Path -LiteralPath $taskResults) { Get-Content -LiteralPath $taskResults -Encoding utf8 }
    if ($taskProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $taskResults) -or -not (Select-String -LiteralPath $taskResults -Pattern '^COMPLETE$' -Quiet)) { throw "Map layout checks failed: $($taskManifest.RunDirectory)" }
    $taskErrors = @(Select-String -LiteralPath (Join-Path $taskManifest.RunDirectory 'stdout.log') -Encoding utf8 -Pattern '^\d{2}:\d{2}:\d{2}\s+ERROR\b')
    if ($taskErrors.Count -ne 0) { throw "Unexpected runtime errors: $($taskManifest.RunDirectory)" }
} finally {
    if ($taskProcess -and -not $taskProcess.HasExited) { Stop-Process -Id $taskProcess.Id -Force; $taskProcess.WaitForExit() }
    $env:SteamAppId = $taskSteamApp
    $env:SteamGameId = $taskSteamGame
    $env:SteamOverlayGameId = $taskSteamOverlay
}
