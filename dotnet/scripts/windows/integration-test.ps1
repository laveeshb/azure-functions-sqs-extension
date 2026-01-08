# Integration test script for AWS extensions
# Runs tests against LocalStack using Testcontainers (Docker required)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Join-Path $scriptDir "../.."

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "Azure Functions AWS Extensions - Integration Tests" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# Check for Docker
try {
    $dockerVersion = docker --version
    Write-Host "Docker found: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker is required for integration tests" -ForegroundColor Red
    Write-Host "Please install Docker: https://docs.docker.com/get-docker/" -ForegroundColor Yellow
    exit 1
}

# Check Docker is running
try {
    docker info | Out-Null
    Write-Host "Docker daemon is running" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker daemon is not running" -ForegroundColor Red
    Write-Host "Please start Docker and try again" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Navigate to root
Push-Location $rootDir

try {
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build --configuration Release
    
    Write-Host ""
    Write-Host "Running integration tests..." -ForegroundColor Yellow
    Write-Host "Note: This will automatically start LocalStack container via Testcontainers" -ForegroundColor Gray
    Write-Host ""
    
    # Run integration tests with detailed output
    dotnet test test/Extensions.AWS.IntegrationTests/Extensions.AWS.IntegrationTests.csproj `
        --configuration Release `
        --logger "console;verbosity=detailed" `
        --no-build
    
    Write-Host ""
    Write-Host "===================================================" -ForegroundColor Green
    Write-Host "Integration tests completed successfully!" -ForegroundColor Green
    Write-Host "===================================================" -ForegroundColor Green
} finally {
    Pop-Location
}
