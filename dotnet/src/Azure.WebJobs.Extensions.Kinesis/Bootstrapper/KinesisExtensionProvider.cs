// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using System.IO;
using System.Text;
using Amazon.Kinesis.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension configuration provider for Amazon Kinesis bindings.
/// </summary>
[Extension(name: "kinesis", configurationSection: "kinesis")]
public class KinesisExtensionProvider : IExtensionConfigProvider
{
    private readonly KinesisTriggerBindingProvider _triggerBindingProvider;
    private readonly ILogger _logger;

    public KinesisExtensionProvider(
        KinesisTriggerBindingProvider triggerBindingProvider,
        ILoggerFactory loggerFactory)
    {
        _triggerBindingProvider = triggerBindingProvider ?? throw new ArgumentNullException(nameof(triggerBindingProvider));
        _logger = loggerFactory?.CreateLogger<KinesisExtensionProvider>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Initializes the Kinesis extension with the Azure Functions host.
    /// </summary>
    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Initializing Kinesis extension");

        // Register output binding
        var outputRule = context.AddBindingRule<KinesisOutAttribute>();
        outputRule.BindToCollector(attribute => new KinesisAsyncCollector(attribute));

        // Add converters for output
        outputRule.AddConverter<KinesisMessage, PutRecordRequest>(ConvertMessageToRequest);
        outputRule.AddConverter<string, PutRecordRequest>(ConvertStringToRequest);
        outputRule.AddConverter<byte[], PutRecordRequest>(ConvertBytesToRequest);

        // Register trigger binding
        var triggerRule = context.AddBindingRule<KinesisTriggerAttribute>();
        triggerRule.BindToTrigger(_triggerBindingProvider);

        _logger.LogInformation("Kinesis extension initialized with output binding and trigger");
    }

    private static PutRecordRequest ConvertMessageToRequest(KinesisMessage message)
    {
        MemoryStream dataStream;

        if (message.DataBytes != null)
        {
            dataStream = new MemoryStream(message.DataBytes);
        }
        else if (!string.IsNullOrEmpty(message.Data))
        {
            dataStream = new MemoryStream(Encoding.UTF8.GetBytes(message.Data));
        }
        else
        {
            throw new InvalidOperationException("Kinesis message must have Data or DataBytes set.");
        }

        return new PutRecordRequest
        {
            StreamName = message.StreamName,
            PartitionKey = message.PartitionKey,
            Data = dataStream,
            ExplicitHashKey = message.ExplicitHashKey,
            SequenceNumberForOrdering = message.SequenceNumberForOrdering
        };
    }

    private static PutRecordRequest ConvertStringToRequest(string data)
    {
        return new PutRecordRequest
        {
            Data = new MemoryStream(Encoding.UTF8.GetBytes(data))
        };
    }

    private static PutRecordRequest ConvertBytesToRequest(byte[] data)
    {
        return new PutRecordRequest
        {
            Data = new MemoryStream(data)
        };
    }
}
