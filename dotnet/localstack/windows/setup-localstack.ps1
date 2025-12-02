#!/usr/bin/env pwsh

# LocalStack Setup Script for Azure Functions SQS Extension Testing
# This script sets up LocalStack with SQS queues for local testing

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting LocalStack for SQS testing..." -ForegroundColor Cyan

# Check if Docker is running
try {
    docker info | Out-Null
} catch {
    Write-Host "❌ Error: Docker is not running. Please start Docker first." -ForegroundColor Red
    exit 1
}

# Start LocalStack
Write-Host "📦 Starting LocalStack container..." -ForegroundColor Yellow
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$localstackDir = Split-Path -Parent $scriptDir
docker-compose -f "$localstackDir/docker-compose.localstack.yml" up -d

# Wait for LocalStack to be ready
Write-Host "⏳ Waiting for LocalStack to be ready..." -ForegroundColor Yellow
$timeout = 30
$counter = 0
$ready = $false

while ($counter -lt $timeout) {
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:4566/_localstack/health" -ErrorAction SilentlyContinue
        if ($health.services.sqs -eq "available") {
            $ready = $true
            break
        }
    } catch {
        # Ignore errors and keep trying
    }
    Write-Host "   Waiting... ($counter/$timeout seconds)"
    Start-Sleep -Seconds 1
    $counter++
}

if (-not $ready) {
    Write-Host "❌ Error: LocalStack failed to start within $timeout seconds" -ForegroundColor Red
    exit 1
}

Write-Host "✅ LocalStack is ready!" -ForegroundColor Green

# Create test queues
Write-Host "📝 Creating SQS test queues..." -ForegroundColor Yellow

# Input queue
try {
    aws --endpoint-url=http://localhost:4566 sqs create-queue `
        --queue-name test-queue `
        --region us-east-1 `
        --no-cli-pager 2>&1 | Out-Null
    Write-Host "   ✓ Created 'test-queue'" -ForegroundColor Green
} catch {
    Write-Host "   ℹ Queue 'test-queue' already exists" -ForegroundColor Gray
}

# Output queue
try {
    aws --endpoint-url=http://localhost:4566 sqs create-queue `
        --queue-name test-output-queue `
        --region us-east-1 `
        --no-cli-pager 2>&1 | Out-Null
    Write-Host "   ✓ Created 'test-output-queue'" -ForegroundColor Green
} catch {
    Write-Host "   ℹ Queue 'test-output-queue' already exists" -ForegroundColor Gray
}

# FIFO queue
try {
    aws --endpoint-url=http://localhost:4566 sqs create-queue `
        --queue-name test-queue.fifo `
        --attributes FifoQueue=true `
        --region us-east-1 `
        --no-cli-pager 2>&1 | Out-Null
    Write-Host "   ✓ Created 'test-queue.fifo'" -ForegroundColor Green
} catch {
    Write-Host "   ℹ Queue 'test-queue.fifo' already exists" -ForegroundColor Gray
}

# Dead letter queue
try {
    aws --endpoint-url=http://localhost:4566 sqs create-queue `
        --queue-name test-dlq `
        --region us-east-1 `
        --no-cli-pager 2>&1 | Out-Null
    Write-Host "   ✓ Created 'test-dlq'" -ForegroundColor Green
} catch {
    Write-Host "   ℹ Queue 'test-dlq' already exists" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ LocalStack SQS setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Available queues:" -ForegroundColor Cyan
aws --endpoint-url=http://localhost:4566 sqs list-queues --region us-east-1 --no-cli-pager
Write-Host ""
Write-Host "🔗 LocalStack endpoint: http://localhost:4566" -ForegroundColor Cyan
Write-Host "🌍 Region: us-east-1" -ForegroundColor Cyan
Write-Host ""
Write-Host "⚙️  Configure your local.settings.json with:" -ForegroundColor Yellow
Write-Host @'
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AWS_REGION": "us-east-1",
    "AWS_ACCESS_KEY_ID": "test",
    "AWS_SECRET_ACCESS_KEY": "test",
    "AWS_ENDPOINT_URL": "http://localhost:4566"
  }
}
'@
Write-Host ""
Write-Host "🧪 To send a test message:" -ForegroundColor Cyan
Write-Host "   .\localstack\send-test-message.ps1"
Write-Host ""
Write-Host "🛑 To stop LocalStack:" -ForegroundColor Cyan
Write-Host "   docker-compose -f localstack/docker-compose.localstack.yml down"
