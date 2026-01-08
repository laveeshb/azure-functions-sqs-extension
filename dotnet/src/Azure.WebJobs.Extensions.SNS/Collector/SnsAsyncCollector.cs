// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Azure.WebJobs;

/// <summary>
/// Async collector for publishing messages to Amazon SNS.
/// </summary>
internal sealed class SnsAsyncCollector : IAsyncCollector<PublishRequest>, IDisposable
{
    private readonly AmazonSimpleNotificationServiceClient _client;
    private readonly SnsOutAttribute _attribute;
    private bool _disposed;

    public SnsAsyncCollector(SnsOutAttribute attribute)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _client = AmazonSnsClientFactory.Build(attribute);
    }

    public async Task AddAsync(PublishRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SnsAsyncCollector));

        ArgumentNullException.ThrowIfNull(request);

        // Apply defaults from attribute if not specified
        request.TopicArn ??= _attribute.TopicArn;
        request.Subject ??= _attribute.Subject;

        await _client.PublishAsync(request, cancellationToken);
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Messages are sent immediately in AddAsync
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
