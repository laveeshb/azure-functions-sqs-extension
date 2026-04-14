
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;

public class SqsQueueMessageValueProvider : IValueProvider
{
    internal const string BindingDataSource = "AWSSQS";
    internal const string BindingDataVersion = "1.0";
    internal const string JsonContentType = "application/json";
    private const string IsolatedWorkerRuntime = "dotnet-isolated";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object _value;
    private readonly bool _isIsolatedWorker;

    public Type Type => _isIsolatedWorker ? typeof(ParameterBindingData) : typeof(Message);

    public SqsQueueMessageValueProvider(object value)
        : this(value, IsRunningInIsolatedWorker())
    {
    }

    internal SqsQueueMessageValueProvider(object value, bool isIsolatedWorker)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _isIsolatedWorker = isIsolatedWorker;
    }

    public Task<object> GetValueAsync()
    {
        if (_isIsolatedWorker && _value is Message message)
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

    private static bool IsRunningInIsolatedWorker()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"),
            IsolatedWorkerRuntime,
            StringComparison.OrdinalIgnoreCase);
    }

    internal static string SerializeMessage(Message message)
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
            data.MessageAttributes = new Dictionary<string, SqsMessageAttributeData>();
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

    internal sealed class SqsMessageData
    {
        public string? MessageId { get; set; }
        public string? ReceiptHandle { get; set; }
        public string? Body { get; set; }
        public string? MD5OfBody { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
        public Dictionary<string, SqsMessageAttributeData>? MessageAttributes { get; set; }
        public string? MD5OfMessageAttributes { get; set; }
    }

    internal sealed class SqsMessageAttributeData
    {
        public string? DataType { get; set; }
        public string? StringValue { get; set; }
        public byte[]? BinaryValue { get; set; }
    }
}
