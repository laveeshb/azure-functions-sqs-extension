namespace Azure.Functions.Worker.Extensions.EventBridge;

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon EventBridge events
/// via API Destination HTTP webhook.
/// Compatible with Azure Functions isolated worker model.
/// </summary>
/// <remarks>
/// EventBridge sends events to this function via HTTP POST using API Destinations.
/// To configure:
/// 1. Create an EventBridge Connection with authentication (API Key recommended)
/// 2. Create an API Destination pointing to your function's webhook URL
/// 3. Create a rule that routes events to the API Destination
/// 
/// The webhook endpoint will be: https://{app}.azurewebsites.net/runtime/webhooks/eventbridge/{route}
/// </remarks>
[InputConverter(typeof(EventBridgeMessageConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class EventBridgeTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBridgeTriggerAttribute"/> class.
    /// </summary>
    public EventBridgeTriggerAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBridgeTriggerAttribute"/> class with a route.
    /// </summary>
    /// <param name="route">The HTTP route for the webhook endpoint.</param>
    public EventBridgeTriggerAttribute(string route)
    {
        Route = route;
    }

    /// <summary>
    /// Gets or sets the HTTP route for the webhook endpoint.
    /// If not specified, defaults to the function name.
    /// The full URL will be: /runtime/webhooks/eventbridge/{route}
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets the expected event source filter (e.g., "aws.ec2", "custom.myapp").
    /// If specified, events from other sources will be rejected.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the expected detail type filter.
    /// If specified, events with different detail types will be rejected.
    /// </summary>
    public string? DetailType { get; set; }

    /// <summary>
    /// Gets or sets whether to validate the event structure.
    /// Default is true.
    /// </summary>
    public bool ValidateEvent { get; set; } = true;

    /// <summary>
    /// Gets or sets the expected API key header name for authentication.
    /// EventBridge API Destinations typically send the key in a custom header.
    /// Default is "x-api-key".
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "x-api-key";

    /// <summary>
    /// Gets or sets the name of the app setting containing the expected API key value.
    /// If specified, requests without a matching API key will be rejected.
    /// </summary>
    public string? ApiKeySettingName { get; set; }
}
