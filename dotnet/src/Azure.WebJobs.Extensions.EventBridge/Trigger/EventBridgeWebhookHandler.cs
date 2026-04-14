// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Delegate for executing a function when an EventBridge event is received.
/// </summary>
public delegate Task<FunctionResult> EventBridgeEventExecutor(EventBridgeEvent eventBridgeEvent, CancellationToken cancellationToken);

/// <summary>
/// Handles incoming EventBridge webhook requests from API Destinations.
/// Implements IAsyncConverter to integrate with the /runtime/webhooks/eventbridge endpoint.
/// </summary>
public class EventBridgeWebhookHandler : IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, (EventBridgeTriggerAttribute Attribute, EventBridgeEventExecutor Executor)> _listeners;

    public EventBridgeWebhookHandler(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory?.CreateLogger<EventBridgeWebhookHandler>() 
            ?? throw new ArgumentNullException(nameof(loggerFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _listeners = new ConcurrentDictionary<string, (EventBridgeTriggerAttribute, EventBridgeEventExecutor)>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers a listener for a specific route.
    /// </summary>
    public void RegisterListener(string route, EventBridgeTriggerAttribute attribute, EventBridgeEventExecutor executor)
    {
        if (_listeners.TryAdd(route, (attribute, executor)))
        {
            _logger.LogInformation("Registered EventBridge webhook listener for route: {Route}", route);
        }
        else
        {
            _logger.LogWarning("EventBridge webhook listener already registered for route: {Route}", route);
        }
    }

    /// <summary>
    /// Unregisters a listener for a specific route.
    /// </summary>
    public void UnregisterListener(string route)
    {
        if (_listeners.TryRemove(route, out _))
        {
            _logger.LogInformation("Unregistered EventBridge webhook listener for route: {Route}", route);
        }
    }

    /// <summary>
    /// Processes an incoming EventBridge webhook request.
    /// </summary>
    public async Task<HttpResponseMessage> ProcessRequestAsync(
        HttpRequestMessage request, 
        string route, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing EventBridge webhook request for route: {Route}", route);

        // Find the registered listener for this route
        if (!_listeners.TryGetValue(route, out var listener))
        {
            _logger.LogWarning("No EventBridge listener registered for route: {Route}", route);
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"No EventBridge listener registered for route: {route}")
            };
        }

        var (attribute, executor) = listener;

        try
        {
            // Validate API key if configured
            if (!ValidateApiKey(request, attribute))
            {
                _logger.LogWarning("Invalid or missing API key for EventBridge webhook");
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Invalid or missing API key")
                };
            }

            // Read and parse the request body
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Empty request body received for EventBridge webhook");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Empty request body")
                };
            }

            _logger.LogDebug("EventBridge webhook body: {Body}", body);

            // Parse the EventBridge event
            EventBridgeEvent? eventBridgeEvent;
            try
            {
                eventBridgeEvent = JsonSerializer.Deserialize<EventBridgeEvent>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse EventBridge event JSON");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"Invalid EventBridge event format: {ex.Message}")
                };
            }

            if (eventBridgeEvent == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Failed to parse EventBridge event")
                };
            }

            // Validate event if configured
            if (attribute.ValidateEvent)
            {
                var validationError = ValidateEvent(eventBridgeEvent, attribute);
                if (validationError != null)
                {
                    _logger.LogWarning("EventBridge event validation failed: {Error}", validationError);
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(validationError)
                    };
                }
            }

            _logger.LogInformation(
                "Received EventBridge event {EventId} from source '{Source}' with detail-type '{DetailType}'",
                eventBridgeEvent.Id,
                eventBridgeEvent.Source,
                eventBridgeEvent.DetailType);

            // Execute the function
            var result = await executor(eventBridgeEvent, cancellationToken);

            if (result.Succeeded)
            {
                _logger.LogDebug("Successfully processed EventBridge event {EventId}", eventBridgeEvent.Id);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { status = "processed", eventId = eventBridgeEvent.Id }), 
                        Encoding.UTF8, "application/json")
                };
            }
            else
            {
                _logger.LogError(result.Exception, "Function execution failed for EventBridge event {EventId}", eventBridgeEvent.Id);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Function execution failed: {result.Exception?.Message}")
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing EventBridge webhook request");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent($"Error processing request: {ex.Message}")
            };
        }
    }

    private bool ValidateApiKey(HttpRequestMessage request, EventBridgeTriggerAttribute attribute)
    {
        // If no API key setting is configured, skip validation
        if (string.IsNullOrEmpty(attribute.ApiKeySettingName))
        {
            return true;
        }

        // Get the expected API key from configuration
        var expectedApiKey = _configuration[attribute.ApiKeySettingName];
        if (string.IsNullOrEmpty(expectedApiKey))
        {
            _logger.LogWarning("API key setting '{SettingName}' is not configured", attribute.ApiKeySettingName);
            return false;
        }

        // Get the API key from the request header
        var headerName = attribute.ApiKeyHeaderName ?? "x-api-key";
        if (!request.Headers.TryGetValues(headerName, out var values))
        {
            return false;
        }

        var providedApiKey = values.FirstOrDefault();
        return string.Equals(expectedApiKey, providedApiKey, StringComparison.Ordinal);
    }

    private string? ValidateEvent(EventBridgeEvent eventBridgeEvent, EventBridgeTriggerAttribute attribute)
    {
        // Validate basic event structure
        if (string.IsNullOrEmpty(eventBridgeEvent.Id))
        {
            return "Missing event id";
        }

        // Validate source filter if specified
        if (!string.IsNullOrEmpty(attribute.Source) && 
            !string.Equals(eventBridgeEvent.Source, attribute.Source, StringComparison.Ordinal))
        {
            return $"Event source '{eventBridgeEvent.Source}' does not match expected source '{attribute.Source}'";
        }

        // Validate detail type filter if specified
        if (!string.IsNullOrEmpty(attribute.DetailType) && 
            !string.Equals(eventBridgeEvent.DetailType, attribute.DetailType, StringComparison.Ordinal))
        {
            return $"Event detail-type '{eventBridgeEvent.DetailType}' does not match expected detail-type '{attribute.DetailType}'";
        }

        return null;
    }

    /// <summary>
    /// IAsyncConverter implementation - converts HTTP request to HTTP response.
    /// This is called by the Azure Functions webhook infrastructure.
    /// </summary>
    public async Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
    {
        // Extract the route from the request URL
        var path = input.RequestUri?.AbsolutePath ?? string.Empty;
        var route = ExtractRoute(path);

        return await ProcessRequestAsync(input, route, cancellationToken);
    }

    private static string ExtractRoute(string path)
    {
        // Path format: /runtime/webhooks/eventbridge/{route}
        const string webhookPrefix = "/runtime/webhooks/eventbridge/";

        if (path.StartsWith(webhookPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path.Substring(webhookPrefix.Length).TrimEnd('/');
        }

        // If no prefix matches, use the last segment as the route
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : string.Empty;
    }
}
