$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK was not found. Install .NET 10 SDK before building CPRD Knowledge Desk.'
}

$version = (& dotnet --version).Trim()
Write-Host "dotnet SDK: $version"

$majorText = ($version -split '\.')[0]
$major = 0
if (-not [int]::TryParse($majorText, [ref]$major)) {
    throw "Unable to parse .NET SDK version: $version"
}

if ($major -lt 10) {
    throw "CPRD Knowledge Desk requires .NET SDK 10.x or newer. Found: $version"
}

Write-Host 'Windows build toolchain check passed.'
