#!/usr/bin/env pwsh

# Script to build Azure Functions SQS Extensions

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [switch]$Package,
    
    [switch]$Clean,
    
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\build.ps1 [OPTIONS]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Configuration <Debug|Release>  Build configuration (default: Debug)"
    Write-Host "  -Package                        Create NuGet packages"
    Write-Host "  -Clean                          Clean before building"
    Write-Host "  -Help                           Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\build.ps1                           # Debug build, no packages"
    Write-Host "  .\build.ps1 -Configuration Release -Package  # Release build with packages"
    Write-Host "  .\build.ps1 -Clean -Configuration Release -Package  # Clean, then release build with packages"
    exit 0
}

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnetDir = Split-Path -Parent $scriptDir

# Ensure .NET is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET SDK not found. Run .\install-prereqs.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "=== Building Azure Functions SQS Extensions ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Create packages: $Package" -ForegroundColor Gray
Write-Host "Clean build: $Clean" -ForegroundColor Gray
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning..." -ForegroundColor Yellow
    dotnet clean "$dotnetDir\src\Azure.WebJobs.Extensions.SQS\Azure.WebJobs.Extensions.SQS.csproj" -c $Configuration
    dotnet clean "$dotnetDir\src\Azure.Functions.Worker.Extensions.SQS\Azure.Functions.Worker.Extensions.SQS.csproj" -c $Configuration
    Write-Host ""
}

# Build WebJobs Extension
Write-Host "Building Azure.WebJobs.Extensions.SQS..." -ForegroundColor Cyan
dotnet restore "$dotnetDir\src\Azure.WebJobs.Extensions.SQS\Azure.WebJobs.Extensions.SQS.csproj"
if ($Package) {
    dotnet build "$dotnetDir\src\Azure.WebJobs.Extensions.SQS\Azure.WebJobs.Extensions.SQS.csproj" `
        -c $Configuration `
        /p:GeneratePackageOnBuild=true
} else {
    dotnet build "$dotnetDir\src\Azure.WebJobs.Extensions.SQS\Azure.WebJobs.Extensions.SQS.csproj" `
        -c $Configuration `
        /p:GeneratePackageOnBuild=false
}
Write-Host ""

# Build Worker Extension
Write-Host "Building Azure.Functions.Worker.Extensions.SQS..." -ForegroundColor Cyan
dotnet restore "$dotnetDir\src\Azure.Functions.Worker.Extensions.SQS\Azure.Functions.Worker.Extensions.SQS.csproj"
if ($Package) {
    dotnet build "$dotnetDir\src\Azure.Functions.Worker.Extensions.SQS\Azure.Functions.Worker.Extensions.SQS.csproj" `
        -c $Configuration `
        /p:GeneratePackageOnBuild=true
} else {
    dotnet build "$dotnetDir\src\Azure.Functions.Worker.Extensions.SQS\Azure.Functions.Worker.Extensions.SQS.csproj" `
        -c $Configuration `
        /p:GeneratePackageOnBuild=false
}
Write-Host ""

# Summary
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host ""
if ($Package) {
    Write-Host "NuGet packages created:" -ForegroundColor Cyan
    Get-ChildItem -Path "$dotnetDir\src" -Filter "*.nupkg" -Recurse | Where-Object { $_.FullName -like "*\$Configuration\*" } | ForEach-Object {
        Write-Host "  $($_.FullName) ($([math]::Round($_.Length/1KB, 2)) KB)" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host "Build artifacts:" -ForegroundColor Cyan
Write-Host "  WebJobs Extension:   $dotnetDir\src\Azure.WebJobs.Extensions.SQS\bin\$Configuration\" -ForegroundColor Gray
Write-Host "  Worker Extension:    $dotnetDir\src\Azure.Functions.Worker.Extensions.SQS\bin\$Configuration\" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  Run tests: .\scripts\ci-test.ps1"
if (-not $Package) {
    Write-Host "  Create packages: .\build.ps1 -Configuration Release -Package"
}
