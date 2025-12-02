# LocalStack Testing Guide

This directory contains tools for testing the Azure Functions SQS Extension with LocalStack, allowing you to test locally without connecting to AWS.

## Prerequisites

- Docker and Docker Compose installed
- AWS CLI installed (`aws --version`)
- Bash shell (Linux/macOS/WSL on Windows)

## Quick Start

### 1. Start LocalStack

```bash
./setup-localstack.sh
```

This script will:
- Start LocalStack in a Docker container
- Create test SQS queues (`test-queue`, `test-queue.fifo`, `test-dlq`)
- Display configuration for your `local.settings.json`

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
    "AWS_ENDPOINT_URL": "http://localhost:4566"
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
cd samples/Extensions.SQS.Sample.v3  # or v2 for in-process
func start
```

### 5. Send Test Messages

```bash
# Send to default queue (test-queue)
./send-test-message.sh

# Send to specific queue
./send-test-message.sh test-queue.fifo
```

## Available Scripts

### setup-localstack.sh
Sets up LocalStack with SQS queues for testing.

**Usage:**
```bash
./setup-localstack.sh
```

### send-test-message.sh
Sends a test message to a LocalStack SQS queue.

**Usage:**
```bash
# Send to default queue
./send-test-message.sh

# Send to specific queue
./send-test-message.sh my-queue-name
```

## Managing LocalStack

### View Logs
```bash
docker-compose -f docker-compose.localstack.yml logs -f
```

### Stop LocalStack
```bash
docker-compose -f docker-compose.localstack.yml down
```

### Restart LocalStack
```bash
docker-compose -f docker-compose.localstack.yml restart
```

### Remove All Data
```bash
docker-compose -f docker-compose.localstack.yml down -v
```

## Manual Queue Operations

### List Queues
```bash
aws --endpoint-url=http://localhost:4566 sqs list-queues --region us-east-1
```

### Create Queue
```bash
aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name my-test-queue \
    --region us-east-1
```

### Send Message
```bash
aws --endpoint-url=http://localhost:4566 sqs send-message \
    --queue-url http://localhost:4566/000000000000/test-queue \
    --message-body "Test message" \
    --region us-east-1
```

### Receive Messages
```bash
aws --endpoint-url=http://localhost:4566 sqs receive-message \
    --queue-url http://localhost:4566/000000000000/test-queue \
    --region us-east-1
```

### Purge Queue
```bash
aws --endpoint-url=http://localhost:4566 sqs purge-queue \
    --queue-url http://localhost:4566/000000000000/test-queue \
    --region us-east-1
```

## Troubleshooting

### LocalStack Not Starting
- Check if Docker is running: `docker info`
- Check if port 4566 is available: `lsof -i :4566`
- View LocalStack logs: `docker-compose -f docker-compose.localstack.yml logs`

### Function Not Receiving Messages
1. Verify LocalStack is running: `curl http://localhost:4566/_localstack/health`
2. Check queue exists: `aws --endpoint-url=http://localhost:4566 sqs list-queues --region us-east-1`
3. Verify AWS credentials in `local.settings.json` (use `test`/`test`)
4. Ensure `AWS_ENDPOINT_URL` is set to `http://localhost:4566`
5. Check function app logs for connection errors

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
- **Standard queues**: Regular SQS queues
- **FIFO queues**: First-In-First-Out queues with exactly-once processing
- **Dead letter queues**: For handling failed messages
- **Message attributes**: Full support for custom attributes
- **Batch operations**: Send/receive/delete multiple messages

## Differences from AWS

While LocalStack provides excellent AWS emulation, some differences exist:
- No charges or quotas
- Simplified IAM (accepts any credentials)
- Faster message delivery (no actual network latency)
- Some advanced AWS features may not be implemented

## Additional Resources

- [LocalStack Documentation](https://docs.localstack.cloud/)
- [LocalStack SQS Documentation](https://docs.localstack.cloud/user-guide/aws/sqs/)
- [AWS CLI SQS Documentation](https://docs.aws.amazon.com/cli/latest/reference/sqs/)
- [Azure Functions Local Development](https://learn.microsoft.com/azure/azure-functions/functions-develop-local)
