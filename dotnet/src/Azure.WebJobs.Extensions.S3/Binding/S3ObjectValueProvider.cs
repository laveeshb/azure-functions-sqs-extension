// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Azure.WebJobs.Host.Bindings;

/// <summary>
/// Value provider that fetches an S3 object and provides it to the function.
/// </summary>
internal sealed class S3ObjectValueProvider : IValueBinder, IDisposable
{
    private readonly S3Attribute _attribute;
    private readonly AmazonS3Client _client;
    private readonly Type _targetType;
    private GetObjectResponse? _response;
    private bool _disposed;

    public S3ObjectValueProvider(S3Attribute attribute, Type targetType)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        _client = AmazonS3ClientFactory.Build(attribute);
    }

    public Type Type => _targetType;

    public async Task<object?> GetValueAsync()
    {
        if (string.IsNullOrEmpty(_attribute.BucketName))
            throw new InvalidOperationException("BucketName is required for S3 input binding.");

        if (string.IsNullOrEmpty(_attribute.Key))
            throw new InvalidOperationException("Key is required for S3 input binding.");

        var request = new GetObjectRequest
        {
            BucketName = _attribute.BucketName,
            Key = _attribute.Key
        };

        if (!string.IsNullOrEmpty(_attribute.VersionId))
        {
            request.VersionId = _attribute.VersionId;
        }

        try
        {
            _response = await _client.GetObjectAsync(request);
            return await ConvertToTargetTypeAsync(_response);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Return null for missing objects
            return null;
        }
    }

    public Task SetValueAsync(object? value, System.Threading.CancellationToken cancellationToken)
    {
        // Input bindings don't set values back
        return Task.CompletedTask;
    }

    private async Task<object?> ConvertToTargetTypeAsync(GetObjectResponse response)
    {
        // Support various target types
        if (_targetType == typeof(GetObjectResponse))
        {
            return response;
        }

        if (_targetType == typeof(Stream))
        {
            // Return the response stream directly
            // Caller is responsible for disposing
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        if (_targetType == typeof(byte[]))
        {
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        if (_targetType == typeof(string))
        {
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync();
        }

        if (_targetType == typeof(S3Object))
        {
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            
            return new S3Object
            {
                BucketName = response.BucketName,
                Key = response.Key,
                Content = memoryStream.ToArray(),
                ContentType = response.Headers.ContentType,
                ContentLength = response.ContentLength,
                ETag = response.ETag,
                LastModified = response.LastModified,
                VersionId = response.VersionId,
                Metadata = response.Metadata.Keys
                    .ToDictionary(k => k, k => response.Metadata[k])
            };
        }

        throw new InvalidOperationException(
            $"Cannot convert S3 object to type {_targetType.FullName}. " +
            "Supported types: GetObjectResponse, Stream, byte[], string, S3Object.");
    }

    public string ToInvokeString()
    {
        return $"s3://{_attribute.BucketName}/{_attribute.Key}";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _response?.Dispose();
        _client?.Dispose();
        _disposed = true;
    }
}
