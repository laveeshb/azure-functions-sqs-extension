# Azure Functions SQS Extension - Test Application

This is a modern Azure Functions v4 isolated worker application demonstrating the SQS extension capabilities.

## Features Demonstrated

### Trigger Functions
- **ProcessSqsMessage**: Basic SQS trigger using AWS credential chain
- **ProcessSqsMessageAsync**: Async SQS message processing

### Output Functions
- **SendSimpleMessage**: Send a simple text message to SQS
- **SendDelayedMessage**: Send messages with delay and custom attributes
- **SendBatchMessages**: Send multiple messages using IAsyncCollector
- **SendWithExplicitCredentials**: Example using explicit AWS credentials

## Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools v4
- AWS Account with SQS queue
- Azurite (for local Azure Storage emulation)

## Setup

### 1. Configure AWS Credentials

The extension supports AWS credential chain. Choose one method:

**Option A: Environment Variables (Recommended for development)**
```bash
export AWS_ACCESS_KEY_ID="your-access-key"
export AWS_SECRET_ACCESS_KEY="your-secret-key"
export AWS_DEFAULT_REGION="us-east-1"
```

**Option B: AWS CLI Configuration**
```bash
aws configure
```

**Option C: IAM Roles** (when running on AWS infrastructure)
- No configuration needed, uses instance profile or ECS task role

### 2. Update local.settings.json

```json
{
  "Values": {
    "SQS_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/YOUR-ACCOUNT-ID/your-queue",
    "SQS_OUTPUT_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/YOUR-ACCOUNT-ID/your-output-queue"
  }
}
```

### 3. Start Azurite

```bash
# Using npm
azurite-blob --silent --location ./azurite --debug ./azurite/debug.log

# Or using Docker
docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
```

## Running Locally

```bash
# Navigate to the test directory
cd dotnet/test/Extensions.SQS.Sample

# Run the function app
func start
```

## Testing the Functions

### Test SQS Trigger
Send a message to your SQS queue using AWS CLI:
```bash
aws sqs send-message \
  --queue-url https://sqs.us-east-1.amazonaws.com/YOUR-ACCOUNT-ID/your-queue \
  --message-body "Hello from AWS CLI"
```

The trigger function will automatically process it.

### Test SQS Output

**Send a simple message:**
```bash
curl "http://localhost:7071/api/send-simple?message=HelloWorld"
```

**Send a delayed message:**
```bash
curl "http://localhost:7071/api/send-delayed?message=DelayedMessage&delay=10"
```

**Send batch messages:**
```bash
curl "http://localhost:7071/api/send-batch?count=5&prefix=TestBatch"
```

## Configuration Options

Configure in `host.json`:

```json
{
  "extensions": {
    "sqsQueue": {
      "maxNumberOfMessages": 10,      // Max messages per poll (1-10)
      "pollingInterval": "00:00:15",  // Poll every 15 seconds
      "visibilityTimeout": "00:00:30" // Message visibility timeout
    }
  }
}
```

## Project Structure

```
Extensions.SQS.Sample/
├── Functions/
│   ├── SqsTriggerFunction.cs   # Trigger examples
│   └── SqsOutputFunction.cs    # Output binding examples
├── Program.cs                   # Application startup
├── host.json                    # Function app configuration
├── local.settings.json          # Local development settings
└── Extensions.SQS.Sample.csproj # Project file
```

## Key Features

- ✅ **No Hardcoded Credentials**: Uses AWS credential chain
- ✅ **Long Polling**: Reduces API calls by up to 90%
- ✅ **Structured Logging**: Modern ILogger patterns
- ✅ **Async/Await**: Proper async patterns throughout
- ✅ **Nullable Safety**: Nullable reference types enabled
- ✅ **Modern C#**: Latest C# features and patterns

## Troubleshooting

**AWS Credentials Not Found:**
- Verify AWS credentials are configured (`aws sts get-caller-identity`)
- Check environment variables are set

**Azurite Connection Failed:**
- Ensure Azurite is running on port 10000
- Check `AzureWebJobsStorage` in local.settings.json

**Messages Not Processing:**
- Verify SQS queue URL is correct
- Check AWS permissions for sqs:ReceiveMessage, sqs:DeleteMessage
- Review function logs for errors

## Learn More

- [Amazon SQS Documentation](https://docs.aws.amazon.com/sqs/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Extension Source Code](../../src/Extensions.SQS/)
