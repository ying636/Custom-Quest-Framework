param([int]$TimeoutSeconds = 420)

$ErrorActionPreference = 'Stop'
$workspaceDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$gameDirectory = [IO.Path]::GetFullPath((Join-Path $workspaceDirectory '..\..'))
$modsDirectory = [IO.Path]::GetFullPath((Join-Path $gameDirectory 'Mods'))
$testModDirectory = [IO.Path]::GetFullPath((Join-Path $modsDirectory 'CQF_HibernationChecks'))
$runDirectory = Join-Path ([IO.Path]::GetTempPath()) ('CQF_HibernationChecks_' + [guid]::NewGuid().ToString('N'))
$testProcess = $null

if (Test-Path -LiteralPath $testModDirectory) { throw "Test mod directory already exists: $testModDirectory" }
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'RimWorld is already running.' }
dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Failed to build map hibernation checks.' }

try {
    New-Item -ItemType Directory -Path (Join-Path $runDirectory 'Config') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $testModDirectory 'Assemblies') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Mod\About') -Destination $testModDirectory -Recurse
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Mod\Defs') -Destination $testModDirectory -Recurse
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\net10.0\CQF.HibernationChecks.dll') -Destination (Join-Path $testModDirectory 'Assemblies')
    @'
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData>
  <version>1.7.4976</version>
  <activeMods>
    <li>brrainz.harmony</li>
    <li>ludeon.rimworld</li>
    <li>hailuan.customquestframework</li>
    <li>cqf.hibernationchecks</li>
  </activeMods>
  <knownExpansions />
</ModsConfigData>
'@ | Set-Content -LiteralPath (Join-Path $runDirectory 'Config\ModsConfig.xml') -Encoding UTF8
    Write-Output "Run directory: $runDirectory"
    $arguments = @('-batchmode', '-quicktest', '-cqfhibernationchecks', "-savedatafolder=`"$runDirectory`"")
    $testProcess = Start-Process -FilePath (Join-Path $gameDirectory 'RimWorld.exe') -WorkingDirectory $gameDirectory -ArgumentList $arguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $runDirectory 'stdout.log') -RedirectStandardError (Join-Path $runDirectory 'stderr.log')
    if (!$testProcess.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $testProcess.Id -Force
        throw "Runtime checks timed out. Logs: $runDirectory"
    }
    $resultsPath = Join-Path $runDirectory 'HibernationChecks.txt'
    if (Test-Path -LiteralPath $resultsPath) { Get-Content -LiteralPath $resultsPath -Encoding UTF8 }
    if ($testProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $resultsPath) -or
        !(Select-String -LiteralPath $resultsPath -Pattern '^COMPLETE$' -Quiet)) {
        throw "Runtime checks failed. Exit code: $($testProcess.ExitCode). Logs: $runDirectory"
    }
    $runtimeLogPath = Join-Path $runDirectory 'stdout.log'
    if (Select-String -LiteralPath $runtimeLogPath -Encoding UTF8 -Pattern 'associated with invalid map index|Root level exception|CQF hibernation checks failed' -Quiet) {
        throw "Runtime log contains map or update errors. Logs: $runDirectory"
    }
} finally {
    if ($testProcess -and !$testProcess.HasExited) { Stop-Process -Id $testProcess.Id -Force }
    $resolvedTestDirectory = [IO.Path]::GetFullPath($testModDirectory)
    if ([IO.Path]::GetDirectoryName($resolvedTestDirectory) -ne $modsDirectory -or
        [IO.Path]::GetFileName($resolvedTestDirectory) -ne 'CQF_HibernationChecks') {
        throw "Unexpected test directory: $resolvedTestDirectory"
    }
    $testModArchive = [IO.Path]::GetFullPath((Join-Path $runDirectory 'TestMod'))
    if ([IO.Path]::GetDirectoryName($testModArchive) -ne [IO.Path]::GetFullPath($runDirectory)) {
        throw "Unexpected archive directory: $testModArchive"
    }
    if (Test-Path -LiteralPath $resolvedTestDirectory) { Move-Item -LiteralPath $resolvedTestDirectory -Destination $testModArchive }
}
