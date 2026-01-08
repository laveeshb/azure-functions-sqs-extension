// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;

/// <summary>
/// Extension configuration provider for Amazon S3 bindings.
/// </summary>
[Extension(name: "s3", configurationSection: "s3")]
public class S3ExtensionProvider : IExtensionConfigProvider
{
    /// <summary>
    /// Initializes the S3 extension with the Azure Functions host.
    /// </summary>
    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Register input binding
        var inputRule = context.AddBindingRule<S3Attribute>();
        inputRule.BindToValueProvider((attribute, type) => 
            Task.FromResult<IValueBinder>(new S3ObjectValueProvider(attribute, type)));

        // Register output binding
        var outputRule = context.AddBindingRule<S3OutAttribute>();
        outputRule.BindToCollector(attribute => new S3AsyncCollector(attribute));

        // Add output converters
        outputRule.AddConverter<S3Message, PutObjectRequest>(ConvertMessageToRequest);
        outputRule.AddConverter<string, PutObjectRequest>(ConvertStringToRequest);
        outputRule.AddConverter<byte[], PutObjectRequest>(ConvertBytesToRequest);
    }

    private static PutObjectRequest ConvertMessageToRequest(S3Message message)
    {
        var request = new PutObjectRequest
        {
            BucketName = message.BucketName,
            Key = message.Key,
            ContentType = message.ContentType
        };

        // Set content from available source
        if (message.ContentStream != null)
        {
            request.InputStream = message.ContentStream;
        }
        else if (message.ContentBytes != null)
        {
            request.InputStream = new MemoryStream(message.ContentBytes);
        }
        else if (!string.IsNullOrEmpty(message.Content))
        {
            request.InputStream = new MemoryStream(Encoding.UTF8.GetBytes(message.Content));
        }

        // Set metadata
        if (message.Metadata != null)
        {
            foreach (var kvp in message.Metadata)
            {
                request.Metadata[kvp.Key] = kvp.Value;
            }
        }

        // Set storage class if specified
        if (!string.IsNullOrEmpty(message.StorageClass))
        {
            request.StorageClass = new S3StorageClass(message.StorageClass);
        }

        // Set server-side encryption if specified
        if (!string.IsNullOrEmpty(message.ServerSideEncryption))
        {
            request.ServerSideEncryptionMethod = new ServerSideEncryptionMethod(message.ServerSideEncryption);
        }

        return request;
    }

    private static PutObjectRequest ConvertStringToRequest(string content)
    {
        return new PutObjectRequest
        {
            InputStream = new MemoryStream(Encoding.UTF8.GetBytes(content)),
            ContentType = "text/plain"
        };
    }

    private static PutObjectRequest ConvertBytesToRequest(byte[] content)
    {
        return new PutObjectRequest
        {
            InputStream = new MemoryStream(content),
            ContentType = "application/octet-stream"
        };
    }
}
