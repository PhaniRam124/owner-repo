param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
Set-Location $repoRoot

& (Join-Path $PSScriptRoot 'verify-toolchain.ps1')

$solution = Join-Path $repoRoot 'CPRD.KnowledgeDesk.sln'
if (-not (Test-Path $solution)) {
    throw 'CPRD.KnowledgeDesk.sln does not exist.'
}

& dotnet restore $solution
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

& dotnet test $solution -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed.' }

& dotnet build $solution -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
