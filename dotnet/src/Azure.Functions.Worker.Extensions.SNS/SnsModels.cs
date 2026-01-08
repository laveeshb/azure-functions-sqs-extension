namespace Azure.Functions.Worker.Extensions.SNS;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an SNS notification received via SQS subscription.
/// </summary>
public class SnsNotification
{
    /// <summary>
    /// The type of notification (e.g., "Notification", "SubscriptionConfirmation").
    /// </summary>
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    /// <summary>
    /// A unique identifier for the message.
    /// </summary>
    [JsonPropertyName("MessageId")]
    public string? MessageId { get; set; }

    /// <summary>
    /// The ARN of the topic that published the message.
    /// </summary>
    [JsonPropertyName("TopicArn")]
    public string? TopicArn { get; set; }

    /// <summary>
    /// The subject of the message (optional).
    /// </summary>
    [JsonPropertyName("Subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// The message content.
    /// </summary>
    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    /// <summary>
    /// The timestamp when the message was published.
    /// </summary>
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The signature version.
    /// </summary>
    [JsonPropertyName("SignatureVersion")]
    public string? SignatureVersion { get; set; }

    /// <summary>
    /// The signature for message verification.
    /// </summary>
    [JsonPropertyName("Signature")]
    public string? Signature { get; set; }

    /// <summary>
    /// URL to the signing certificate.
    /// </summary>
    [JsonPropertyName("SigningCertURL")]
    public string? SigningCertUrl { get; set; }

    /// <summary>
    /// URL to unsubscribe from the topic.
    /// </summary>
    [JsonPropertyName("UnsubscribeURL")]
    public string? UnsubscribeUrl { get; set; }

    /// <summary>
    /// Message attributes.
    /// </summary>
    [JsonPropertyName("MessageAttributes")]
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; set; }
}

/// <summary>
/// Represents an SNS notification with a strongly-typed message payload.
/// </summary>
/// <typeparam name="TMessage">The type of the message content.</typeparam>
public class SnsNotification<TMessage>
{
    /// <summary>
    /// The type of notification.
    /// </summary>
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    /// <summary>
    /// A unique identifier for the message.
    /// </summary>
    [JsonPropertyName("MessageId")]
    public string? MessageId { get; set; }

    /// <summary>
    /// The ARN of the topic that published the message.
    /// </summary>
    [JsonPropertyName("TopicArn")]
    public string? TopicArn { get; set; }

    /// <summary>
    /// The subject of the message.
    /// </summary>
    [JsonPropertyName("Subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// The strongly-typed message content.
    /// </summary>
    [JsonPropertyName("Message")]
    public TMessage? Message { get; set; }

    /// <summary>
    /// The timestamp when the message was published.
    /// </summary>
    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Message attributes.
    /// </summary>
    [JsonPropertyName("MessageAttributes")]
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; set; }
}

/// <summary>
/// Represents an SNS message attribute.
/// </summary>
public class SnsMessageAttribute
{
    /// <summary>
    /// The attribute type (String, Number, Binary).
    /// </summary>
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    /// <summary>
    /// The attribute value.
    /// </summary>
    [JsonPropertyName("Value")]
    public string? Value { get; set; }
}

/// <summary>
/// Represents a message to be published to SNS.
/// </summary>
public class SnsOutputMessage
{
    /// <summary>
    /// The message content to publish.
    /// </summary>
    public object? Message { get; set; }

    /// <summary>
    /// The message subject (optional).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The topic ARN to publish to. If not specified, uses the attribute's TopicArn.
    /// </summary>
    public string? TopicArn { get; set; }

    /// <summary>
    /// The message group ID for FIFO topics.
    /// </summary>
    public string? MessageGroupId { get; set; }

    /// <summary>
    /// The message deduplication ID for FIFO topics.
    /// </summary>
    public string? MessageDeduplicationId { get; set; }

    /// <summary>
    /// Message attributes to include.
    /// </summary>
    public Dictionary<string, SnsMessageAttributeValue>? MessageAttributes { get; set; }
}

/// <summary>
/// Represents a message attribute value for output.
/// </summary>
public class SnsMessageAttributeValue
{
    /// <summary>
    /// The data type (String, Number, Binary, String.Array).
    /// </summary>
    public string DataType { get; set; } = "String";

    /// <summary>
    /// The string value.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// The binary value.
    /// </summary>
    public byte[]? BinaryValue { get; set; }
}
