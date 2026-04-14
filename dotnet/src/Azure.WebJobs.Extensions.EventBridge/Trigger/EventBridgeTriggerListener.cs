// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.Logging;

/// <summary>
/// Listener for EventBridge HTTP webhook triggers via API Destination.
/// Registers with the webhook handler to receive events.
/// </summary>
public class EventBridgeTriggerListener : IListener
{
    private readonly EventBridgeTriggerAttribute _attribute;
    private readonly ITriggeredFunctionExecutor _executor;
    private readonly ILogger _logger;
    private readonly EventBridgeWebhookHandler _webhookHandler;
    private readonly FunctionDescriptor _functionDescriptor;
    private readonly string _route;
    private bool _disposed;

    public EventBridgeTriggerListener(
        EventBridgeTriggerAttribute attribute,
        ITriggeredFunctionExecutor executor,
        ILoggerFactory loggerFactory,
        EventBridgeWebhookHandler webhookHandler,
        FunctionDescriptor functionDescriptor)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _functionDescriptor = functionDescriptor ?? throw new ArgumentNullException(nameof(functionDescriptor));
        _logger = loggerFactory?.CreateLogger<EventBridgeTriggerListener>() 
            ?? throw new ArgumentNullException(nameof(loggerFactory));

        // Determine the route for this listener
        _route = !string.IsNullOrEmpty(_attribute.Route) 
            ? _attribute.Route 
            : _functionDescriptor.ShortName;

        _logger.LogInformation("EventBridge trigger listener created for route: {Route}", _route);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting EventBridge trigger listener for function '{FunctionName}' on route '{Route}'",
            _functionDescriptor.ShortName,
            _route);

        // Register this listener with the webhook handler
        _webhookHandler.RegisterListener(_route, _attribute, ExecuteAsync);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping EventBridge trigger listener for function '{FunctionName}'",
            _functionDescriptor.ShortName);

        // Unregister from the webhook handler
        _webhookHandler.UnregisterListener(_route);

        return Task.CompletedTask;
    }

    private async Task<FunctionResult> ExecuteAsync(EventBridgeEvent eventBridgeEvent, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Executing function '{FunctionName}' for EventBridge event {EventId} from source '{Source}'",
            _functionDescriptor.ShortName,
            eventBridgeEvent.Id,
            eventBridgeEvent.Source);

        var input = new TriggeredFunctionData
        {
            TriggerValue = eventBridgeEvent
        };

        return await _executor.TryExecuteAsync(input, cancellationToken);
    }

    public void Cancel()
    {
        // Nothing to cancel for webhook-based trigger
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _webhookHandler.UnregisterListener(_route);
            _disposed = true;
        }
    }
}
