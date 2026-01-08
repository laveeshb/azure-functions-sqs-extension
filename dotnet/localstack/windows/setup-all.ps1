#Requires -Version 5.1
$ErrorActionPreference = "Stop"

Write-Host "Setting up AWS services in LocalStack..." -ForegroundColor Cyan

$Endpoint = if ($env:LOCALSTACK_ENDPOINT) { $env:LOCALSTACK_ENDPOINT } else { "http://localhost:4566" }

$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
$env:AWS_DEFAULT_REGION = "us-east-1"

function Invoke-Aws {
    param([string[]]$Args)
    & aws --endpoint-url $Endpoint @Args
}

# SQS
Write-Host "`n[SQS] Creating queues..." -ForegroundColor Yellow
Invoke-Aws sqs, create-queue, --queue-name, "test-queue" | Out-Null
Invoke-Aws sqs, create-queue, --queue-name, "s3-events-queue" | Out-Null
Invoke-Aws sqs, create-queue, --queue-name, "eventbridge-target-queue" | Out-Null

# SNS
Write-Host "[SNS] Creating topic..." -ForegroundColor Yellow
$topicArn = (Invoke-Aws sns, create-topic, --name, "test-topic" --output, json | ConvertFrom-Json).TopicArn

# Subscribe SQS to SNS for testing
Write-Host "[SNS] Subscribing SQS queue to topic..." -ForegroundColor Yellow
Invoke-Aws sns, subscribe, `
    --topic-arn, $topicArn, `
    --protocol, sqs, `
    --notification-endpoint, "arn:aws:sqs:us-east-1:000000000000:test-queue"

# EventBridge
Write-Host "[EventBridge] Creating event bus..." -ForegroundColor Yellow
try { Invoke-Aws events, create-event-bus, --name, "test-bus" }
catch { Write-Host "  Event bus may already exist" -ForegroundColor Gray }

Invoke-Aws events, put-rule, `
    --name, "catch-all", `
    --event-bus-name, "test-bus", `
    --event-pattern, '{"source":[{"prefix":""}]}'

Invoke-Aws events, put-targets, `
    --rule, "catch-all", `
    --event-bus-name, "test-bus", `
    --targets, "Id=sqs-target,Arn=arn:aws:sqs:us-east-1:000000000000:eventbridge-target-queue"

# S3
Write-Host "[S3] Creating bucket..." -ForegroundColor Yellow
try { Invoke-Aws s3, mb, "s3://test-bucket" }
catch { Write-Host "  Bucket may already exist" -ForegroundColor Gray }

# Configure S3 to send events to SQS
$s3NotificationConfig = @{
    QueueConfigurations = @(
        @{
            QueueArn = "arn:aws:sqs:us-east-1:000000000000:s3-events-queue"
            Events = @("s3:ObjectCreated:*", "s3:ObjectRemoved:*")
        }
    )
} | ConvertTo-Json -Depth 5

$s3NotificationConfig | Out-File -FilePath "s3-notification.json" -Encoding utf8
Invoke-Aws s3api, put-bucket-notification-configuration, `
    --bucket, "test-bucket", `
    --notification-configuration, "file://s3-notification.json"
Remove-Item "s3-notification.json"

# Kinesis
Write-Host "[Kinesis] Creating stream..." -ForegroundColor Yellow
try {
    Invoke-Aws kinesis, create-stream, --stream-name, "test-stream", --shard-count, 1
    Write-Host "  Waiting for stream to become active..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
}
catch { Write-Host "  Stream may already exist" -ForegroundColor Gray }

Write-Host "`n✅ Setup complete!" -ForegroundColor Green
Write-Host @"

Resources created:
  SQS Queues:
    - http://localhost:4566/000000000000/test-queue
    - http://localhost:4566/000000000000/s3-events-queue
    - http://localhost:4566/000000000000/eventbridge-target-queue

  SNS Topic:
    - arn:aws:sns:us-east-1:000000000000:test-topic

  EventBridge:
    - Event Bus: test-bus (with catch-all rule -> SQS)

  S3:
    - Bucket: test-bucket (with notification -> SQS)

  Kinesis:
    - Stream: test-stream (1 shard)

Environment variables for local.settings.json:
  AWS_ACCESS_KEY_ID=test
  AWS_SECRET_ACCESS_KEY=test
  AWS_REGION=us-east-1
  SQS_QUEUE_URL=http://localhost:4566/000000000000/test-queue
  SNS_TOPIC_ARN=arn:aws:sns:us-east-1:000000000000:test-topic
  EVENT_BUS_NAME=test-bus
  S3_BUCKET_NAME=test-bucket
  KINESIS_STREAM_NAME=test-stream
  *_SERVICE_URL=http://localhost:4566
"@
