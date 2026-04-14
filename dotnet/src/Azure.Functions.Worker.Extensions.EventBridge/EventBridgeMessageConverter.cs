namespace Azure.Functions.Worker.Extensions.EventBridge;

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Converter to bind EventBridge event data to strongly typed parameters in isolated worker functions.
/// Converts ModelBindingData from the Azure Functions host to EventBridge event types.
/// </summary>
[SupportsDeferredBinding]
[SupportedTargetType(typeof(EventBridgeEvent))]
[SupportedTargetType(typeof(EventBridgeEvent<>))]
[SupportedTargetType(typeof(string))]
internal sealed class EventBridgeMessageConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        try
        {
            if (context.Source is string json && !string.IsNullOrEmpty(json))
            {
                // Try to parse as SQS message containing EventBridge event
                var sqsMessage = JsonSerializer.Deserialize<SqsMessageWrapper>(json);
                var eventJson = sqsMessage?.Body ?? json;

                if (context.TargetType == typeof(EventBridgeEvent))
                {
                    var eventBridgeEvent = JsonSerializer.Deserialize<EventBridgeEvent>(eventJson);
                    if (eventBridgeEvent == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize EventBridge event.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(eventBridgeEvent));
                }

                if (context.TargetType == typeof(string))
                {
                    var eventBridgeEvent = JsonSerializer.Deserialize<EventBridgeEvent>(eventJson);
                    return new ValueTask<ConversionResult>(ConversionResult.Success(eventBridgeEvent?.Detail ?? eventJson));
                }

                // Handle generic EventBridgeEvent<T>
                if (context.TargetType.IsGenericType && 
                    context.TargetType.GetGenericTypeDefinition() == typeof(EventBridgeEvent<>))
                {
                    var eventBridgeEvent = JsonSerializer.Deserialize(eventJson, context.TargetType);
                    if (eventBridgeEvent == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize EventBridge event.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(eventBridgeEvent));
                }
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
        catch (Exception ex)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
        }
    }

    private sealed class SqsMessageWrapper
    {
        public string? Body { get; set; }
    }
}
