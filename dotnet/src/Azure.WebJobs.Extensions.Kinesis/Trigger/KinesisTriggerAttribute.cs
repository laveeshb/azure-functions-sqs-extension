// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using Microsoft.Azure.WebJobs.Description;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon Kinesis stream records.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class KinesisTriggerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisTriggerAttribute"/> class.
    /// </summary>
    /// <param name="streamName">The name of the Kinesis stream.</param>
    public KinesisTriggerAttribute(string streamName)
    {
        StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
    }

    /// <summary>
    /// Gets the name of the Kinesis stream.
    /// Supports %AppSetting% syntax for configuration binding.
    /// </summary>
    [AutoResolve]
    public string StreamName { get; }

    /// <summary>
    /// Gets or sets the AWS Access Key ID. If not specified, uses AWS credential chain.
    /// Supports %AppSetting% syntax for configuration binding.
    /// </summary>
    [AutoResolve]
    public string? AWSKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS Secret Access Key. If not specified, uses AWS credential chain.
    /// Supports %AppSetting% syntax for configuration binding.
    /// </summary>
    [AutoResolve]
    public string? AWSAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the AWS Region (e.g., "us-east-1"). If not specified, uses AWS credential chain.
    /// Supports %AppSetting% syntax for configuration binding.
    /// </summary>
    [AutoResolve]
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the starting position for reading records when no checkpoint exists.
    /// Valid values: TRIM_HORIZON (oldest), LATEST (newest), AT_TIMESTAMP.
    /// Default is TRIM_HORIZON.
    /// </summary>
    public string StartingPosition { get; set; } = "TRIM_HORIZON";

    /// <summary>
    /// Gets or sets the timestamp to start reading from when StartingPosition is AT_TIMESTAMP.
    /// </summary>
    public DateTime? StartingTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of records to retrieve per shard per batch.
    /// Default is 100, maximum is 10000.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the polling interval in milliseconds between Kinesis GetRecords calls.
    /// Default is 1000 (1 second). Kinesis has a limit of 5 reads per second per shard.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to checkpoint (save shard iterator position) after each batch.
    /// Default is true. When false, the function will restart from StartingPosition on restart.
    /// </summary>
    public bool EnableCheckpointing { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the consumer application for checkpointing.
    /// Default is the function app name. Used to track shard iterator positions.
    /// </summary>
    [AutoResolve]
    public string? ConsumerName { get; set; }
}
