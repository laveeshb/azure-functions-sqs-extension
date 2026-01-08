
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs.Host.Bindings;

public class SqsQueueMessageValueProvider : IValueProvider
{
    private readonly object _value;
    private readonly Type _targetType;

    public Type Type => _targetType;

    public SqsQueueMessageValueProvider(object value, Type targetType)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _targetType = targetType ?? typeof(Message);
    }

    public SqsQueueMessageValueProvider(object value) : this(value, typeof(Message))
    {
    }

    public Task<object> GetValueAsync()
    {
        // If target type is Message, return as-is
        if (_targetType == typeof(Message) || _targetType == typeof(object))
        {
            return Task.FromResult(_value);
        }

        // Get the message body
        string? messageBody = _value switch
        {
            Message msg => msg.Body,
            string str => str,
            _ => _value.ToString()
        };

        if (string.IsNullOrEmpty(messageBody))
        {
            throw new InvalidOperationException("Message body is null or empty");
        }

        // If target type is string, return the body
        if (_targetType == typeof(string))
        {
            return Task.FromResult<object>(messageBody);
        }

        // Try to deserialize to the target type
        try
        {
            var result = JsonSerializer.Deserialize(messageBody, _targetType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize message to {_targetType.Name}");
            }

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize SQS message to {_targetType.Name}: {ex.Message}", ex);
        }
    }

    public string ToInvokeString()
    {
        return _value switch
        {
            Message msg => msg.Body ?? string.Empty,
            _ => _value.ToString() ?? string.Empty
        };
    }
}
