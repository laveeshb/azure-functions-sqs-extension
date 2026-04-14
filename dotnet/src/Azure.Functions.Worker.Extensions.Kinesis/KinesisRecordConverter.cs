namespace Azure.Functions.Worker.Extensions.Kinesis;

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Converter to bind Kinesis record data to strongly typed parameters in isolated worker functions.
/// </summary>
[SupportsDeferredBinding]
[SupportedTargetType(typeof(KinesisRecord))]
[SupportedTargetType(typeof(KinesisRecord<>))]
[SupportedTargetType(typeof(string))]
[SupportedTargetType(typeof(byte[]))]
internal sealed class KinesisRecordConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        try
        {
            if (context.Source is string json && !string.IsNullOrEmpty(json))
            {
                if (context.TargetType == typeof(KinesisRecord))
                {
                    var record = JsonSerializer.Deserialize<KinesisRecord>(json);
                    if (record == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize Kinesis record.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(record));
                }

                if (context.TargetType == typeof(string))
                {
                    var record = JsonSerializer.Deserialize<KinesisRecord>(json);
                    return new ValueTask<ConversionResult>(ConversionResult.Success(record?.Data ?? json));
                }

                if (context.TargetType == typeof(byte[]))
                {
                    var record = JsonSerializer.Deserialize<KinesisRecord>(json);
                    var data = record?.Data ?? string.Empty;
                    var bytes = Convert.FromBase64String(data);
                    return new ValueTask<ConversionResult>(ConversionResult.Success(bytes));
                }

                // Handle generic KinesisRecord<T>
                if (context.TargetType.IsGenericType && 
                    context.TargetType.GetGenericTypeDefinition() == typeof(KinesisRecord<>))
                {
                    var record = JsonSerializer.Deserialize(json, context.TargetType);
                    if (record == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize Kinesis record.");
                    }
                    return new ValueTask<ConversionResult>(ConversionResult.Success(record));
                }
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
        catch (Exception ex)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
        }
    }
}
