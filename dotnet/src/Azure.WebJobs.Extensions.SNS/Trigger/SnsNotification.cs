// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an SNS notification received via HTTP webhook.
/// </summary>
public class SnsNotification
{
    /// <summary>
    /// The type of notification: "Notification", "SubscriptionConfirmation", or "UnsubscribeConfirmation".
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
    /// The subject of the message (optional, only for Notification type).
    /// </summary>
    [JsonPropertyName("Subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// The message content.
    /// </summary>
    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    /// <summary>
    /// The timestamp when the message was published (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("Timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// The signature version ("1" for SHA1, "2" for SHA256).
    /// </summary>
    [JsonPropertyName("SignatureVersion")]
    public string? SignatureVersion { get; set; }

    /// <summary>
    /// The Base64-encoded signature for message verification.
    /// </summary>
    [JsonPropertyName("Signature")]
    public string? Signature { get; set; }

    /// <summary>
    /// URL to the X.509 certificate used to sign the message.
    /// </summary>
    [JsonPropertyName("SigningCertURL")]
    public string? SigningCertUrl { get; set; }

    /// <summary>
    /// URL to unsubscribe from the topic (only for Notification type).
    /// </summary>
    [JsonPropertyName("UnsubscribeURL")]
    public string? UnsubscribeUrl { get; set; }

    /// <summary>
    /// URL to confirm the subscription (only for SubscriptionConfirmation type).
    /// </summary>
    [JsonPropertyName("SubscribeURL")]
    public string? SubscribeUrl { get; set; }

    /// <summary>
    /// Token for subscription confirmation (only for SubscriptionConfirmation type).
    /// </summary>
    [JsonPropertyName("Token")]
    public string? Token { get; set; }

    /// <summary>
    /// Message attributes.
    /// </summary>
    [JsonPropertyName("MessageAttributes")]
    public Dictionary<string, SnsMessageAttributeValue>? MessageAttributes { get; set; }
}

/// <summary>
/// Represents an SNS message attribute value.
/// </summary>
public class SnsMessageAttributeValue
{
    /// <summary>
    /// The data type of the attribute value: "String", "String.Array", "Number", or "Binary".
    /// </summary>
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    /// <summary>
    /// The attribute value.
    /// </summary>
    [JsonPropertyName("Value")]
    public string? Value { get; set; }
}
