#!/usr/bin/env pwsh

# Script to install prerequisites for Azure Functions SQS Extension development

$ErrorActionPreference = "Stop"

Write-Host "=== Installing Prerequisites ===" -ForegroundColor Cyan
Write-Host ""

# Function to check if command exists
function Test-CommandExists {
    param($Command)
    $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

# Install .NET SDK
if (Test-CommandExists dotnet) {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK already installed: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "Installing .NET SDK..." -ForegroundColor Yellow
    Write-Host "Please download and install .NET SDK from: https://dot.net/download" -ForegroundColor Cyan
    Write-Host "After installation, restart this script." -ForegroundColor Yellow
    Start-Process "https://dot.net/download"
    exit 0
}

# Install Azure Functions Core Tools
if (Test-CommandExists func) {
    $funcVersion = func --version
    Write-Host "✓ Azure Functions Core Tools already installed: $funcVersion" -ForegroundColor Green
} else {
    Write-Host "Installing Azure Functions Core Tools..." -ForegroundColor Yellow
    
    # Check if winget is available
    if (Test-CommandExists winget) {
        try {
            winget install Microsoft.Azure.FunctionsCoreTools
            Write-Host "✓ Azure Functions Core Tools installed" -ForegroundColor Green
        } catch {
            Write-Host "Failed to install via winget. Please install manually." -ForegroundColor Yellow
            Write-Host "Download from: https://github.com/Azure/azure-functions-core-tools" -ForegroundColor Cyan
        }
    } elseif (Test-CommandExists choco) {
        try {
            choco install azure-functions-core-tools -y
            Write-Host "✓ Azure Functions Core Tools installed" -ForegroundColor Green
        } catch {
            Write-Host "Failed to install via chocolatey. Please install manually." -ForegroundColor Yellow
            Write-Host "Download from: https://github.com/Azure/azure-functions-core-tools" -ForegroundColor Cyan
        }
    } else {
        Write-Host "Please install Azure Functions Core Tools manually:" -ForegroundColor Yellow
        Write-Host "  Download from: https://github.com/Azure/azure-functions-core-tools" -ForegroundColor Cyan
        Write-Host "  Or install winget/chocolatey and run this script again" -ForegroundColor Cyan
    }
}

# Install AWS CLI (optional but recommended)
if (Test-CommandExists aws) {
    $awsVersion = aws --version
    Write-Host "✓ AWS CLI already installed: $awsVersion" -ForegroundColor Green
} else {
    $installAws = Read-Host "AWS CLI not found. Install? (y/n)"
    if ($installAws -match "^[Yy]$") {
        Write-Host "Installing AWS CLI..." -ForegroundColor Yellow
        
        if (Test-CommandExists winget) {
            try {
                winget install Amazon.AWSCLI
                Write-Host "✓ AWS CLI installed" -ForegroundColor Green
            } catch {
                Write-Host "Failed to install via winget. Installing via MSI..." -ForegroundColor Yellow
                $msiUrl = "https://awscli.amazonaws.com/AWSCLIV2.msi"
                $msiPath = "$env:TEMP\AWSCLIV2.msi"
                Invoke-WebRequest -Uri $msiUrl -OutFile $msiPath
                Start-Process msiexec.exe -Wait -ArgumentList "/i $msiPath /quiet"
                Remove-Item $msiPath
                Write-Host "✓ AWS CLI installed" -ForegroundColor Green
            }
        } else {
            Write-Host "Downloading and installing AWS CLI..." -ForegroundColor Yellow
            $msiUrl = "https://awscli.amazonaws.com/AWSCLIV2.msi"
            $msiPath = "$env:TEMP\AWSCLIV2.msi"
            Invoke-WebRequest -Uri $msiUrl -OutFile $msiPath
            Start-Process msiexec.exe -Wait -ArgumentList "/i $msiPath /quiet"
            Remove-Item $msiPath
            Write-Host "✓ AWS CLI installed" -ForegroundColor Green
        }
    } else {
        Write-Host "⊘ Skipping AWS CLI installation" -ForegroundColor Gray
    }
}

# Install Docker (for LocalStack testing)
if (Test-CommandExists docker) {
    $dockerVersion = docker --version
    Write-Host "✓ Docker already installed: $dockerVersion" -ForegroundColor Green
} else {
    $installDocker = Read-Host "Docker not found. Install? (y/n)"
    if ($installDocker -match "^[Yy]$") {
        Write-Host "Please install Docker Desktop for Windows:" -ForegroundColor Yellow
        Write-Host "  Download from: https://www.docker.com/products/docker-desktop" -ForegroundColor Cyan
        Start-Process "https://www.docker.com/products/docker-desktop"
        Write-Host "⊘ Manual installation required for Docker Desktop" -ForegroundColor Yellow
    } else {
        Write-Host "⊘ Skipping Docker installation" -ForegroundColor Gray
    }
}

# Check Docker Compose (included with Docker Desktop on Windows)
if ((Test-CommandExists docker-compose) -or (docker compose version 2>$null)) {
    Write-Host "✓ Docker Compose already installed" -ForegroundColor Green
} else {
    Write-Host "ℹ Docker Compose is included with Docker Desktop for Windows" -ForegroundColor Gray
}

# Install Azurite (optional, for local Azure Storage emulation)
if (Test-CommandExists azurite) {
    Write-Host "✓ Azurite already installed" -ForegroundColor Green
} else {
    $installAzurite = Read-Host "Azurite not found. Install? (y/n)"
    if ($installAzurite -match "^[Yy]$") {
        if (Test-CommandExists npm) {
            Write-Host "Installing Azurite..." -ForegroundColor Yellow
            npm install -g azurite
            Write-Host "✓ Azurite installed" -ForegroundColor Green
        } else {
            Write-Host "⊘ npm not found. Install Node.js first to use Azurite." -ForegroundColor Yellow
            Write-Host "  Download from: https://nodejs.org/" -ForegroundColor Cyan
        }
    } else {
        Write-Host "⊘ Skipping Azurite installation" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== Prerequisites Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Installed components:" -ForegroundColor Cyan
if (Test-CommandExists dotnet) { 
    $dotnetVer = dotnet --version
    Write-Host "  ✓ .NET SDK: $dotnetVer" -ForegroundColor Green 
}
if (Test-CommandExists func) { 
    $funcVer = func --version
    Write-Host "  ✓ Azure Functions Core Tools: $funcVer" -ForegroundColor Green 
}
if (Test-CommandExists aws) { 
    $awsVer = aws --version
    Write-Host "  ✓ AWS CLI: $awsVer" -ForegroundColor Green 
}
if (Test-CommandExists docker) { 
    $dockerVer = docker --version
    Write-Host "  ✓ Docker: $dockerVer" -ForegroundColor Green 
}
if ((Test-CommandExists docker-compose) -or (docker compose version 2>$null)) { 
    Write-Host "  ✓ Docker Compose: installed" -ForegroundColor Green 
}
if (Test-CommandExists azurite) { 
    $azuriteVer = azurite --version
    Write-Host "  ✓ Azurite: $azuriteVer" -ForegroundColor Green 
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. For AWS: Configure credentials with 'aws configure'"
Write-Host "  2. For LocalStack: Run '.\localstack\setup-localstack.ps1' (see localstack\README.md)"
Write-Host "  3. Build extensions: .\scripts\build.ps1"
Write-Host "  4. Run tests: .\scripts\ci-test.ps1"
