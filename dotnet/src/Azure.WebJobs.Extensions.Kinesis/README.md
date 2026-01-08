# Azure.WebJobs.Extensions.Kinesis

Azure Functions WebJobs extension for Amazon Kinesis Data Streams. This package provides trigger and output bindings for Kinesis.

## Installation

```bash
dotnet add package Extensions.Azure.WebJobs.Kinesis
```

## Trigger Binding

The Kinesis trigger uses **poll-based** message retrieval using the Kinesis GetRecords API. The trigger automatically manages shard iterators and processes records from all shards in the stream.

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.Kinesis;

public class KinesisFunctions
{
    [FunctionName("ProcessRecord")]
    public static async Task Run(
        [KinesisTrigger(
            StreamName = "my-stream",
            Region = "us-east-1",
            BatchSize = 100,
            StartingPosition = "TRIM_HORIZON")] KinesisRecord record,
        ILogger log)
    {
        log.LogInformation($"Sequence Number: {record.SequenceNumber}");
        log.LogInformation($"Partition Key: {record.PartitionKey}");
        log.LogInformation($"Shard ID: {record.ShardId}");
        
        // Get data as string
        string data = record.DataAsString;
        log.LogInformation($"Data: {data}");
        
        // Or deserialize to a strongly-typed object
        var myEvent = record.GetData<MyEventType>();
        log.LogInformation($"Event ID: {myEvent.Id}");
    }
}
```

### KinesisTriggerAttribute Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StreamName` | `string` | Required | Name of the Kinesis stream |
| `Region` | `string` | From env | AWS region |
| `BatchSize` | `int` | 100 | Max records per GetRecords call |
| `PollingIntervalMs` | `int` | 1000 | Milliseconds between polls |
| `StartingPosition` | `string` | TRIM_HORIZON | Where to start reading |
| `StartingPositionTimestamp` | `string` | null | ISO 8601 timestamp for AT_TIMESTAMP |

### Starting Positions

- **TRIM_HORIZON**: Start from the oldest record in the shard
- **LATEST**: Start from the newest record (only new records)
- **AT_TIMESTAMP**: Start from a specific timestamp (requires `StartingPositionTimestamp`)

```csharp
// Start from the beginning
[KinesisTrigger(StreamName = "my-stream", StartingPosition = "TRIM_HORIZON")]

// Start from now (only new records)
[KinesisTrigger(StreamName = "my-stream", StartingPosition = "LATEST")]

// Start from a specific time
[KinesisTrigger(
    StreamName = "my-stream", 
    StartingPosition = "AT_TIMESTAMP",
    StartingPositionTimestamp = "2024-01-01T00:00:00Z")]
```

### KinesisRecord Properties

| Property | Type | Description |
|----------|------|-------------|
| `SequenceNumber` | `string` | Unique sequence number within the shard |
| `PartitionKey` | `string` | Partition key used for routing |
| `ShardId` | `string` | ID of the shard containing the record |
| `ApproximateArrivalTimestamp` | `DateTime?` | When the record arrived at Kinesis |
| `DataBytes` | `byte[]` | Raw record data |
| `DataAsString` | `string` | Data decoded as UTF-8 string |
| `EncryptionType` | `string` | Encryption type (NONE or KMS) |

### Typed Deserialization

```csharp
// Deserialize JSON data to a strongly-typed object
var eventData = record.GetData<MyEventType>();

// Or work with raw data
byte[] bytes = record.DataBytes;
string text = record.DataAsString;
```

## Output Binding

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.Kinesis;

public class KinesisFunctions
{
    [FunctionName("SendRecord")]
    [return: KinesisOut(StreamName = "my-stream", Region = "us-east-1")]
    public static KinesisMessage Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        return new KinesisMessage
        {
            PartitionKey = "user-123",
            Data = "{\"event\": \"click\", \"timestamp\": \"2024-01-01T00:00:00Z\"}"
        };
    }
}
```

### Usage (Isolated Worker)

```csharp
using Azure.Functions.Worker.Extensions.Kinesis;

public class KinesisFunctions
{
    [Function("SendRecord")]
    [KinesisOutput("my-stream", Region = "us-east-1")]
    public static KinesisRecord Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        return new KinesisRecord
        {
            PartitionKey = "user-123",
            Data = "{\"event\": \"click\", \"timestamp\": \"2024-01-01T00:00:00Z\"}"
        };
    }
}
```

## Configuration

Configure AWS credentials in `local.settings.json`:

```json
{
  "Values": {
    "AWS_ACCESS_KEY_ID": "your-access-key",
    "AWS_SECRET_ACCESS_KEY": "your-secret-key",
    "AWS_REGION": "us-east-1"
  }
}
```
