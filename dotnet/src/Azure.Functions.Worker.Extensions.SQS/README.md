# Azure Functions Worker SQS Extension

Amazon SQS extension for Azure Functions using the **Isolated Worker (Out-of-Process) hosting model**.

> ⭐ **Recommended for new projects!** The isolated worker model is the recommended approach for new Azure Functions.

## Installation

```bash
dotnet add package Azure.Functions.Worker.Extensions.SQS
```

## Features

- ✅ Trigger functions from Amazon SQS queues
- ✅ AWS credential chain support (no hardcoded credentials needed)
- ✅ Automatic message deletion on successful processing
- ✅ Runs in separate process for better isolation
- ✅ Support for any .NET version (.NET 6, .NET 8, etc.)
- ✅ Async/await support throughout
- ✅ Strong typing with Message objects or strings

## Quick Start

### Prerequisites

- Azure Functions Runtime v4
- .NET 6 or .NET 8
- AWS Account with SQS queue
- AWS credentials configured (environment variables, IAM role, or AWS CLI)

### Setup Program.cs

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
```

### Trigger Example

```csharp
using Amazon.SQS.Model;
using Azure.Functions.Worker.Extensions.SQS;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class SqsFunctions
{
    private readonly ILogger<SqsFunctions> _logger;

    public SqsFunctions(ILogger<SqsFunctions> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessSqsMessage))]
    public void ProcessSqsMessage(
        [SqsTrigger("%SQS_QUEUE_URL%")] Message message)
    {
        _logger.LogInformation("Processing message: {MessageId}", message.MessageId);
        _logger.LogInformation("Message body: {Body}", message.Body);
        
        // Your processing logic here
        // Message is automatically deleted on success
    }
}
```

### Output Binding Example

For isolated worker, use the AWS SDK directly:

```csharp
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

public class SqsOutputFunctions
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsOutputFunctions(IConfiguration configuration)
    {
        _queueUrl = configuration["SQS_OUTPUT_QUEUE_URL"] 
            ?? throw new InvalidOperationException("SQS_OUTPUT_QUEUE_URL not configured");
        _sqsClient = new AmazonSQSClient(); // Uses AWS credential chain
    }

    [Function(nameof(SendMessage))]
    public async Task<IActionResult> SendMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        var message = await new StreamReader(req.Body).ReadToEndAsync();
        
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = message
        });
        
        return new OkObjectResult(new { status = "Message sent" });
    }
}
```

## Configuration

### AWS Credentials

The extension uses the AWS credential chain in this order:
1. Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`)
2. IAM roles (when running in AWS or Azure with federated credentials)
3. AWS CLI credentials file (`~/.aws/credentials`)

### local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SQS_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
    "SQS_OUTPUT_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/123456789012/my-output-queue",
    "AWS_REGION": "us-east-1"
  }
}
```

## Attributes

### SqsTriggerAttribute

Triggers a function when messages are available in an SQS queue.

```csharp
[SqsTrigger(
    queueUrl: "%SQS_QUEUE_URL%",  // Required: Queue URL
    AWSKeyId = null,              // Optional: AWS Access Key ID
    AWSAccessKey = null,          // Optional: AWS Secret Access Key
    Region = null,                // Optional: AWS Region
    MaxNumberOfMessages = 10,     // Optional: Max messages per batch (1-10)
    WaitTimeSeconds = 20,         // Optional: Long polling wait time (0-20)
    VisibilityTimeout = 30,       // Optional: Visibility timeout (seconds)
    AutoDelete = true             // Optional: Auto-delete on success
)]
```

## Advanced Usage

### Binding to Message Object

```csharp
[Function("ProcessFullMessage")]
public void ProcessFullMessage(
    [SqsTrigger("%SQS_QUEUE_URL%")] Message message,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessFullMessage");
    
    logger.LogInformation("Message ID: {Id}", message.MessageId);
    logger.LogInformation("Receipt Handle: {Handle}", message.ReceiptHandle);
    logger.LogInformation("Attributes: {Count}", message.Attributes.Count);
    
    foreach (var attr in message.MessageAttributes)
    {
        logger.LogInformation("Attribute {Key}: {Value}", 
            attr.Key, attr.Value.StringValue);
    }
}
```

### Binding to String (Message Body Only)

```csharp
[Function("ProcessMessageBody")]
public void ProcessMessageBody(
    [SqsTrigger("%SQS_QUEUE_URL%")] string messageBody)
{
    // messageBody contains only the message body content
    Console.WriteLine($"Received: {messageBody}");
}
```

### Async Processing

```csharp
[Function("ProcessAsync")]
public async Task ProcessAsync(
    [SqsTrigger("%SQS_QUEUE_URL%")] Message message)
{
    // Simulate async work
    await Task.Delay(100);
    
    // Process message
    Console.WriteLine($"Processed: {message.MessageId}");
}
```

### Sending Messages with AWS SDK

```csharp
public class SqsSender
{
    private readonly IAmazonSQS _sqs;
    private readonly string _queueUrl;

    public SqsSender(IConfiguration config)
    {
        _sqs = new AmazonSQSClient();
        _queueUrl = config["SQS_OUTPUT_QUEUE_URL"]!;
    }

    [Function("SendWithAttributes")]
    public async Task<IActionResult> SendWithAttributes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = "Hello with attributes",
            DelaySeconds = 5,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["Priority"] = new MessageAttributeValue 
                { 
                    DataType = "Number", 
                    StringValue = "1" 
                },
                ["Source"] = new MessageAttributeValue 
                { 
                    DataType = "String", 
                    StringValue = "AzureFunctions-Isolated" 
                }
            }
        };
        
        var response = await _sqs.SendMessageAsync(request);
        
        return new OkObjectResult(new 
        { 
            status = "sent",
            messageId = response.MessageId 
        });
    }
}
```

## Migration Guide

### From In-Process Model (Azure.WebJobs.Extensions.SQS)

#### 1. Update Project File

```xml
<!-- Change TargetFramework -->
<TargetFramework>net8.0</TargetFramework>
<OutputType>Exe</OutputType>

<!-- Update packages -->
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.23.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.18.1" />
<PackageReference Include="Azure.Functions.Worker.Extensions.SQS" Version="1.0.0" />
```

#### 2. Update Configuration

```json
{
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"  // Changed from "dotnet"
  }
}
```

#### 3. Update Code

**Trigger Changes:**
```csharp
// Old (In-Process)
[FunctionName("Process")]
public void Process(
    [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
    ILogger log)
{ }

// New (Isolated Worker)
[Function("Process")]
public void Process(
    [SqsTrigger("%SQS_QUEUE_URL%")] Message message)
{
    // Inject ILogger via constructor
}
```

**Output Changes:**
```csharp
// Old (In-Process) - Used output binding
[FunctionName("Send")]
public async Task Send(
    [HttpTrigger] HttpRequest req,
    [SqsQueueOut(QueueUrl = "%URL%")] IAsyncCollector<string> messages)
{
    await messages.AddAsync("message");
}

// New (Isolated Worker) - Use AWS SDK directly
[Function("Send")]
public async Task Send(
    [HttpTrigger] HttpRequest req)
{
    var sqs = new AmazonSQSClient();
    await sqs.SendMessageAsync(new SendMessageRequest 
    { 
        QueueUrl = _queueUrl,
        MessageBody = "message" 
    });
}
```

## Troubleshooting

### "Binding type 'sqsTrigger' not registered"

This error means the extension metadata wasn't generated. Ensure:
1. You're using `Microsoft.Azure.Functions.Worker.Sdk` package
2. The project builds successfully
3. Clean and rebuild: `dotnet clean && dotnet build`

### Messages Not Being Processed

1. Check AWS credentials are configured correctly
2. Verify queue URL is correct in configuration
3. Check IAM permissions for SQS actions
4. Review Azure Functions logs for errors
5. Ensure `FUNCTIONS_WORKER_RUNTIME` is set to `dotnet-isolated`

### Permission Issues

Ensure your AWS credentials have these permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "sqs:ReceiveMessage",
        "sqs:DeleteMessage",
        "sqs:GetQueueAttributes",
        "sqs:SendMessage"
      ],
      "Resource": "arn:aws:sqs:*:*:*"
    }
  ]
}
```

## Why Isolated Worker?

The isolated worker model is Microsoft's recommended approach for new Azure Functions because:

- ✅ **Better isolation**: Your function runs in a separate process
- ✅ **More flexibility**: Use any .NET version, not tied to Functions runtime
- ✅ **Easier testing**: Standard .NET patterns without WebJobs dependencies
- ✅ **Future-proof**: Microsoft's focus for new features
- ✅ **Better dependency management**: No conflicts with host dependencies

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](https://github.com/laveeshb/azure-functions-sqs-extension/blob/main/CONTRIBUTING.md).

## License

MIT License - see [LICENSE](https://github.com/laveeshb/azure-functions-sqs-extension/blob/main/LICENSE)

## Links

- [GitHub Repository](https://github.com/laveeshb/azure-functions-sqs-extension)
- [NuGet Package](https://www.nuget.org/packages/Azure.Functions.Worker.Extensions.SQS)
- [In-Process Package](https://www.nuget.org/packages/Azure.WebJobs.Extensions.SQS)
- [Report Issues](https://github.com/laveeshb/azure-functions-sqs-extension/issues)
