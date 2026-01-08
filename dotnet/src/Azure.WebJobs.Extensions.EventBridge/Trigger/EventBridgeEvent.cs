// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an EventBridge event received via API Destination webhook.
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
    /// The time the event occurred.
    /// </summary>
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// The type of detail in the event (e.g., "EC2 Instance State-change Notification").
    /// </summary>
    [JsonPropertyName("detail-type")]
    public string? DetailType { get; set; }

    /// <summary>
    /// The event detail as a raw JSON object.
    /// Use GetDetail&lt;T&gt;() to deserialize to a specific type.
    /// </summary>
    [JsonPropertyName("detail")]
    public System.Text.Json.JsonElement? Detail { get; set; }

    /// <summary>
    /// Resources involved in the event.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<string>? Resources { get; set; }

    /// <summary>
    /// Deserializes the detail property to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized detail, or default if detail is null.</returns>
    public T? GetDetail<T>()
    {
        if (Detail == null)
        {
            return default;
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(Detail.Value.GetRawText());
    }

    /// <summary>
    /// Gets the detail as a raw JSON string.
    /// </summary>
    /// <returns>The detail as a JSON string, or null if detail is null.</returns>
    public string? GetDetailRaw()
    {
        return Detail?.GetRawText();
    }
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
    /// The time the event occurred.
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
