
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs.Host.Bindings;

public class SqsQueueMessageValueProvider : IValueProvider
{
    private readonly object _value;

    public Type Type => typeof(Message);

    public SqsQueueMessageValueProvider(object value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Task<object> GetValueAsync()
    {
        return Task.FromResult(_value);
    }

    public string ToInvokeString()
    {
        return _value.ToString() ?? string.Empty;
    }
}
