# Azure Functions SQS Extension - In-Process Test Application

This test application demonstrates the **In-Process (WebJobs)** hosting model for Azure Functions with SQS bindings.

## In-Process Model Features

- Uses `SqsQueueTriggerAttribute` for triggers
- Uses `SqsQueueOutAttribute` for output bindings
- Supports `IAsyncCollector<T>` for sending messages
- Traditional WebJobs SDK patterns

## Functions Included

### Trigger Functions
- `ProcessSqsMessage` - Basic SQS message processing
- `ProcessSqsMessageAsync` - Async message processing
- `ProcessSqsMessageBody` - String body binding

### Output Functions
- `SendSimpleMessage` - Send simple string messages
- `SendBatchMessages` - Send multiple messages in one invocation
- `SendAdvancedMessage` - Send full Message objects with attributes

## Running the Test Application

1. **Configure AWS Credentials**
   ```bash
   export AWS_ACCESS_KEY_ID=your_key
   export AWS_SECRET_ACCESS_KEY=your_secret
   export AWS_REGION=us-east-1
   ```

2. **Update local.settings.json**
   ```json
   {
     "Values": {
       "SQS_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/your-account/your-queue",
       "SQS_OUTPUT_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/your-account/your-output-queue"
     }
   }
   ```

3. **Build and Run**
   ```bash
   dotnet build
   func start
   ```

4. **Test Triggers**
   Send a message to your SQS queue using AWS CLI:
   ```bash
   aws sqs send-message \
     --queue-url https://sqs.us-east-1.amazonaws.com/your-account/your-queue \
     --message-body "Test message"
   ```

5. **Test Output Bindings**
   ```bash
   # Send simple message
   curl "http://localhost:7071/api/send-simple?message=Hello"
   
   # Send batch messages
   curl "http://localhost:7071/api/send-batch?count=5"
   
   # Send advanced message with attributes
   curl "http://localhost:7071/api/send-advanced?message=HelloWorld"
   ```

## Comparison with Isolated Worker Model

| Feature | In-Process (This App) | Isolated Worker |
|---------|----------------------|-----------------|
| Trigger Attribute | `SqsQueueTriggerAttribute` | `SqsTriggerAttribute` |
| Output Attribute | `SqsQueueOutAttribute` | `SqsOutputAttribute` |
| Output Pattern | `IAsyncCollector<T>` | Return values |
| Runtime | In same process as host | Separate process |
| Performance | Slightly faster | Better isolation |
| Recommended | Legacy apps | New apps |
