# LocalStack Testing Guide

This directory contains tools for testing the Azure Functions AWS Extensions with LocalStack, allowing you to test locally without connecting to AWS.

## Supported AWS Services

LocalStack provides local emulation for all AWS services supported by this extension:

| Service | Status | LocalStack Support |
|---------|--------|-------------------|
| SQS | ✅ Full | Queues, FIFO, DLQ, attributes |
| EventBridge | ✅ Full | Event buses, rules, targets |
| SNS | ✅ Full | Topics, subscriptions, filtering |
| S3 | ✅ Full | Buckets, objects, events |
| Kinesis | ✅ Full | Streams, shards, records |

## Prerequisites

- Docker and Docker Compose installed
- AWS CLI installed (`aws --version`)
- **Linux/macOS:** Bash shell
- **Windows:** PowerShell 5.1+ or PowerShell Core

## Quick Start

### Option 1: Setup All Services

**Linux/macOS:**
```bash
./localstack/unix/setup-all.sh
```

**Windows:**
```powershell
.\localstack\windows\setup-all.ps1
```

This creates test resources for ALL AWS services:
- SQS queues (`test-queue`, `test-queue.fifo`, `test-dlq`)
- EventBridge event bus (`test-event-bus`)
- SNS topic (`test-topic`) with SQS subscription
- S3 bucket (`test-bucket`) with event notifications
- Kinesis stream (`test-stream`)

### Option 2: Setup SQS Only

**Linux/macOS:**
```bash
./localstack/unix/setup-localstack.sh
```

**Windows:**
```powershell
.\localstack\windows\setup-localstack.ps1
```

### 2. Configure Your Function App

Update your function app's `local.settings.json` with LocalStack settings:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AWS_REGION": "us-east-1",
    "AWS_ACCESS_KEY_ID": "test",
    "AWS_SECRET_ACCESS_KEY": "test",
    
    "SQS_QUEUE_URL": "http://localhost:4566/000000000000/test-queue",
    "SQS_SERVICE_URL": "http://localhost:4566",
    
    "EVENTBRIDGE_SERVICE_URL": "http://localhost:4566",
    "EVENT_BUS_NAME": "test-event-bus",
    
    "SNS_SERVICE_URL": "http://localhost:4566",
    "SNS_TOPIC_ARN": "arn:aws:sns:us-east-1:000000000000:test-topic",
    
    "S3_SERVICE_URL": "http://localhost:4566",
    "S3_BUCKET_NAME": "test-bucket",
    
    "KINESIS_SERVICE_URL": "http://localhost:4566",
    "KINESIS_STREAM_NAME": "test-stream"
  }
}
```

### 3. Update Extension Configuration

For the SQS extension to connect to LocalStack, you need to configure it to use the custom endpoint:

**In-Process Model:**
```csharp
[FunctionName("ProcessSQSMessage")]
public static void Run(
    [SqsQueueTrigger("test-queue", Connection = "AWS")] string message,
    ILogger log)
{
    log.LogInformation($"Received message: {message}");
}
```

In `local.settings.json`, add:
```json
"AWS:ServiceURL": "http://localhost:4566"
```

**Isolated Worker Model:**
```csharp
[Function("ProcessSQSMessage")]
public void Run([SqsQueueTrigger("test-queue")] string message)
{
    _logger.LogInformation($"Received message: {message}");
}
```

### 4. Start Your Function App

```bash
cd test/Extensions.SQS.Test.Isolated  # or Extensions.SQS.Test.InProcess
func start
```

### 5. Send Test Messages

**Linux/macOS:**
```bash
# Send to default queue (test-queue)
./localstack/unix/send-test-message.sh

# Send to specific queue
./localstack/unix/send-test-message.sh test-queue.fifo
```

**Windows:**
```powershell
# Send to default queue (test-queue)
.\localstack\windows\send-test-message.ps1

# Send to specific queue
.\localstack\windows\send-test-message.ps1 -QueueName test-queue.fifo
```

## Available Scripts

### Setup LocalStack
Sets up LocalStack with SQS queues for testing.

**Linux/macOS:**
```bash
./localstack/unix/setup-localstack.sh
```

**Windows:**
```powershell
.\localstack\windows\setup-localstack.ps1
```

### Send Test Message
Sends a test message to a LocalStack SQS queue.

**Linux/macOS:**
```bash
# Send to default queue
./localstack/unix/send-test-message.sh

# Send to specific queue
./localstack/unix/send-test-message.sh my-queue-name
```

**Windows:**
```powershell
# Send to default queue
.\localstack\windows\send-test-message.ps1

# Send to specific queue
.\localstack\windows\send-test-message.ps1 -QueueName my-queue-name
```

## Managing LocalStack

### View Logs
```bash
docker-compose -f localstack/docker-compose.localstack.yml logs -f
```

### Stop LocalStack
```bash
docker-compose -f localstack/docker-compose.localstack.yml down
```

### Restart LocalStack
```bash
docker-compose -f localstack/docker-compose.localstack.yml restart
```

### Remove All Data
```bash
docker-compose -f localstack/docker-compose.localstack.yml down -v
```

## Manual Queue Operations

### SQS Operations

#### List Queues
```bash
aws --endpoint-url=http://localhost:4566 sqs list-queues --region us-east-1
```

#### Create Queue
```bash
aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name my-test-queue \
    --region us-east-1
```

#### Send Message
```bash
aws --endpoint-url=http://localhost:4566 sqs send-message \
    --queue-url http://localhost:4566/000000000000/test-queue \
    --message-body "Test message" \
    --region us-east-1
```

#### Receive Messages
```bash
aws --endpoint-url=http://localhost:4566 sqs receive-message \
    --queue-url http://localhost:4566/000000000000/test-queue \
    --region us-east-1
```

#### Purge Queue
```bash
aws --endpoint-url=http://localhost:4566 sqs purge-queue \
    --queue-url http://localhost:4566/000000000000/test-queue \
    --region us-east-1
```

### EventBridge Operations

#### List Event Buses
```bash
aws --endpoint-url=http://localhost:4566 events list-event-buses --region us-east-1
```

#### Put Events
```bash
aws --endpoint-url=http://localhost:4566 events put-events \
    --entries '[{"EventBusName":"test-event-bus","Source":"test","DetailType":"TestEvent","Detail":"{\"key\":\"value\"}"}]' \
    --region us-east-1
```

### SNS Operations

#### List Topics
```bash
aws --endpoint-url=http://localhost:4566 sns list-topics --region us-east-1
```

#### Publish Message
```bash
aws --endpoint-url=http://localhost:4566 sns publish \
    --topic-arn arn:aws:sns:us-east-1:000000000000:test-topic \
    --message "Test message" \
    --region us-east-1
```

### S3 Operations

#### List Buckets
```bash
aws --endpoint-url=http://localhost:4566 s3 ls
```

#### Upload File
```bash
aws --endpoint-url=http://localhost:4566 s3 cp myfile.txt s3://test-bucket/
```

#### List Objects
```bash
aws --endpoint-url=http://localhost:4566 s3 ls s3://test-bucket/
```

### Kinesis Operations

#### List Streams
```bash
aws --endpoint-url=http://localhost:4566 kinesis list-streams --region us-east-1
```

#### Put Record
```bash
aws --endpoint-url=http://localhost:4566 kinesis put-record \
    --stream-name test-stream \
    --partition-key "key1" \
    --data "dGVzdCBkYXRh" \
    --region us-east-1
```

#### Describe Stream
```bash
aws --endpoint-url=http://localhost:4566 kinesis describe-stream \
    --stream-name test-stream \
    --region us-east-1
```

## Troubleshooting

### LocalStack Not Starting
- Check if Docker is running: `docker info`
- Check if port 4566 is available: `lsof -i :4566`
- View LocalStack logs: `docker-compose -f localstack/docker-compose.localstack.yml logs`

### Function Not Receiving Messages (SQS)
1. Verify LocalStack is running: `curl http://localhost:4566/_localstack/health`
2. Check queue exists: `aws --endpoint-url=http://localhost:4566 sqs list-queues --region us-east-1`
3. Verify AWS credentials in `local.settings.json` (use `test`/`test`)
4. Ensure `AWS_ENDPOINT_URL` is set to `http://localhost:4566`
5. Check function app logs for connection errors

### EventBridge Events Not Triggering
1. Verify event bus exists: `aws --endpoint-url=http://localhost:4566 events list-event-buses --region us-east-1`
2. Check rules on the event bus: `aws --endpoint-url=http://localhost:4566 events list-rules --event-bus-name test-event-bus --region us-east-1`
3. Ensure `AWS_EVENTBRIDGE_URL` is configured correctly

### SNS Messages Not Being Delivered
1. Check topic exists: `aws --endpoint-url=http://localhost:4566 sns list-topics --region us-east-1`
2. Verify subscriptions: `aws --endpoint-url=http://localhost:4566 sns list-subscriptions-by-topic --topic-arn arn:aws:sns:us-east-1:000000000000:test-topic --region us-east-1`
3. Ensure `AWS_SNS_URL` is configured correctly

### S3 Events Not Triggering
1. Check bucket exists: `aws --endpoint-url=http://localhost:4566 s3 ls`
2. Verify bucket notification configuration: `aws --endpoint-url=http://localhost:4566 s3api get-bucket-notification-configuration --bucket test-bucket`
3. Ensure `AWS_S3_URL` is configured correctly

### Kinesis Stream Issues
1. Check stream exists: `aws --endpoint-url=http://localhost:4566 kinesis list-streams --region us-east-1`
2. Describe stream: `aws --endpoint-url=http://localhost:4566 kinesis describe-stream --stream-name test-stream --region us-east-1`
3. Ensure `AWS_KINESIS_URL` is configured correctly

### AWS CLI Not Found
```bash
# macOS
brew install awscli

# Ubuntu/Debian
sudo apt-get install awscli

# Windows
# Download from https://aws.amazon.com/cli/
```

## LocalStack Features

LocalStack provides a fully functional local AWS cloud stack, including:

### SQS Features
- **Standard queues**: Regular SQS queues
- **FIFO queues**: First-In-First-Out queues with exactly-once processing
- **Dead letter queues**: For handling failed messages
- **Message attributes**: Full support for custom attributes
- **Batch operations**: Send/receive/delete multiple messages

### EventBridge Features
- **Event buses**: Custom and default event buses
- **Rules and targets**: Route events to targets
- **Event patterns**: Filter events based on patterns

### SNS Features
- **Topics**: Standard and FIFO topics
- **Subscriptions**: HTTP, SQS, Lambda subscriptions
- **Message filtering**: Filter policies for subscriptions
- **Message attributes**: Custom message metadata

### S3 Features
- **Buckets and objects**: Full CRUD operations
- **Event notifications**: Trigger on object operations
- **Versioning**: Object version management
- **Lifecycle policies**: Automated object management

### Kinesis Features
- **Data streams**: Real-time data streaming
- **Shards**: Parallel processing units
- **Records**: Put and get records
- **Enhanced fan-out**: Consumer applications

## Differences from AWS

While LocalStack provides excellent AWS emulation, some differences exist:
- No charges or quotas
- Simplified IAM (accepts any credentials)
- Faster message delivery (no actual network latency)
- Some advanced AWS features may not be implemented

## Additional Resources

- [LocalStack Documentation](https://docs.localstack.cloud/)
- [LocalStack SQS Documentation](https://docs.localstack.cloud/user-guide/aws/sqs/)
- [LocalStack EventBridge Documentation](https://docs.localstack.cloud/user-guide/aws/events/)
- [LocalStack SNS Documentation](https://docs.localstack.cloud/user-guide/aws/sns/)
- [LocalStack S3 Documentation](https://docs.localstack.cloud/user-guide/aws/s3/)
- [LocalStack Kinesis Documentation](https://docs.localstack.cloud/user-guide/aws/kinesis/)
- [AWS CLI Reference](https://docs.aws.amazon.com/cli/latest/reference/)
- [Azure Functions Local Development](https://learn.microsoft.com/azure/azure-functions/functions-develop-local)
