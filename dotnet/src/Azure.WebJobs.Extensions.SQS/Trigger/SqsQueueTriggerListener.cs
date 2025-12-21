
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class SqsQueueTriggerListener : IListener
{
    private Task? _pollingTask;
    private AmazonSQSClient? _amazonSqsClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly IOptions<SqsQueueOptions> _sqsQueueOptions;
    private readonly SqsQueueTriggerAttribute _triggerParameters;
    private readonly ITriggeredFunctionExecutor _executor;
    private readonly ILogger? _logger;
    private volatile bool _isRunning;
    private bool _disposed;

    public SqsQueueTriggerListener(
        SqsQueueTriggerAttribute triggerParameters, 
        IOptions<SqsQueueOptions> sqsQueueOptions, 
        ITriggeredFunctionExecutor executor,
        ILogger? logger = null)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _sqsQueueOptions = sqsQueueOptions ?? throw new ArgumentNullException(nameof(sqsQueueOptions));
        _triggerParameters = triggerParameters ?? throw new ArgumentNullException(nameof(triggerParameters));
        _logger = logger;

        // Set default values
        _sqsQueueOptions.Value.MaxNumberOfMessages ??= 5;
        _sqsQueueOptions.Value.PollingInterval ??= TimeSpan.FromSeconds(5);
        _sqsQueueOptions.Value.VisibilityTimeout ??= TimeSpan.FromSeconds(30);

        _amazonSqsClient = AmazonSQSClientFactory.Build(triggerParameters);
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _amazonSqsClient?.Dispose();
        _amazonSqsClient = null;

        _disposed = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SqsQueueTriggerListener));

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;
        
        // Start background polling loop instead of timer to avoid overlapping polls
        _pollingTask = Task.Run(() => PollLoopAsync(_cancellationTokenSource.Token));

        _logger?.LogInformation(
            "Started SQS trigger listener for queue: {QueueUrl}",
            _triggerParameters.QueueUrl);

        return Task.CompletedTask;
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var messagesReceived = await PollAndProcessAsync(cancellationToken);
                
                // If no messages were received, wait before polling again
                // Long polling already waits up to 20s, so this is just an additional delay
                if (messagesReceived == 0 && _sqsQueueOptions.Value.PollingInterval.HasValue)
                {
                    await Task.Delay(_sqsQueueOptions.Value.PollingInterval.Value, cancellationToken);
                }
                // If messages were received, poll again immediately (more might be waiting)
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break; // Clean shutdown
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, 
                    "Error in polling loop for queue: {QueueUrl}. Retrying in 5 seconds.", 
                    _triggerParameters.QueueUrl);
                
                // Wait before retrying to avoid tight error loop
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        
        _logger?.LogDebug("Polling loop exited for queue: {QueueUrl}", _triggerParameters.QueueUrl);
    }

    private async Task<int> PollAndProcessAsync(CancellationToken cancellationToken)
    {
        if (_disposed || cancellationToken.IsCancellationRequested || _amazonSqsClient == null)
            return 0;

        var receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = _triggerParameters.QueueUrl,
            MaxNumberOfMessages = _sqsQueueOptions.Value.MaxNumberOfMessages!.Value,
            VisibilityTimeout = (int)_sqsQueueOptions.Value.VisibilityTimeout!.Value.TotalSeconds,
            WaitTimeSeconds = 20, // Enable long polling
            MessageAttributeNames = ["All"],
            AttributeNames = ["All"] // Request all message attributes
        };

        var result = await _amazonSqsClient.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);
        
        if (result.Messages.Count > 0)
        {
            _logger?.LogDebug(
                "Received {MessageCount} messages from queue: {QueueUrl}",
                result.Messages.Count,
                _triggerParameters.QueueUrl);

            await Task.WhenAll(result.Messages.Select(message => ProcessMessageAsync(message, cancellationToken)));
        }
        
        return result.Messages.Count;
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (_disposed || _amazonSqsClient == null)
            return;

        try
        {
            var triggerData = new TriggeredFunctionData
            {
                ParentId = Guid.NewGuid(),
                TriggerValue = message,
                TriggerDetails = new Dictionary<string, string>
                {
                    ["MessageId"] = message.MessageId,
                    ["QueueUrl"] = _triggerParameters.QueueUrl
                }
            };

            var functionExecutionResult = await _executor.TryExecuteAsync(triggerData, cancellationToken);
            
            if (functionExecutionResult.Succeeded)
            {
                // Delete message only if function succeeded
                var deleteMessageRequest = new DeleteMessageRequest
                {
                    QueueUrl = _triggerParameters.QueueUrl,
                    ReceiptHandle = message.ReceiptHandle
                };

                await _amazonSqsClient.DeleteMessageAsync(deleteMessageRequest, cancellationToken);
                
                _logger?.LogDebug(
                    "Successfully processed and deleted message: {MessageId}",
                    message.MessageId);
            }
            else
            {
                _logger?.LogWarning(
                    "Function execution failed for message: {MessageId}. Message will become visible again after visibility timeout.",
                    message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Error processing message: {MessageId}",
                message.MessageId);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation(
            "Stopping SQS trigger listener for queue: {QueueUrl}",
            _triggerParameters.QueueUrl);

        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        
        // Wait for polling loop to complete gracefully
        if (_pollingTask != null)
        {
            try
            {
                // Wait up to 30 seconds for the polling loop to finish
                await Task.WhenAny(_pollingTask, Task.Delay(TimeSpan.FromSeconds(30), cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Expected if cancellation is requested during shutdown
            }
        }

        Dispose();
    }
}
