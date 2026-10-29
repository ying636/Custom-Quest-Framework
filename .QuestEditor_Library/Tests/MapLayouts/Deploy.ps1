param(
    [string]$Workspace = (Join-Path $PSScriptRoot '../../..'),
    [string]$GameDirectory = 'D:/RimWorld-Dev',
    [string]$Dotnet = 'C:/Users/24904/AppData/Local/HaiLuan/dotnet/dotnet.exe'
)

$ErrorActionPreference = 'Stop'
$cqfRoot = (Resolve-Path -LiteralPath $Workspace).Path
if (Get-Process -Name RimWorld -ErrorAction SilentlyContinue) { throw 'CQF_Deployment_GameRunning' }
$cqfPackage = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) ('CQF/DeploymentPackages/RegionLayouts-' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
[void](New-Item -ItemType Directory -Path $cqfPackage)
$cqfBuild = Join-Path $cqfPackage 'Candidate'
$cqfSources = @(Get-ChildItem -LiteralPath (Join-Path $cqfRoot '.QuestEditor_Library') -Recurse -File |
    Where-Object { $_.Extension -in @('.cs', '.csproj', '.props') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    Sort-Object FullName | ForEach-Object { [pscustomobject]@{ Path = $_.FullName; Hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } })
$cqfHarmony = Join-Path $GameDirectory 'Mods/Workshop-2009463077/1.7/Assemblies/0Harmony.dll'
& $Dotnet build (Join-Path $cqfRoot '.QuestEditor_Library/CQF.Editor/CQF.Editor.csproj') -c Release "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$cqfHarmony" "-p:OutputPath=$cqfBuild/" -p:AppendTargetFrameworkToOutputPath=false --nologo -v:q *> (Join-Path $cqfPackage 'Build.log')
if ($LASTEXITCODE -ne 0) { throw "CQF_Deployment_BuildFailed: $cqfPackage" }
$cqfCore = Join-Path $cqfBuild 'QuestEditor_Library.dll'
$cqfEditor = Join-Path $cqfBuild 'CQF.Editor.dll'
& $Dotnet run --project (Join-Path $cqfRoot '.QuestEditor_Library/Tests/MapLayouts/Checks.csproj') -c Release "-p:RimWorldDir=$GameDirectory" "-p:HarmonyPath=$cqfHarmony" "-p:CQFCoreAssembly=$cqfCore" *> (Join-Path $cqfPackage 'Rules.log')
if ($LASTEXITCODE -ne 0) { throw "CQF_Deployment_RulesFailed: $cqfPackage" }
& (Join-Path $cqfRoot '.QuestEditor_Library/Tests/MapLayoutsRuntime/RunChecks.ps1') -CoreAssembly $cqfCore -EditorAssembly $cqfEditor -GameDirectory $GameDirectory -Dotnet $Dotnet -RunRoot (Join-Path $cqfPackage 'Runtime') -Launch *> (Join-Path $cqfPackage 'Runtime.log')
if (Get-Process -Name RimWorld -ErrorAction SilentlyContinue) { throw 'CQF_Deployment_GameRunning' }
 $cqfCurrentSources = @(Get-ChildItem -LiteralPath (Join-Path $cqfRoot '.QuestEditor_Library') -Recurse -File |
    Where-Object { $_.Extension -in @('.cs', '.csproj', '.props') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | Select-Object -ExpandProperty FullName)
if ($cqfCurrentSources.Count -ne $cqfSources.Count -or @(Compare-Object $cqfSources.Path $cqfCurrentSources).Count -gt 0) { throw 'CQF_Deployment_SourceChanged' }
foreach ($cqfSource in $cqfSources) {
    if (-not (Test-Path -LiteralPath $cqfSource.Path) -or (Get-FileHash -LiteralPath $cqfSource.Path -Algorithm SHA256).Hash -ne $cqfSource.Hash) {
        throw "CQF_Deployment_SourceChanged: $($cqfSource.Path)"
    }
}
$cqfEntries = @(
    @('QuestEditor_Library.dll', '1.7/Assemblies/net10.0/QuestEditor_Library.dll'),
    @('QuestEditor_Library.pdb', '1.7/Assemblies/net10.0/QuestEditor_Library.pdb'),
    @('CQF.Editor.dll', '1.7/Optional/Editor/CQF.Editor.dll'),
    @('CQF.Editor.pdb', '1.7/Optional/Editor/CQF.Editor.pdb')
)
$cqfRecords = @()
foreach ($cqfEntry in $cqfEntries) {
    $cqfTarget = [IO.Path]::GetFullPath((Join-Path $cqfRoot $cqfEntry[1]))
    if (-not $cqfTarget.StartsWith($cqfRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'CQF_Deployment_InvalidTarget' }
    $cqfBackup = Join-Path $cqfPackage ('Backup/' + $cqfEntry[1])
    $cqfExisted = Test-Path -LiteralPath $cqfTarget
    if ($cqfExisted) {
        [void](New-Item -ItemType Directory -Path (Split-Path -Parent $cqfBackup) -Force)
        Copy-Item -LiteralPath $cqfTarget -Destination $cqfBackup
    }
    $cqfRecords += [pscustomobject]@{ Target = $cqfTarget; Source = (Join-Path $cqfBuild $cqfEntry[0]); Backup = $cqfBackup; Existed = $cqfExisted }
}
$cqfRecords | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $cqfPackage 'Deployment.json') -Encoding UTF8
try {
    foreach ($cqfRecord in $cqfRecords) {
        [void](New-Item -ItemType Directory -Path (Split-Path -Parent $cqfRecord.Target) -Force)
        Copy-Item -LiteralPath $cqfRecord.Source -Destination $cqfRecord.Target -Force
        if ((Get-FileHash -LiteralPath $cqfRecord.Source).Hash -ne (Get-FileHash -LiteralPath $cqfRecord.Target).Hash) { throw "CQF_Deployment_HashMismatch: $($cqfRecord.Target)" }
    }
} catch {
    $cqfFailure = $_
    foreach ($cqfRecord in $cqfRecords) {
        if ($cqfRecord.Existed) { Copy-Item -LiteralPath $cqfRecord.Backup -Destination $cqfRecord.Target -Force }
        elseif (Test-Path -LiteralPath $cqfRecord.Target) { Remove-Item -LiteralPath $cqfRecord.Target }
    }
    throw $cqfFailure
}
Write-Output "CQF_Deployment_Complete: $cqfPackage"
