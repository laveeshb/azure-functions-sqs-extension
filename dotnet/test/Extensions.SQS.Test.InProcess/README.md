# Azure Functions AWS Extensions - In-Process Test Application

This test application demonstrates the **In-Process (WebJobs)** hosting model for Azure Functions with AWS service bindings.

## In-Process Model Features

- Uses `SqsQueueTriggerAttribute` for SQS triggers
- Uses `SnsTriggerAttribute` for SNS webhook triggers
- Uses `EventBridgeTriggerAttribute` for EventBridge webhook triggers
- Uses `KinesisTriggerAttribute` for Kinesis stream triggers
- Uses `S3Attribute` for S3 input bindings
- Uses `*OutAttribute` for output bindings
- Supports `IAsyncCollector<T>` for sending messages

## Functions Included

### SQS Functions
- `ProcessSqsMessage` - Basic SQS message processing
- `ProcessSqsMessageAsync` - Async message processing
- `SendSimpleMessage` - Send simple string messages
- `SendBatchMessages` - Send multiple messages

### SNS Functions
- `ProcessSnsNotification` - Receive SNS webhook notifications
- `PublishToSns` - Publish messages to SNS topics
- `PublishToSnsWithAttributes` - Publish with message attributes

### EventBridge Functions
- `ProcessEventBridgeEvent` - Receive events via API Destinations
- `SendToEventBridge` - Send events to EventBridge
- `SendBatchToEventBridge` - Send multiple events

### S3 Functions
- `ReadS3AsString` - Read S3 objects as strings
- `ReadS3AsBytes` - Read S3 objects as byte arrays
- `ReadS3Object` - Read with full metadata
- `GetDocument` - Dynamic key binding with route params
- `UploadToS3` - Upload content to S3
- `UploadJsonToS3` - Upload JSON with metadata

### Kinesis Functions
- `ProcessKinesisRecord` - Process Kinesis stream records
- `ProcessLatestKinesisRecords` - Start from latest record
- `SendToKinesis` - Send records to Kinesis
- `SendBatchToKinesis` - Send multiple records
- `SendBinaryToKinesis` - Send binary data

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
       "SQS_OUTPUT_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/your-account/your-output-queue",
       "SNS_TOPIC_ARN": "arn:aws:sns:us-east-1:your-account:your-topic",
       "EVENTBRIDGE_BUS_NAME": "default",
       "S3_BUCKET_NAME": "your-bucket-name",
       "KINESIS_STREAM_NAME": "your-stream-name"
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
