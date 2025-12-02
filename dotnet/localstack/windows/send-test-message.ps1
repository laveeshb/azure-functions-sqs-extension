#!/usr/bin/env pwsh

# Send Test Message to LocalStack SQS
# This script sends a test message to the LocalStack SQS queue for testing Azure Functions triggers

$ErrorActionPreference = "Stop"

param(
    [string]$QueueName = "test-queue"
)

$ENDPOINT_URL = if ($env:AWS_ENDPOINT_URL) { $env:AWS_ENDPOINT_URL } else { "http://localhost:4566" }
$REGION = if ($env:AWS_REGION) { $env:AWS_REGION } else { "us-east-1" }

Write-Host "📤 Sending test message to queue: $QueueName" -ForegroundColor Cyan

# Get queue URL
try {
    $QUEUE_URL = aws --endpoint-url=$ENDPOINT_URL sqs get-queue-url `
        --queue-name $QueueName `
        --region $REGION `
        --output text `
        --no-cli-pager
} catch {
    Write-Host "❌ Error: Could not find queue '$QueueName'" -ForegroundColor Red
    Write-Host "   Make sure LocalStack is running: .\setup-localstack.ps1" -ForegroundColor Yellow
    exit 1
}

if (-not $QUEUE_URL) {
    Write-Host "❌ Error: Could not find queue '$QueueName'" -ForegroundColor Red
    Write-Host "   Make sure LocalStack is running: .\setup-localstack.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "   Queue URL: $QUEUE_URL" -ForegroundColor Gray

# Generate test message
$TIMESTAMP = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ" -AsUTC
$MESSAGE_ID = [guid]::NewGuid().ToString()

$MESSAGE_BODY = @"
{
  "id": "$MESSAGE_ID",
  "timestamp": "$TIMESTAMP",
  "type": "test",
  "message": "Hello from LocalStack SQS!",
  "data": {
    "source": "send-test-message.ps1",
    "environment": "local"
  }
}
"@

# Send message
$SENT_MESSAGE_ID = aws --endpoint-url=$ENDPOINT_URL sqs send-message `
    --queue-url $QUEUE_URL `
    --message-body $MESSAGE_BODY `
    --region $REGION `
    --output text `
    --query 'MessageId' `
    --no-cli-pager

Write-Host "✅ Message sent successfully!" -ForegroundColor Green
Write-Host "   Message ID: $SENT_MESSAGE_ID" -ForegroundColor Gray
Write-Host ""
Write-Host "📋 Message body:" -ForegroundColor Cyan
Write-Host $MESSAGE_BODY
Write-Host ""
Write-Host "🔍 Check your Azure Function logs to see the trigger response" -ForegroundColor Yellow
