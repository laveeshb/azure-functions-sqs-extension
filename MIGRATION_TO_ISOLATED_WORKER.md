# Migration to Isolated Worker Model

## Current State

The Azure Functions SQS Extension (v3.0.0) is currently built using the **WebJobs SDK**, which supports the **in-process model** for Azure Functions. This model is being deprecated by Microsoft, with end-of-support on **November 10, 2026**.

### What This Means

- ✅ **Extension works perfectly** with in-process Azure Functions (.NET 6/8)
- ⚠️ **Not compatible** with isolated worker process model
- 📅 **In-process model deprecated** - Nov 10, 2026

## Why the Isolated Worker Model Requires Significant Changes

The isolated worker model uses a completely different architecture:

1. **Different SDK**: Uses `Microsoft.Azure.Functions.Worker` instead of `Microsoft.Azure.WebJobs`
2. **Different Binding System**: Custom bindings require implementing different interfaces
3. **Different Attribute System**: Attributes must inherit from different base classes
4. **No IAsyncCollector**: Output bindings work differently (return values or multi-output types)
5. **Separate Process**: Functions run in a separate process from the host

## Migration Options

### Option 1: Dual Support (Recommended)

Create separate packages:
- `AzureFunctions.Extension.SQS` (current, in-process) - v3.x
- `AzureFunctions.Extension.SQS.Worker` (new, isolated worker) - v4.x

**Pros:**
- Supports both models during transition period
- Users can migrate at their own pace
- Clear separation of concerns

**Cons:**
- Two codebases to maintain
- More complex release process

### Option 2: Isolated Worker Only

Drop in-process support, focus entirely on isolated worker model.

**Pros:**
- Single codebase
- Future-proof immediately
- Cleaner architecture

**Cons:**
- Breaking change for existing users
- Requires immediate migration for all users

### Option 3: Continue In-Process Until 2026

Keep the current implementation, plan migration closer to deprecation date.

**Pros:**
- More time to plan
- Existing users not disrupted
- Can wait for better isolated worker tooling

**Cons:**
- Still need to migrate eventually
- Less time to test in production

## What Needs to Change for Isolated Worker

### 1. Project Structure
```
dotnet/
  src/
    Extensions.SQS/              # In-process (current)
    Extensions.SQS.Worker/       # Isolated worker (new)
```

### 2. Package Dependencies
Replace:
- `Microsoft.Azure.WebJobs` → `Microsoft.Azure.Functions.Worker.Extensions.Abstractions`
- Remove `IAsyncCollector<T>` patterns
- Add Worker SDK dependencies

### 3. Trigger Attribute
```csharp
// Current (WebJobs)
public class SqsQueueTriggerAttribute : Attribute { }

// Isolated Worker
[AttributeUsage(AttributeTargets.Parameter)]
public class SqsQueueTriggerAttribute : TriggerBindingAttribute { }
```

### 4. Trigger Implementation
- Implement `IInputBindingProvider<TAttribute>`
- Create converter from SQS message to function parameter
- Use `FunctionContext` instead of execution context

### 5. Output Binding
```csharp
// Current (WebJobs)
[SqsQueueOut("queue-url")] out SqsQueueMessage message

// Isolated Worker - Return Value
[Function("SendMessage")]
[SqsQueueOutput("queue-url")]
public SqsQueueMessage Run([HttpTrigger] HttpRequest req)

// Isolated Worker - Multi-Output
public class MyOutputs
{
    [SqsQueueOutput("queue-url")]
    public SqsQueueMessage? Message { get; set; }
    
    public IActionResult HttpResponse { get; set; }
}
```

### 6. Extension Startup
```csharp
// Current (WebJobs)
[assembly: WebJobsStartup(typeof(SqsExtensionStartup))]

// Isolated Worker
public static class WorkerExtensions
{
    public static IHostBuilder ConfigureSqsExtension(this IHostBuilder builder)
    {
        builder.ConfigureFunctionsWorkerDefaults(worker =>
        {
            worker.Services.AddSingleton<ISqsQueueTriggerBindingProvider>();
        });
        return builder;
    }
}
```

## Recommended Path Forward

### Phase 1: Document Current State ✅
- ✅ Update README to clarify in-process model support
- ✅ Add this migration guide
- ✅ Create in-process test sample

### Phase 2: Test In-Process Implementation (Next Step)
- Create simple in-process test app
- Verify trigger and output bindings work
- Publish v3.0.0 with in-process support

### Phase 3: Plan Isolated Worker (Future)
- Decide on dual support vs. isolated-only
- Create detailed implementation plan
- Set up new project structure

### Phase 4: Implement Isolated Worker (Major Effort)
- Implement new binding system
- Create comprehensive tests
- Update documentation
- Release v4.0.0

## Timeline Recommendation

- **Now - Q1 2025**: Release v3.0.0 (in-process, modernized dependencies)
- **Q2-Q3 2025**: Implement and test isolated worker support
- **Q4 2025**: Release v4.0.0 (isolated worker)
- **2026**: Support both models until Nov 2026 deadline
- **Post Nov 2026**: Isolated worker only

## Testing the Current Extension

To test the current in-process extension:

1. Create a new in-process Functions app
2. Reference the extension
3. Use WebJobs-style attributes
4. Run with Azure Functions v4 runtime

See the samples in `dotnet/test/` for working examples.

## Resources

- [Azure Functions Isolated Worker Guide](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [Migrate to Isolated Worker](https://learn.microsoft.com/en-us/azure/azure-functions/migrate-dotnet-to-isolated-model)
- [Custom Bindings in Isolated Worker](https://github.com/Azure/azure-functions-dotnet-worker/wiki/Custom-Bindings)
- [In-Process Model Retirement Announcement](https://aka.ms/azure-functions-retirements/in-process-model)

## Questions?

Open an issue on GitHub to discuss the migration strategy or ask questions about isolated worker support.
