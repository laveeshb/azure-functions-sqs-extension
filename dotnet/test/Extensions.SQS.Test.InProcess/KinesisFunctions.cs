namespace Azure.Functions.Extensions.SQS.Test.InProcess;

using Azure.WebJobs.Extensions.Kinesis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Sample functions demonstrating Kinesis trigger and output bindings.
/// </summary>
public class KinesisFunctions
{
    #region Trigger Functions

    /// <summary>
    /// Kinesis Stream Trigger - polls records from a Kinesis data stream.
    /// 
    /// The trigger uses GetRecords API to poll shards and automatically manages
    /// shard iterators. It starts from TRIM_HORIZON by default (oldest record).
    /// </summary>
    [FunctionName(nameof(ProcessKinesisRecord))]
    public async Task ProcessKinesisRecord(
        [KinesisTrigger("%KINESIS_STREAM_NAME%",
            BatchSize = 100,
            PollingIntervalMs = 1000,
            StartingPosition = "TRIM_HORIZON")] KinesisRecord record,
        ILogger log)
    {
        log.LogInformation("=== Kinesis Record Received ===");
        log.LogInformation("Sequence Number: {SequenceNumber}", record.SequenceNumber);
        log.LogInformation("Partition Key: {PartitionKey}", record.PartitionKey);
        log.LogInformation("Shard ID: {ShardId}", record.ShardId);
        log.LogInformation("Arrival Time: {ArrivalTime}", record.ApproximateArrivalTimestamp);
        log.LogInformation("Data: {Data}", record.DataAsString);
        
        // For typed deserialization, use:
        // var eventData = record.GetData<MyEventType>();
        
        await Task.CompletedTask;
        log.LogInformation("Kinesis record processed successfully");
    }

    /// <summary>
    /// Kinesis trigger starting from LATEST (only new records).
    /// </summary>
    [FunctionName(nameof(ProcessLatestKinesisRecords))]
    public async Task ProcessLatestKinesisRecords(
        [KinesisTrigger("%KINESIS_STREAM_NAME_LATEST%",
            StartingPosition = "LATEST")] KinesisRecord record,
        ILogger log)
    {
        log.LogInformation("New Kinesis record: {SequenceNumber} - {Data}", 
            record.SequenceNumber, record.DataAsString);
        
        await Task.CompletedTask;
    }

    #endregion

    #region Output Functions

    /// <summary>
    /// Sends a record to a Kinesis stream using output binding.
    /// Example: curl -X POST "http://localhost:7071/api/kinesis/send?partitionKey=user-123&data=Hello"
    /// </summary>
    [FunctionName(nameof(SendToKinesis))]
    public async Task<IActionResult> SendToKinesis(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "kinesis/send")] HttpRequest req,
        [KinesisOut(StreamName = "%KINESIS_STREAM_NAME%")] IAsyncCollector<KinesisMessage> records,
        ILogger log)
    {
        var partitionKey = req.Query["partitionKey"].ToString();
        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = Guid.NewGuid().ToString();
        }

        var data = req.Query["data"].ToString();
        if (string.IsNullOrEmpty(data))
        {
            data = System.Text.Json.JsonSerializer.Serialize(new
            {
                eventType = "sample",
                timestamp = DateTime.UtcNow
            });
        }

        await records.AddAsync(new KinesisMessage
        {
            PartitionKey = partitionKey,
            Data = data
        });

        log.LogInformation("Sent record to Kinesis: {PartitionKey}", partitionKey);

        return new OkObjectResult(new
        {
            status = "Record sent to Kinesis",
            partitionKey,
            dataLength = data.Length
        });
    }

    /// <summary>
    /// Sends multiple records to Kinesis in batch.
    /// Example: curl -X POST "http://localhost:7071/api/kinesis/send-batch?count=10"
    /// </summary>
    [FunctionName(nameof(SendBatchToKinesis))]
    public async Task<IActionResult> SendBatchToKinesis(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "kinesis/send-batch")] HttpRequest req,
        [KinesisOut(StreamName = "%KINESIS_STREAM_NAME%")] IAsyncCollector<KinesisMessage> records,
        ILogger log)
    {
        var count = int.TryParse(req.Query["count"], out var c) ? c : 5;
        var partitionKey = req.Query["partitionKey"].ToString();
        if (string.IsNullOrEmpty(partitionKey))
        {
            partitionKey = "batch-" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        for (int i = 0; i < count; i++)
        {
            await records.AddAsync(new KinesisMessage
            {
                PartitionKey = partitionKey,
                Data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    index = i,
                    batchId = partitionKey,
                    timestamp = DateTime.UtcNow
                })
            });
        }

        log.LogInformation("Sent {Count} records to Kinesis with partition key: {PartitionKey}", count, partitionKey);

        return new OkObjectResult(new
        {
            status = "Batch records sent to Kinesis",
            partitionKey,
            count
        });
    }

    /// <summary>
    /// Sends binary data to Kinesis stream.
    /// </summary>
    [FunctionName(nameof(SendBinaryToKinesis))]
    public async Task<IActionResult> SendBinaryToKinesis(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "kinesis/send-binary")] HttpRequest req,
        [KinesisOut(StreamName = "%KINESIS_STREAM_NAME%")] IAsyncCollector<KinesisMessage> records,
        ILogger log)
    {
        // Read request body as bytes
        using var memoryStream = new MemoryStream();
        await req.Body.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        await records.AddAsync(new KinesisMessage
        {
            PartitionKey = req.Query["partitionKey"].ToString() ?? "binary-data",
            DataBytes = bytes
        });

        log.LogInformation("Sent binary data to Kinesis: {Size} bytes", bytes.Length);

        return new OkObjectResult(new
        {
            status = "Binary data sent to Kinesis",
            size = bytes.Length
        });
    }

    #endregion
}
