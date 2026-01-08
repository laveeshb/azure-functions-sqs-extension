// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

/// <summary>
/// Represents a message to be published to Amazon SNS.
/// </summary>
public class SnsMessage
{
    /// <summary>
    /// Gets or sets the message body to publish.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the subject of the message (for email endpoints).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the ARN of the topic to publish to.
    /// If not specified, uses the TopicArn from the attribute.
    /// </summary>
    public string? TopicArn { get; set; }

    /// <summary>
    /// Gets or sets the message group ID for FIFO topics.
    /// </summary>
    public string? MessageGroupId { get; set; }

    /// <summary>
    /// Gets or sets the message deduplication ID for FIFO topics.
    /// </summary>
    public string? MessageDeduplicationId { get; set; }

    /// <summary>
    /// Gets or sets message attributes as a dictionary.
    /// </summary>
    public Dictionary<string, MessageAttributeValue>? MessageAttributes { get; set; }
}

/// <summary>
/// Represents an SNS message attribute value.
/// </summary>
public class MessageAttributeValue
{
    /// <summary>
    /// Gets or sets the data type (String, Number, Binary, or String.Array).
    /// </summary>
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Gets or sets the binary value.
    /// </summary>
    public byte[]? BinaryValue { get; set; }
}
