#!/usr/bin/env pwsh

# LocalStack Cleanup Script
# This script stops and removes LocalStack containers, networks, and volumes

$ErrorActionPreference = "Stop"

Write-Host "🧹 Cleaning up LocalStack resources..." -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$localstackDir = Split-Path -Parent $scriptDir

# Stop and remove containers
Write-Host "🛑 Stopping and removing LocalStack containers..." -ForegroundColor Yellow
docker-compose -f "$localstackDir/docker-compose.localstack.yml" down -v

# Remove LocalStack images (optional)
$response = Read-Host "Do you want to remove LocalStack images as well? (y/N)"
if ($response -match "^[Yy]$") {
    Write-Host "🗑️  Removing LocalStack images..." -ForegroundColor Yellow
    $images = docker images | Select-String "localstack" | ForEach-Object { ($_ -split '\s+')[2] }
    if ($images) {
        $images | ForEach-Object { 
            try {
                docker rmi -f $_
            } catch {
                Write-Host "   Could not remove image: $_" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "   No LocalStack images found" -ForegroundColor Gray
    }
}

# Clean up any dangling volumes
Write-Host "🧽 Cleaning up dangling volumes..." -ForegroundColor Yellow
docker volume prune -f | Out-Null

Write-Host ""
Write-Host "✅ LocalStack cleanup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To restart LocalStack, run:" -ForegroundColor Cyan
Write-Host "   .\localstack\setup-localstack.ps1"
