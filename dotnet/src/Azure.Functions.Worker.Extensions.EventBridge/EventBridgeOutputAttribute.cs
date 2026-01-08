namespace Azure.Functions.Worker.Extensions.EventBridge;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to configure an Amazon EventBridge output binding.
/// Compatible with Azure Functions isolated worker model.
/// Use on return type properties or method to send events to EventBridge.
/// </summary>
public sealed class EventBridgeOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBridgeOutputAttribute"/> class.
    /// </summary>
    /// <param name="eventBusName">The name or ARN of the EventBridge event bus.</param>
    public EventBridgeOutputAttribute(string eventBusName)
    {
        EventBusName = eventBusName ?? throw new ArgumentNullException(nameof(eventBusName));
    }

    /// <summary>
    /// Gets the name or ARN of the EventBridge event bus.
    /// </summary>
    public string EventBusName { get; }

    /// <summary>
    /// Gets or sets the AWS Access Key ID. If not specified, uses AWS credential chain.
    /// </summary>
    public string? AWSKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS Secret Access Key. If not specified, uses AWS credential chain.
    /// </summary>
    public string? AWSAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the AWS Region (e.g., "us-east-1"). If not specified, uses AWS credential chain.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the default source for events. Can be overridden per event.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the default detail type for events. Can be overridden per event.
    /// </summary>
    public string? DetailType { get; set; }
}
