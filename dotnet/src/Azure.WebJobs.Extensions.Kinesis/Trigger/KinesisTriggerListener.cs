// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;

/// <summary>
/// Listener for Kinesis stream triggers.
/// Polls Kinesis shards for new records and invokes the function.
/// </summary>
public class KinesisTriggerListener : IListener
{
    private readonly KinesisTriggerAttribute _attribute;
    private readonly ITriggeredFunctionExecutor _executor;
    private readonly ILogger _logger;
    private AmazonKinesisClient? _kinesisClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask;
    private bool _disposed;

    // Track shard iterators for each shard
    private readonly ConcurrentDictionary<string, string> _shardIterators = new();

    public KinesisTriggerListener(
        KinesisTriggerAttribute attribute,
        ITriggeredFunctionExecutor executor,
        ILoggerFactory loggerFactory)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = loggerFactory?.CreateLogger<KinesisTriggerListener>() 
            ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KinesisTriggerListener));
        }

        _logger.LogInformation(
            "Starting Kinesis trigger listener for stream: {StreamName}",
            _attribute.StreamName);

        _kinesisClient = AmazonKinesisClientFactory.Build(_attribute);
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Initialize shard iterators
        await InitializeShardIteratorsAsync(_cancellationTokenSource.Token);

        // Start polling task
        _pollingTask = Task.Run(() => PollLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    private async Task InitializeShardIteratorsAsync(CancellationToken cancellationToken)
    {
        var describeRequest = new DescribeStreamRequest
        {
            StreamName = _attribute.StreamName
        };

        var response = await _kinesisClient!.DescribeStreamAsync(describeRequest, cancellationToken);
        var shards = response.StreamDescription.Shards;

        _logger.LogInformation(
            "Found {ShardCount} shards for stream {StreamName}",
            shards.Count,
            _attribute.StreamName);

        foreach (var shard in shards)
        {
            var iteratorRequest = new GetShardIteratorRequest
            {
                StreamName = _attribute.StreamName,
                ShardId = shard.ShardId,
                ShardIteratorType = GetShardIteratorType()
            };

            if (_attribute.StartingPosition == "AT_TIMESTAMP" && _attribute.StartingTimestamp.HasValue)
            {
                iteratorRequest.Timestamp = _attribute.StartingTimestamp.Value;
            }

            var iteratorResponse = await _kinesisClient.GetShardIteratorAsync(iteratorRequest, cancellationToken);
            _shardIterators[shard.ShardId] = iteratorResponse.ShardIterator;

            _logger.LogDebug(
                "Initialized iterator for shard {ShardId} with type {IteratorType}",
                shard.ShardId,
                _attribute.StartingPosition);
        }
    }

    private ShardIteratorType GetShardIteratorType()
    {
        return _attribute.StartingPosition.ToUpperInvariant() switch
        {
            "LATEST" => ShardIteratorType.LATEST,
            "AT_TIMESTAMP" => ShardIteratorType.AT_TIMESTAMP,
            _ => ShardIteratorType.TRIM_HORIZON
        };
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollAllShardsAsync(cancellationToken);
                await Task.Delay(_attribute.PollingIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling Kinesis stream {StreamName}", _attribute.StreamName);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task PollAllShardsAsync(CancellationToken cancellationToken)
    {
        var shardIds = _shardIterators.Keys.ToList();

        foreach (var shardId in shardIds)
        {
            if (!_shardIterators.TryGetValue(shardId, out var iterator) || string.IsNullOrEmpty(iterator))
            {
                continue;
            }

            try
            {
                var request = new GetRecordsRequest
                {
                    ShardIterator = iterator,
                    Limit = _attribute.BatchSize
                };

                var response = await _kinesisClient!.GetRecordsAsync(request, cancellationToken);

                // Update the shard iterator for next poll
                if (!string.IsNullOrEmpty(response.NextShardIterator))
                {
                    _shardIterators[shardId] = response.NextShardIterator;
                }
                else
                {
                    // Shard has been closed (split or merged)
                    _shardIterators.TryRemove(shardId, out _);
                    _logger.LogInformation("Shard {ShardId} has been closed", shardId);
                }

                if (response.Records.Count > 0)
                {
                    _logger.LogDebug(
                        "Received {RecordCount} records from shard {ShardId}",
                        response.Records.Count,
                        shardId);

                    foreach (var record in response.Records)
                    {
                        await ProcessRecordAsync(record, shardId, cancellationToken);
                    }
                }

                if (response.MillisBehindLatest > 0)
                {
                    _logger.LogDebug(
                        "Shard {ShardId} is {MillisBehind}ms behind latest",
                        shardId,
                        response.MillisBehindLatest);
                }
            }
            catch (ExpiredIteratorException)
            {
                _logger.LogWarning("Iterator expired for shard {ShardId}, reinitializing", shardId);
                await ReinitializeShardIteratorAsync(shardId, cancellationToken);
            }
            catch (ProvisionedThroughputExceededException)
            {
                _logger.LogWarning("Throughput exceeded for shard {ShardId}, backing off", shardId);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task ReinitializeShardIteratorAsync(string shardId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetShardIteratorRequest
            {
                StreamName = _attribute.StreamName,
                ShardId = shardId,
                ShardIteratorType = ShardIteratorType.LATEST // Use LATEST on reinitialization
            };

            var response = await _kinesisClient!.GetShardIteratorAsync(request, cancellationToken);
            _shardIterators[shardId] = response.ShardIterator;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reinitialize iterator for shard {ShardId}", shardId);
        }
    }

    private async Task ProcessRecordAsync(Record record, string shardId, CancellationToken cancellationToken)
    {
        var kinesisRecord = new KinesisRecord
        {
            SequenceNumber = record.SequenceNumber,
            ApproximateArrivalTimestamp = record.ApproximateArrivalTimestamp,
            PartitionKey = record.PartitionKey,
            DataBytes = record.Data.ToArray(),
            EncryptionType = record.EncryptionType?.Value,
            ShardId = shardId,
            StreamName = _attribute.StreamName
        };

        var input = new TriggeredFunctionData
        {
            TriggerValue = kinesisRecord
        };

        try
        {
            var result = await _executor.TryExecuteAsync(input, cancellationToken);

            if (!result.Succeeded)
            {
                _logger.LogError(
                    result.Exception,
                    "Function execution failed for record {SequenceNumber} from shard {ShardId}",
                    record.SequenceNumber,
                    shardId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing function for record {SequenceNumber} from shard {ShardId}",
                record.SequenceNumber,
                shardId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping Kinesis trigger listener for stream: {StreamName}",
            _attribute.StreamName);

        _cancellationTokenSource?.Cancel();
        return _pollingTask ?? Task.CompletedTask;
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _kinesisClient?.Dispose();
        _disposed = true;
    }
}
