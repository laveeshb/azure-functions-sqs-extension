
namespace Azure.Functions.Extensions.SQS;

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs;

public class SqsQueueAsyncCollector : IAsyncCollector<SendMessageRequest>, IDisposable
{
    private readonly AmazonSQSClient _amazonSqsClient;
    private readonly SqsQueueOutAttribute _sqsQueueOut;
    private bool _disposed;

    public SqsQueueAsyncCollector(SqsQueueOutAttribute sqsQueueOut)
    {
        _sqsQueueOut = sqsQueueOut ?? throw new ArgumentNullException(nameof(sqsQueueOut));
        _amazonSqsClient = AmazonSQSClientFactory.Build(sqsQueueOut);
    }

    public async Task AddAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SqsQueueAsyncCollector));

        ArgumentNullException.ThrowIfNull(request);

        request.QueueUrl ??= _sqsQueueOut.QueueUrl;
        await _amazonSqsClient.SendMessageAsync(request, cancellationToken);
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // Batching not supported - messages are sent immediately in AddAsync
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _amazonSqsClient?.Dispose();
        _disposed = true;
    }
}
