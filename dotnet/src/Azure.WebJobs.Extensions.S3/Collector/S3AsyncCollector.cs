// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Azure.WebJobs;

/// <summary>
/// Async collector for uploading objects to Amazon S3.
/// </summary>
internal sealed class S3AsyncCollector : IAsyncCollector<PutObjectRequest>, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly S3OutAttribute _attribute;
    private bool _disposed;

    public S3AsyncCollector(S3OutAttribute attribute)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _client = AmazonS3ClientFactory.Build(attribute);
    }

    public async Task AddAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(S3AsyncCollector));

        ArgumentNullException.ThrowIfNull(request);

        // Apply defaults from attribute if not specified
        request.BucketName ??= _attribute.BucketName;

        // Apply key prefix if specified
        if (!string.IsNullOrEmpty(_attribute.KeyPrefix) && !string.IsNullOrEmpty(request.Key))
        {
            if (!request.Key.StartsWith(_attribute.KeyPrefix))
            {
                request.Key = $"{_attribute.KeyPrefix.TrimEnd('/')}/{request.Key}";
            }
        }

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Objects are uploaded immediately in AddAsync
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _client?.Dispose();
        _disposed = true;
    }
}
