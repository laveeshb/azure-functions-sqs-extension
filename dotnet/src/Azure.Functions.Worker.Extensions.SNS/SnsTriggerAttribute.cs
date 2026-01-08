namespace Azure.Functions.Worker.Extensions.SNS;

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon SNS HTTP webhook notifications.
/// SNS sends messages directly to the function endpoint via HTTP POST.
/// Compatible with Azure Functions isolated worker model.
/// </summary>
/// <remarks>
/// The trigger exposes an HTTP endpoint that SNS can call. To use:
/// 1. Deploy the function with this trigger
/// 2. Subscribe the function's endpoint URL to your SNS topic (HTTPS protocol)
/// 3. The trigger will automatically handle subscription confirmation
/// 4. Messages are verified using SNS signature validation for security
/// </remarks>
[InputConverter(typeof(SnsMessageConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class SnsTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnsTriggerAttribute"/> class.
    /// </summary>
    public SnsTriggerAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsTriggerAttribute"/> class with a topic ARN filter.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to accept messages from.</param>
    public SnsTriggerAttribute(string topicArn)
    {
        TopicArn = topicArn;
    }

    /// <summary>
    /// Gets or sets the ARN of the SNS topic to accept messages from. 
    /// If specified, messages from other topics will be rejected.
    /// Supports %AppSetting% syntax for configuration binding.
    /// </summary>
    public string? TopicArn { get; set; }

    /// <summary>
    /// Gets or sets the HTTP route for the webhook endpoint.
    /// If not specified, defaults to the function name.
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets whether to verify SNS message signatures. Default is true.
    /// Signature verification confirms messages are authentically from AWS SNS.
    /// Disable only for testing with mock SNS services that don't sign messages.
    /// </summary>
    public bool VerifySignature { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically confirm subscription requests. Default is true.
    /// When enabled, the trigger automatically handles SNS SubscriptionConfirmation messages.
    /// </summary>
    public bool AutoConfirmSubscription { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional subject filter pattern.
    /// Only messages with matching subjects will trigger the function.
    /// </summary>
    public string? SubjectFilter { get; set; }
}
