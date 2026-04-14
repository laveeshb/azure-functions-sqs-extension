// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

/// <summary>
/// Represents a record to be sent to Amazon Kinesis.
/// </summary>
public class KinesisMessage
{
    /// <summary>
    /// Gets or sets the partition key for the record.
    /// Determines which shard the record is routed to.
    /// </summary>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets the data as a string.
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the data as bytes.
    /// </summary>
    public byte[]? DataBytes { get; set; }

    /// <summary>
    /// Gets or sets the stream name. If not specified, uses the StreamName from the attribute.
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets or sets the explicit hash key for the record.
    /// </summary>
    public string? ExplicitHashKey { get; set; }

    /// <summary>
    /// Gets or sets the sequence number for ordering guarantee.
    /// </summary>
    public string? SequenceNumberForOrdering { get; set; }
}
