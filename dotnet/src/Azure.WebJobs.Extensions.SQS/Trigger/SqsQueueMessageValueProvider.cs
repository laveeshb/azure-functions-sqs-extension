
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;

public class SqsQueueMessageValueProvider : IValueProvider
{
    private const string IsolatedWorkerRuntime = "dotnet-isolated";
    private const string BindingDataSource = "AWSSQS";
    private const string BindingDataVersion = "1.0";
    private const string JsonContentType = "application/json";

    private static readonly bool IsIsolatedWorker =
        string.Equals(
            Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"),
            IsolatedWorkerRuntime,
            StringComparison.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object _value;

    public Type Type => IsIsolatedWorker ? typeof(ParameterBindingData) : typeof(Message);

    public SqsQueueMessageValueProvider(object value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Task<object> GetValueAsync()
    {
        if (IsIsolatedWorker && _value is Message message)
        {
            var payload = SerializeMessage(message);
            var bindingData = new ParameterBindingData(
                version: BindingDataVersion,
                source: BindingDataSource,
                content: BinaryData.FromString(payload),
                contentType: JsonContentType);
            return Task.FromResult<object>(bindingData);
        }

        return Task.FromResult(_value);
    }

    public string ToInvokeString()
    {
        return _value.ToString() ?? string.Empty;
    }

    private static string SerializeMessage(Message message)
    {
        var data = new SqsMessageData
        {
            MessageId = message.MessageId,
            ReceiptHandle = message.ReceiptHandle,
            Body = message.Body,
            MD5OfBody = message.MD5OfBody,
            Attributes = message.Attributes,
            MD5OfMessageAttributes = message.MD5OfMessageAttributes
        };

        if (message.MessageAttributes is { Count: > 0 })
        {
            data.MessageAttributes = new();
            foreach (var kvp in message.MessageAttributes)
            {
                data.MessageAttributes[kvp.Key] = new SqsMessageAttributeData
                {
                    DataType = kvp.Value.DataType,
                    StringValue = kvp.Value.StringValue,
                    BinaryValue = kvp.Value.BinaryValue?.ToArray()
                };
            }
        }

        return JsonSerializer.Serialize(data, SerializerOptions);
    }

    private sealed class SqsMessageData
    {
        public string? MessageId { get; set; }
        public string? ReceiptHandle { get; set; }
        public string? Body { get; set; }
        public string? MD5OfBody { get; set; }
        public System.Collections.Generic.Dictionary<string, string>? Attributes { get; set; }
        public System.Collections.Generic.Dictionary<string, SqsMessageAttributeData>? MessageAttributes { get; set; }
        public string? MD5OfMessageAttributes { get; set; }
    }

    private sealed class SqsMessageAttributeData
    {
        public string? DataType { get; set; }
        public string? StringValue { get; set; }
        public byte[]? BinaryValue { get; set; }
    }
}
