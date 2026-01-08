// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Microsoft.Azure.WebJobs;

/// <summary>
/// Async collector for sending records to Amazon Kinesis.
/// </summary>
internal sealed class KinesisAsyncCollector : IAsyncCollector<PutRecordRequest>, IDisposable
{
    private readonly AmazonKinesisClient _client;
    private readonly KinesisOutAttribute _attribute;
    private bool _disposed;

    public KinesisAsyncCollector(KinesisOutAttribute attribute)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _client = AmazonKinesisClientFactory.Build(attribute);
    }

    public async Task AddAsync(PutRecordRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(KinesisAsyncCollector));

        ArgumentNullException.ThrowIfNull(request);

        // Apply defaults from attribute if not specified
        request.StreamName ??= _attribute.StreamName;
        request.PartitionKey ??= _attribute.PartitionKey;

        if (string.IsNullOrEmpty(request.PartitionKey))
        {
            throw new InvalidOperationException("PartitionKey is required for Kinesis records.");
        }

        await _client.PutRecordAsync(request, cancellationToken);
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Records are sent immediately in AddAsync
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
