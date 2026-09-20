param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Version = '1.0.0-beta2'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
Set-Location $repoRoot

& (Join-Path $PSScriptRoot 'verify-toolchain.ps1')

$publishDir = Join-Path $repoRoot 'artifacts/publish'
$releaseDir = Join-Path $repoRoot 'artifacts/release'

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

$project = Join-Path $repoRoot 'src/CPRD.KnowledgeDesk.App/CPRD.KnowledgeDesk.App.csproj'

& dotnet publish $project     -c $Configuration     -r $Runtime     --self-contained true     -p:PublishSingleFile=true     -p:IncludeNativeLibrariesForSelfExtract=true     -p:PublishReadyToRun=false     -p:Version=$Version     -p:AssemblyVersion=1.0.0.0     -p:FileVersion=1.0.0.0     -o $publishDir

if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$portableZip = Join-Path $releaseDir "CPRD-Knowledge-Desk-$Version-win-x64-portable.zip"
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $portableZip -CompressionLevel Optimal

Write-Host "Publish output: $publishDir"
Write-Host "Portable package: $portableZip"
