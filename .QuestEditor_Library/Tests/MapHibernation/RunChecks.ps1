param([int]$TimeoutSeconds = 420, [string]$FrameworkBuildRoot)

$ErrorActionPreference = 'Stop'
$workspaceDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$gameDirectory = [IO.Path]::GetFullPath((Join-Path $workspaceDirectory '..\..'))
$modsDirectory = [IO.Path]::GetFullPath((Join-Path $gameDirectory 'Mods'))
$testModDirectory = [IO.Path]::GetFullPath((Join-Path $modsDirectory 'CQF_HibernationChecks'))
$runDirectory = Join-Path ([IO.Path]::GetTempPath()) ('CQF_HibernationChecks_' + [guid]::NewGuid().ToString('N'))
$testProcess = $null
$frameworkModDirectory = $null
$frameworkPackageId = 'hailuan.customquestframework'

if (Test-Path -LiteralPath $testModDirectory) { throw "Test mod directory already exists: $testModDirectory" }
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'RimWorld is already running.' }
if ($FrameworkBuildRoot) {
    $FrameworkBuildRoot = (Resolve-Path -LiteralPath $FrameworkBuildRoot).Path
    $frameworkModDirectory = [IO.Path]::GetFullPath((Join-Path $modsDirectory 'CQF_HibernationFrameworkChecks'))
    if (Test-Path -LiteralPath $frameworkModDirectory) { throw "Test framework directory already exists: $frameworkModDirectory" }
    if (!(Test-Path -LiteralPath (Join-Path $FrameworkBuildRoot '1.7/Assemblies/net10.0/QuestEditor_Library.dll'))) {
        throw "Missing staged framework build: $FrameworkBuildRoot"
    }
    $frameworkPackageId = 'cqf.hibernationframeworkchecks'
    dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -p:BuildProjectReferences=false -v quiet -clp:ErrorsOnly
} else {
    dotnet build (Join-Path $PSScriptRoot 'Checks.csproj') -c Release -v quiet -clp:ErrorsOnly
}
if ($LASTEXITCODE -ne 0) { throw 'Failed to build map hibernation checks.' }

try {
    New-Item -ItemType Directory -Path (Join-Path $runDirectory 'Config') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $testModDirectory 'Assemblies') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Mod\About') -Destination $testModDirectory -Recurse
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Mod\Defs') -Destination $testModDirectory -Recurse
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\net10.0\CQF.HibernationChecks.dll') -Destination (Join-Path $testModDirectory 'Assemblies')
    if ($frameworkModDirectory) {
        New-Item -ItemType Directory -Path $frameworkModDirectory | Out-Null
        foreach ($name in @('About','Languages','Patches','Quests','Textures','1.7','LoadFolders.xml')) {
            Copy-Item -LiteralPath (Join-Path $workspaceDirectory $name) -Destination $frameworkModDirectory -Recurse
        }
        Copy-Item -LiteralPath (Join-Path $FrameworkBuildRoot '1.7') -Destination $frameworkModDirectory -Recurse -Force
        $aboutPath = Join-Path $frameworkModDirectory 'About/About.xml'
        [xml]$about = [IO.File]::ReadAllText($aboutPath, [Text.Encoding]::UTF8)
        $about.ModMetaData.packageId = $frameworkPackageId
        $about.Save($aboutPath)
        $testAboutPath = Join-Path $testModDirectory 'About/About.xml'
        [xml]$testAbout = [IO.File]::ReadAllText($testAboutPath, [Text.Encoding]::UTF8)
        $testAbout.ModMetaData.modDependencies.li.packageId = $frameworkPackageId
        $testAbout.Save($testAboutPath)
    }
    $configuration = @'
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
'@
    $configuration.Replace('hailuan.customquestframework', $frameworkPackageId) |
        Set-Content -LiteralPath (Join-Path $runDirectory 'Config\ModsConfig.xml') -Encoding UTF8
    Write-Output "Run directory: $runDirectory"
    $arguments = @('-batchmode', '-quicktest', '-cqfhibernationchecks', "-savedatafolder=`"$runDirectory`"")
    $testProcess = Start-Process -FilePath (Join-Path $gameDirectory 'RimWorld.exe') -WorkingDirectory $gameDirectory -ArgumentList $arguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $runDirectory 'stdout.log') -RedirectStandardError (Join-Path $runDirectory 'stderr.log')
    if (!$testProcess.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $testProcess.Id -Force
        if (!$testProcess.WaitForExit(10000)) { throw 'Timed-out test process did not exit.' }
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
    if ($testProcess -and !$testProcess.HasExited) {
        Stop-Process -Id $testProcess.Id -Force
        if (!$testProcess.WaitForExit(10000)) { throw 'Test process did not exit before archiving.' }
    }
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
    if ($frameworkModDirectory) {
        $resolvedFrameworkDirectory = [IO.Path]::GetFullPath($frameworkModDirectory)
        $frameworkArchive = [IO.Path]::GetFullPath((Join-Path $runDirectory 'FrameworkMod'))
        if ([IO.Path]::GetDirectoryName($resolvedFrameworkDirectory) -ne $modsDirectory -or
            [IO.Path]::GetFileName($resolvedFrameworkDirectory) -ne 'CQF_HibernationFrameworkChecks' -or
            [IO.Path]::GetDirectoryName($frameworkArchive) -ne [IO.Path]::GetFullPath($runDirectory)) {
            throw 'Unexpected framework test archive directory.'
        }
        if (Test-Path -LiteralPath $resolvedFrameworkDirectory) { Move-Item -LiteralPath $resolvedFrameworkDirectory -Destination $frameworkArchive }
    }
}
