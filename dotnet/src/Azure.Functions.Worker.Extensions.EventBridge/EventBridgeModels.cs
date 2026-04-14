namespace Azure.Functions.Worker.Extensions.EventBridge;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an EventBridge event received from AWS.
/// </summary>
public class EventBridgeEvent
{
    /// <summary>
    /// The version of the event format (typically "0").
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// A unique identifier for the event.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The source of the event (e.g., "aws.ec2", "custom.myapp").
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// The AWS account ID where the event originated.
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// The AWS region where the event originated.
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// The time the event occurred in ISO 8601 format.
    /// </summary>
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// The type of detail in the event (e.g., "EC2 Instance State-change Notification").
    /// </summary>
    [JsonPropertyName("detail-type")]
    public string? DetailType { get; set; }

    /// <summary>
    /// The event detail as a JSON string.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// Resources involved in the event.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<string>? Resources { get; set; }
}

/// <summary>
/// Represents an EventBridge event with a strongly-typed detail payload.
/// </summary>
/// <typeparam name="TDetail">The type of the event detail.</typeparam>
public class EventBridgeEvent<TDetail>
{
    /// <summary>
    /// The version of the event format (typically "0").
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// A unique identifier for the event.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The source of the event (e.g., "aws.ec2", "custom.myapp").
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// The AWS account ID where the event originated.
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// The AWS region where the event originated.
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// The time the event occurred in ISO 8601 format.
    /// </summary>
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// The type of detail in the event.
    /// </summary>
    [JsonPropertyName("detail-type")]
    public string? DetailType { get; set; }

    /// <summary>
    /// The strongly-typed event detail.
    /// </summary>
    [JsonPropertyName("detail")]
    public TDetail? Detail { get; set; }

    /// <summary>
    /// Resources involved in the event.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<string>? Resources { get; set; }
}

/// <summary>
/// Represents an event to be sent to EventBridge.
/// </summary>
public class EventBridgeOutputEvent
{
    /// <summary>
    /// The source of the event.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The detail type of the event.
    /// </summary>
    public string? DetailType { get; set; }

    /// <summary>
    /// The event detail (will be serialized to JSON if not already a string).
    /// </summary>
    public object? Detail { get; set; }

    /// <summary>
    /// The event bus name or ARN. If not specified, uses the attribute's EventBusName.
    /// </summary>
    public string? EventBusName { get; set; }

    /// <summary>
    /// Resources associated with the event.
    /// </summary>
    public List<string>? Resources { get; set; }

    /// <summary>
    /// Optional trace header for X-Ray tracing.
    /// </summary>
    public string? TraceHeader { get; set; }
}
