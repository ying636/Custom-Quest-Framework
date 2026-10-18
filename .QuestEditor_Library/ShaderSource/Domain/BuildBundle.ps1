param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath($ProjectPath)
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'ProjectPath must be an existing Unity project with the intended editor version.'
}
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'Packages/manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($module in @('com.unity.modules.assetbundle', 'com.unity.modules.imageconversion')) {
    if (-not $manifest.dependencies.$module) { throw "The Unity project must enable $module." }
}
$shaderFolder = Join-Path $projectRoot 'Assets/Shaders'
$editorFolder = Join-Path $projectRoot 'Assets/Editor'
New-Item -ItemType Directory -Path $shaderFolder, $editorFolder -Force | Out-Null
foreach ($name in @('CQFDomainOpening.shader', 'CQFDomainTransition.shader')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $shaderFolder $name)
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CQFDomainBundleBuilder.cs') -Destination (Join-Path $editorFolder 'CQFDomainBundleBuilder.cs')
$bundleRoot = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path $bundleRoot -Force | Out-Null
$verification = Join-Path $bundleRoot 'verified.txt'
if (Test-Path -LiteralPath $verification) { Remove-Item -LiteralPath $verification }
$previousOutput = $env:CQF_DOMAIN_BUNDLE_OUTPUT
try {
    $env:CQF_DOMAIN_BUNDLE_OUTPUT = $bundleRoot
    $buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-quit', '-projectPath', ('"' + $projectRoot + '"'),
        '-executeMethod', 'CQFDomainBundleBuilder.Build', '-logFile', ('"' + (Join-Path $bundleRoot 'build.log') + '"')) -WindowStyle Hidden -PassThru
    while (-not $buildProcess.WaitForExit(10000)) { Write-Output 'Unity is building and verifying the CQF domain shaders.' }
    if ($buildProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $verification)) {
        throw "Domain bundle validation failed. Read $(Join-Path $bundleRoot 'build.log')."
    }
    Write-Output "Verified bundle: $(Join-Path $bundleRoot 'cqf_domain_win')"
}
finally { $env:CQF_DOMAIN_BUNDLE_OUTPUT = $previousOutput }
