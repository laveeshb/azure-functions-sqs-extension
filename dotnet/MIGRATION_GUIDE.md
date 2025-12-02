# Migration Guide

This guide helps you migrate between different versions of the Azure Functions SQS Extension.

## Current State (v1.0.0)

The Azure Functions SQS Extension now provides **two separate packages** to support both hosting models:

| Package | Model | Status | Azure Functions Platform Support |
|---------|-------|--------|----------------------------------|
| **Azure.WebJobs.Extensions.SQS** | In-process | ✅ Available | [Until Nov 10, 2026](https://aka.ms/azure-functions-retirements/in-process-model) |
| **Azure.Functions.Worker.Extensions.SQS** | Isolated worker | ✅ Available (Recommended) | Ongoing |

> ⚠️ **Important**: Microsoft Azure Functions will end support for the in-process hosting model on **November 10, 2026**. After this date, in-process functions will no longer be supported by the Azure Functions runtime. [Learn more about Microsoft's retirement timeline](https://aka.ms/azure-functions-retirements/in-process-model).

### Which Package Should I Use?

- **New projects**: Use `Azure.Extensions.Functions.SQS` (isolated worker model)
- **Existing in-process apps**: Can continue using `Azure.Extensions.WebJobs.SQS` until **November 10, 2026** when Microsoft ends Azure Functions platform support for in-process model
- **Legacy apps**: Migrate from old `AzureFunctions.Extension.SQS` package

## Migration Scenarios

### Scenario 1: Legacy Package → In-Process Model

Migrating from `AzureFunctions.Extension.SQS` (v2.x/v3.x) to `Azure.Extensions.WebJobs.SQS` (v1.x).

#### 1. Update Package Reference

```xml
<!-- Remove old package -->
<PackageReference Include="AzureFunctions.Extension.SQS" Version="3.0.0" />

<!-- Add new package -->
<PackageReference Include="Azure.Extensions.WebJobs.SQS" Version="1.0.0" />
```

#### 2. Update Namespace

```csharp
// Old
using Extensions.SQS;

// New
using Azure.WebJobs.Extensions.SQS;
```

#### 3. Update Code (Minimal Changes)

**Trigger binding** - mostly compatible:
```csharp
// Old and New - same syntax
[FunctionName("ProcessMessage")]
public void Run(
    [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
    ILogger log)
{
    log.LogInformation("Message: {Body}", message.Body);
}
```

**Output binding** - same pattern:
```csharp
// Old and New - same syntax
[FunctionName("SendMessage")]
public void Run(
    [HttpTrigger] HttpRequest req,
    [SqsQueueOut(QueueUrl = "%SQS_QUEUE_URL%")] out SqsQueueMessage message,
    ILogger log)
{
    message = new SqsQueueMessage { Body = "Hello" };
}
```

#### 4. Update Configuration (No Changes Needed)

`local.settings.json` and Application Settings remain the same:
```json
{
  "Values": {
    "AWS_ACCESS_KEY_ID": "your-key",
    "AWS_SECRET_ACCESS_KEY": "your-secret",
    "AWS_REGION": "us-east-1",
    "SQS_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/..."
  }
}
```

### Scenario 2: Legacy Package → Isolated Worker Model (Recommended)

Migrating from `AzureFunctions.Extension.SQS` to `Azure.Extensions.Functions.SQS`.

#### 1. Update Project File

```xml
<!-- Change target framework if needed -->
<TargetFramework>net8.0</TargetFramework>
<AzureFunctionsVersion>v4</AzureFunctionsVersion>

<!-- Remove old packages -->
<PackageReference Include="AzureFunctions.Extension.SQS" Version="3.0.0" />
<PackageReference Include="Microsoft.Azure.WebJobs" Version="3.0.41" />
<PackageReference Include="Microsoft.NET.Sdk.Functions" Version="4.x.x" />

<!-- Add isolated worker packages -->
<PackageReference Include="Azure.Extensions.Functions.SQS" Version="1.0.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.23.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.18.1" />
```

#### 2. Update Program.cs

Create/update `Program.cs` for isolated worker:
```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.SQS;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Register Amazon SQS client
        services.AddSingleton<IAmazonSQS>(sp => new AmazonSQSClient());
    })
    .Build();

host.Run();
```

#### 3. Update Namespaces

```csharp
// Remove
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

// Add
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Functions.Worker.Extensions.SQS;
```

#### 4. Update Function Code

**Trigger binding:**
```csharp
// OLD (In-Process)
[FunctionName("ProcessMessage")]
public void Run(
    [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
    ILogger log)
{
    log.LogInformation("Message: {Body}", message.Body);
}

// NEW (Isolated Worker)
[Function("ProcessMessage")]
public void Run(
    [SqsTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessMessage");
    logger.LogInformation("Message: {Body}", message.Body);
}
```

**Output binding:**
```csharp
// OLD (In-Process)
[FunctionName("SendMessage")]
public void Run(
    [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
    [SqsQueueOut(QueueUrl = "%SQS_QUEUE_URL%")] out SqsQueueMessage message)
{
    message = new SqsQueueMessage { Body = "Hello" };
}

// NEW (Isolated Worker) - Use IAmazonSQS directly
public class SqsFunctions
{
    private readonly IAmazonSQS _sqsClient;

    public SqsFunctions(IAmazonSQS sqsClient)
    {
        _sqsClient = sqsClient;
    }

    [Function("SendMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var queueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");
        
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = "Hello"
        });

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
```

#### 5. Update host.json

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  }
}
```

#### 6. Update local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AWS_ACCESS_KEY_ID": "your-key",
    "AWS_SECRET_ACCESS_KEY": "your-secret",
    "AWS_REGION": "us-east-1",
    "SQS_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/..."
  }
}
```

### Scenario 3: In-Process → Isolated Worker

Migrating from `Azure.Extensions.WebJobs.SQS` to `Azure.Extensions.Functions.SQS`.

Follow the same steps as **Scenario 2**, but:
- Start with simpler namespace changes
- Project structure is already modern
- Main changes are in function signatures and DI setup

## Key Differences Between Models

| Feature | In-Process | Isolated Worker |
|---------|------------|-----------------|
| **Package** | Azure.Extensions.WebJobs.SQS | Azure.Extensions.Functions.SQS |
| **Trigger Attribute** | `[SqsQueueTrigger]` | `[SqsTrigger]` |
| **Output Attribute** | `[SqsQueueOut]` with `out` parameter | Use `IAmazonSQS` directly |
| **Function Attribute** | `[FunctionName]` | `[Function]` |
| **Logging** | `ILogger` parameter | `FunctionContext.GetLogger()` |
| **HTTP Trigger** | `HttpRequest` | `HttpRequestData` |
| **HTTP Response** | `IActionResult` | `HttpResponseData` |
| **Dependency Injection** | Constructor or method injection | Constructor injection only |
| **Runtime** | Same process as host | Separate process |

## Testing After Migration

### 1. Local Testing

```bash
# In-process
cd test/Extensions.SQS.Test.InProcess
func start

# Isolated worker
cd test/Extensions.SQS.Test.Isolated
func start
```

### 2. Verify Trigger

Send a test message to your SQS queue:
```bash
aws sqs send-message \
  --queue-url "your-queue-url" \
  --message-body "Test message"
```

### 3. Verify Output

Check your function logs and output queue for sent messages.

## Troubleshooting

### Issue: Function not triggering

**Solution:**
- Verify AWS credentials in Application Settings
- Check queue URL is correct
- Ensure IAM permissions allow `sqs:ReceiveMessage`, `sqs:DeleteMessage`
- Check function app logs for errors

### Issue: "Attribute not found" error

**Solution:**
- In-process: Use `[SqsQueueTrigger]` and `[SqsQueueOut]`
- Isolated worker: Use `[SqsTrigger]`, no output attribute (use `IAmazonSQS`)

### Issue: DI not working in isolated worker

**Solution:**
- Ensure `IAmazonSQS` is registered in `Program.cs`:
  ```csharp
  services.AddSingleton<IAmazonSQS>(sp => new AmazonSQSClient());
  ```

## Timeline Recommendations

- **2024-2025**: Migrate to isolated worker model for new projects
- **By Q4 2025**: Plan migration for existing in-process apps
- **By November 10, 2026**: Complete migration before Microsoft ends Azure Functions platform support for in-process model ([official timeline](https://aka.ms/azure-functions-retirements/in-process-model))

## Resources

- [In-Process Package README](./src/Azure.WebJobs.Extensions.SQS/README.md)
- [Isolated Worker Package README](./src/Azure.Functions.Worker.Extensions.SQS/README.md)
- [Azure Functions Isolated Worker Guide](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Migrate to Isolated Worker (Microsoft Docs)](https://learn.microsoft.com/azure/azure-functions/migrate-dotnet-to-isolated-model)

## Need Help?

Open an issue on [GitHub](https://github.com/laveeshb/azure-functions-sqs-extension/issues) for migration assistance.
