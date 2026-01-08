namespace Azure.Functions.Worker.Extensions.SNS;

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Converter to bind SNS message data to strongly typed parameters in isolated worker functions.
/// </summary>
[SupportsDeferredBinding]
[SupportedTargetType(typeof(SnsNotification))]
[SupportedTargetType(typeof(SnsNotification<>))]
[SupportedTargetType(typeof(string))]
internal sealed class SnsMessageConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        try
        {
            if (context.Source is string json && !string.IsNullOrEmpty(json))
            {
                // Parse SQS message first, then extract SNS notification from body
                var sqsMessage = JsonSerializer.Deserialize<SqsMessageWrapper>(json);
                var snsJson = sqsMessage?.Body ?? json;

                if (context.TargetType == typeof(SnsNotification))
                {
                    var notification = JsonSerializer.Deserialize<SnsNotification>(snsJson);
                    if (notification == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize SNS notification.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(notification));
                }

                if (context.TargetType == typeof(string))
                {
                    var notification = JsonSerializer.Deserialize<SnsNotification>(snsJson);
                    return new ValueTask<ConversionResult>(ConversionResult.Success(notification?.Message ?? snsJson));
                }

                // Handle generic SnsNotification<T>
                if (context.TargetType.IsGenericType && 
                    context.TargetType.GetGenericTypeDefinition() == typeof(SnsNotification<>))
                {
                    var notification = JsonSerializer.Deserialize(snsJson, context.TargetType);
                    if (notification == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize SNS notification.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(notification));
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
