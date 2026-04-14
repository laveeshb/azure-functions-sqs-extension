// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

/// <summary>
/// Represents a message to be sent to Amazon EventBridge.
/// </summary>
public class EventBridgeMessage
{
    /// <summary>
    /// Gets or sets the source of the event. Identifies the service that generated the event.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the detail type of the event. Identifies the type of event.
    /// </summary>
    public string? DetailType { get; set; }

    /// <summary>
    /// Gets or sets the event detail as a JSON string.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets the name or ARN of the event bus to send the event to.
    /// If not specified, uses the EventBusName from the attribute.
    /// </summary>
    public string? EventBusName { get; set; }

    /// <summary>
    /// Gets or sets AWS resources that the event primarily concerns.
    /// </summary>
    public List<string>? Resources { get; set; }

    /// <summary>
    /// Gets or sets the trace header for AWS X-Ray tracing.
    /// </summary>
    public string? TraceHeader { get; set; }
}
