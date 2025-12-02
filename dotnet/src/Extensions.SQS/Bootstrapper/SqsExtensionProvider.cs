
namespace Azure.Functions.Extensions.SQS;

using System;
using System.Text;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Options;

[Extension(name: "sqsQueue", configurationSection: "sqsQueue")]
public class SqsExtensionProvider : IExtensionConfigProvider
{
    private readonly IOptions<SqsQueueOptions> _sqsQueueOptions;
    private readonly INameResolver _nameResolver;

    public SqsExtensionProvider(IOptions<SqsQueueOptions> sqsQueueOptions, INameResolver nameResolver)
    {
        _sqsQueueOptions = sqsQueueOptions ?? throw new ArgumentNullException(nameof(sqsQueueOptions));
        _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
    }

    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var queueTriggerRule = context.AddBindingRule<SqsQueueTriggerAttribute>();
        queueTriggerRule.BindToTrigger(new SqsQueueTriggerBindingProvider(_sqsQueueOptions, _nameResolver));

        var queueCollectorRule = context.AddBindingRule<SqsQueueOutAttribute>();
        queueCollectorRule.BindToCollector(attribute => new SqsQueueAsyncCollector(attribute));
        queueCollectorRule.AddConverter<SqsQueueMessage, SendMessageRequest>(ConvertSqsQueueMessageToSendMessageRequest);
        queueCollectorRule.AddConverter<string, SendMessageRequest>(ConvertStringToSendMessageRequest);
        queueCollectorRule.AddConverter<byte[], SendMessageRequest>(ConvertByteArrayToSendMessageRequest);
    }

    private static SendMessageRequest ConvertByteArrayToSendMessageRequest(byte[] body) 
    {
        var utfString = Encoding.UTF8.GetString(body, 0, body.Length);
        return ConvertStringToSendMessageRequest(utfString);
    }

    private static SendMessageRequest ConvertStringToSendMessageRequest(string body)
    {
        return new SendMessageRequest
        {
            MessageBody = body,
        };
    }

    private static SendMessageRequest ConvertSqsQueueMessageToSendMessageRequest(SqsQueueMessage sqsQueueMessage)
    {
        return new SendMessageRequest
        {
            QueueUrl = sqsQueueMessage.QueueUrl,
            MessageBody = sqsQueueMessage.Body,
        };
    }
}
