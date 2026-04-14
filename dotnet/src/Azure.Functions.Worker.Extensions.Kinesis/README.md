# Azure Functions Kinesis Extension (Isolated Worker)

This package provides Azure Functions bindings for AWS Kinesis Data Streams, enabling you to trigger functions from Kinesis stream records and write records to streams.

## Installation

```bash
dotnet add package Extensions.Azure.Functions.Worker.Kinesis
```

## Usage

### Kinesis Trigger

Process records from a Kinesis stream:

```csharp
[Function("ProcessKinesisRecord")]
public void Run(
    [KinesisTrigger("my-stream",
        AWSKeyId = "%AWS_ACCESS_KEY_ID%",
        AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
        Region = "us-east-1",
        StartingPosition = "LATEST")] 
    KinesisRecord record,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessKinesisRecord");
    logger.LogInformation($"Sequence Number: {record.SequenceNumber}");
    logger.LogInformation($"Partition Key: {record.PartitionKey}");
    logger.LogInformation($"Data: {record.DecodedData}");
}
```

### Strongly-Typed Records

Use generic `KinesisRecord<T>` for type-safe data access:

```csharp
public class SensorData
{
    public string SensorId { get; set; }
    public double Temperature { get; set; }
    public DateTime Timestamp { get; set; }
}

[Function("ProcessSensorData")]
public void Run(
    [KinesisTrigger("sensor-stream",
        Region = "us-east-1",
        BatchSize = 50)] 
    KinesisRecord<SensorData> record,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessSensorData");
    logger.LogInformation($"Sensor {record.Data?.SensorId}: {record.Data?.Temperature}°C");
}
```

### Kinesis Output

Write records to a Kinesis stream:

```csharp
[Function("WriteToKinesis")]
[KinesisOutput("my-stream",
    AWSKeyId = "%AWS_ACCESS_KEY_ID%",
    AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
    Region = "us-east-1",
    PartitionKey = "default-partition")]
public KinesisOutputRecord Run(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
    FunctionContext context)
{
    return new KinesisOutputRecord
    {
        Data = new { message = "Hello from Azure Functions!", timestamp = DateTime.UtcNow },
        PartitionKey = "my-partition-key"
    };
}
```

## Configuration

### Trigger Attributes

| Property | Description | Required |
|----------|-------------|----------|
| StreamName | Kinesis stream name | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| StartingPosition | Starting position (TRIM_HORIZON, LATEST, AT_TIMESTAMP) | No (default: TRIM_HORIZON) |
| BatchSize | Max records per batch | No (default: 100) |
| PollingIntervalMs | Polling interval in milliseconds | No (default: 1000) |

### Output Attributes

| Property | Description | Required |
|----------|-------------|----------|
| StreamName | Kinesis stream name | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| PartitionKey | Default partition key | No |

\* If not provided, the AWS SDK credential chain will be used.

## Starting Positions

- **TRIM_HORIZON**: Start reading from the oldest record in the shard
- **LATEST**: Start reading from the newest record (only new records)
- **AT_TIMESTAMP**: Start reading from a specific timestamp

## Data Encoding

Kinesis data is base64-encoded. The `KinesisRecord` class provides:
- `Data`: Raw base64-encoded string
- `DecodedData`: Automatically decoded UTF-8 string

For binary data or custom deserialization, use `KinesisRecord<T>`.

## Architecture

```
Kinesis Stream → Azure Function (polls stream directly) → Your Code
                                      ↓
Your Code → Kinesis Output → Kinesis Stream
```

Note: Unlike EventBridge, SNS, and S3 triggers (which use SQS queues), the Kinesis trigger polls the stream directly using GetShardIterator and GetRecords APIs.

## License

MIT
