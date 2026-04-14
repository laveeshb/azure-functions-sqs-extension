namespace Azure.Functions.Worker.Extensions.S3;

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Converter to bind S3 event data to strongly typed parameters in isolated worker functions.
/// </summary>
[SupportsDeferredBinding]
[SupportedTargetType(typeof(S3Event))]
[SupportedTargetType(typeof(S3EventRecord))]
[SupportedTargetType(typeof(string))]
internal sealed class S3EventConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        try
        {
            if (context.Source is string json && !string.IsNullOrEmpty(json))
            {
                // Parse SQS message first, then extract S3 event from body
                var sqsMessage = JsonSerializer.Deserialize<SqsMessageWrapper>(json);
                var s3Json = sqsMessage?.Body ?? json;

                if (context.TargetType == typeof(S3Event))
                {
                    var s3Event = JsonSerializer.Deserialize<S3Event>(s3Json);
                    if (s3Event == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize S3 event.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(s3Event));
                }

                if (context.TargetType == typeof(S3EventRecord))
                {
                    var s3Event = JsonSerializer.Deserialize<S3Event>(s3Json);
                    var record = s3Event?.Records?.FirstOrDefault();
                    if (record == null)
                    {
                        throw new InvalidOperationException("No S3 event records found.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(record));
                }

                if (context.TargetType == typeof(string))
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(s3Json));
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
