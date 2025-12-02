namespace Azure.Functions.Worker.Extensions.SQS;

using System.Text.Json;
using Amazon.SQS.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Converter to bind SQS message data to strongly typed parameters in isolated worker functions.
/// Converts ModelBindingData from the Azure Functions host to SQS message types.
/// </summary>
[SupportsDeferredBinding]
[SupportedTargetType(typeof(Message))]
[SupportedTargetType(typeof(string))]
internal sealed class SqsMessageConverter : IInputConverter
{
    private const string ExpectedContentType = "application/json";

    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        try
        {
            // For now, handle string sources from trigger data
            // The host will serialize SQS messages as JSON strings
            if (context.Source is string json && !string.IsNullOrEmpty(json))
            {
                if (context.TargetType == typeof(Message))
                {
                    var message = JsonSerializer.Deserialize<SqsMessageData>(json);
                    if (message == null)
                    {
                        throw new InvalidOperationException("Failed to deserialize SQS message data.");
                    }

                    var result = ConvertToSqsMessage(message);
                    return new ValueTask<ConversionResult>(ConversionResult.Success(result));
                }

                if (context.TargetType == typeof(string))
                {
                    var message = JsonSerializer.Deserialize<SqsMessageData>(json);
                    return new ValueTask<ConversionResult>(ConversionResult.Success(message?.Body ?? json));
                }
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
        catch (Exception ex)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Failed(ex));
        }
    }

    private static Message ConvertToSqsMessage(SqsMessageData content)
    {
        var message = new Message
        {
            MessageId = content.MessageId,
            ReceiptHandle = content.ReceiptHandle,
            Body = content.Body,
            MD5OfBody = content.MD5OfBody,
            Attributes = content.Attributes ?? new Dictionary<string, string>(),
            MessageAttributes = ConvertMessageAttributes(content.MessageAttributes),
            MD5OfMessageAttributes = content.MD5OfMessageAttributes
        };

        return message;
    }

    private static Dictionary<string, MessageAttributeValue> ConvertMessageAttributes(
        Dictionary<string, SqsMessageAttributeData>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return new Dictionary<string, MessageAttributeValue>();
        }

        var result = new Dictionary<string, MessageAttributeValue>();
        foreach (var kvp in attributes)
        {
            result[kvp.Key] = new MessageAttributeValue
            {
                DataType = kvp.Value.DataType,
                StringValue = kvp.Value.StringValue,
                BinaryValue = kvp.Value.BinaryValue != null 
                    ? new MemoryStream(kvp.Value.BinaryValue) 
                    : null
            };
        }
        return result;
    }

    /// <summary>
    /// Data structure matching the JSON format sent by the Functions host for SQS messages.
    /// </summary>
    private sealed class SqsMessageData
    {
        public string? MessageId { get; set; }
        public string? ReceiptHandle { get; set; }
        public string? Body { get; set; }
        public string? MD5OfBody { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
        public Dictionary<string, SqsMessageAttributeData>? MessageAttributes { get; set; }
        public string? MD5OfMessageAttributes { get; set; }
    }

    private sealed class SqsMessageAttributeData
    {
        public string? DataType { get; set; }
        public string? StringValue { get; set; }
        public byte[]? BinaryValue { get; set; }
    }
}
