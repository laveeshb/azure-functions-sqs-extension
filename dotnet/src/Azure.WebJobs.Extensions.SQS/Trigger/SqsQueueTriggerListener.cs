
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
    private Timer? _triggerTimer;
    private AmazonSQSClient? _amazonSqsClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly IOptions<SqsQueueOptions> _sqsQueueOptions;
    private readonly SqsQueueTriggerAttribute _triggerParameters;
    private readonly ITriggeredFunctionExecutor _executor;
    private readonly ILogger? _logger;
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

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _triggerTimer?.Dispose();
        _triggerTimer = null;

        _amazonSqsClient?.Dispose();
        _amazonSqsClient = null;

        _disposed = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SqsQueueTriggerListener));

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        _triggerTimer = new Timer(
            callback: async (state) => await OnTriggerCallbackAsync(_cancellationTokenSource.Token),
            state: null,
            dueTime: TimeSpan.Zero,
            period: _sqsQueueOptions.Value.PollingInterval!.Value);

        _logger?.LogInformation(
            "Started SQS trigger listener for queue: {QueueUrl}, polling every {PollingInterval}",
            _triggerParameters.QueueUrl,
            _sqsQueueOptions.Value.PollingInterval);

        return Task.CompletedTask;
    }

    private async Task OnTriggerCallbackAsync(CancellationToken cancellationToken)
    {
        if (_disposed || cancellationToken.IsCancellationRequested || _amazonSqsClient == null)
            return;

        try
        {
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, 
                "Error polling SQS queue: {QueueUrl}", 
                _triggerParameters.QueueUrl);
        }
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation(
            "Stopping SQS trigger listener for queue: {QueueUrl}",
            _triggerParameters.QueueUrl);

        Dispose();
        return Task.CompletedTask;
    }
}
