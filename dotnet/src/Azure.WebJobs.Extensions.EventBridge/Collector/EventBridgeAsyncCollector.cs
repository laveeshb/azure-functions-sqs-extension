// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Azure.WebJobs;

/// <summary>
/// Async collector for sending events to Amazon EventBridge.
/// </summary>
internal sealed class EventBridgeAsyncCollector : IAsyncCollector<PutEventsRequestEntry>, IDisposable
{
    private readonly AmazonEventBridgeClient _client;
    private readonly EventBridgeOutAttribute _attribute;
    private bool _disposed;

    public EventBridgeAsyncCollector(EventBridgeOutAttribute attribute)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _client = AmazonEventBridgeClientFactory.Build(attribute);
    }

    public async Task AddAsync(PutEventsRequestEntry entry, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventBridgeAsyncCollector));

        ArgumentNullException.ThrowIfNull(entry);

        // Apply defaults from attribute if not specified on the entry
        entry.EventBusName ??= _attribute.EventBusName;
        entry.Source ??= _attribute.Source;
        entry.DetailType ??= _attribute.DetailType;

        var request = new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry> { entry }
        };

        var response = await _client.PutEventsAsync(request, cancellationToken);

        if (response.FailedEntryCount > 0)
        {
            var failedEntry = response.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.ErrorCode));
            throw new InvalidOperationException(
                $"Failed to send event to EventBridge: {failedEntry?.ErrorCode} - {failedEntry?.ErrorMessage}");
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Events are sent immediately in AddAsync
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
