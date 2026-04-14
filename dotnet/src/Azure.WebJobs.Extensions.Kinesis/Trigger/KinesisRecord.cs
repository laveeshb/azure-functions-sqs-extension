// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a Kinesis stream record.
/// </summary>
public class KinesisRecord
{
    /// <summary>
    /// The unique identifier of the record within its shard.
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public string? SequenceNumber { get; set; }

    /// <summary>
    /// The approximate time that the record was inserted into the stream.
    /// </summary>
    [JsonPropertyName("approximateArrivalTimestamp")]
    public DateTime ApproximateArrivalTimestamp { get; set; }

    /// <summary>
    /// Identifies which shard in the stream the data record is assigned to.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string? PartitionKey { get; set; }

    /// <summary>
    /// The raw data bytes.
    /// </summary>
    [JsonIgnore]
    public byte[]? DataBytes { get; set; }

    /// <summary>
    /// The data as a base64-encoded string.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data
    {
        get => DataBytes != null ? Convert.ToBase64String(DataBytes) : null;
        set => DataBytes = value != null ? Convert.FromBase64String(value) : null;
    }

    /// <summary>
    /// The encryption type used on the record.
    /// </summary>
    [JsonPropertyName("encryptionType")]
    public string? EncryptionType { get; set; }

    /// <summary>
    /// The shard ID from which the record was retrieved.
    /// </summary>
    [JsonPropertyName("shardId")]
    public string? ShardId { get; set; }

    /// <summary>
    /// The stream name from which the record was retrieved.
    /// </summary>
    [JsonPropertyName("streamName")]
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets the data as a decoded UTF-8 string.
    /// </summary>
    [JsonIgnore]
    public string? DataAsString => DataBytes != null ? Encoding.UTF8.GetString(DataBytes) : null;

    /// <summary>
    /// Deserializes the data to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized data, or default if data is null.</returns>
    public T? GetData<T>()
    {
        if (DataBytes == null)
        {
            return default;
        }

        var json = Encoding.UTF8.GetString(DataBytes);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}

/// <summary>
/// Represents a batch of Kinesis records from a single shard.
/// </summary>
public class KinesisRecordBatch
{
    /// <summary>
    /// The stream name.
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// The shard ID.
    /// </summary>
    public string? ShardId { get; set; }

    /// <summary>
    /// The records in the batch.
    /// </summary>
    public List<KinesisRecord> Records { get; set; } = new();

    /// <summary>
    /// The number of milliseconds behind the latest record in the stream.
    /// </summary>
    public long MillisBehindLatest { get; set; }
}
