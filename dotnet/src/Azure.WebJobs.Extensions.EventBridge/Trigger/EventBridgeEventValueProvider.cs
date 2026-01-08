// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

/// <summary>
/// Value provider for EventBridge events.
/// Supports automatic deserialization to target types.
/// </summary>
public class EventBridgeEventValueProvider : IValueProvider
{
    private readonly EventBridgeEvent _event;
    private readonly Type _targetType;

    public Type Type => _targetType;

    public EventBridgeEventValueProvider(EventBridgeEvent eventBridgeEvent, Type targetType)
    {
        _event = eventBridgeEvent ?? throw new ArgumentNullException(nameof(eventBridgeEvent));
        _targetType = targetType ?? typeof(EventBridgeEvent);
    }

    public EventBridgeEventValueProvider(EventBridgeEvent eventBridgeEvent) 
        : this(eventBridgeEvent, typeof(EventBridgeEvent))
    {
    }

    public Task<object> GetValueAsync()
    {
        // If target type is EventBridgeEvent, return as-is
        if (_targetType == typeof(EventBridgeEvent) || _targetType == typeof(object))
        {
            return Task.FromResult<object>(_event);
        }

        // If target type is string, return the raw JSON of the event
        if (_targetType == typeof(string))
        {
            var json = JsonSerializer.Serialize(_event, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            return Task.FromResult<object>(json);
        }

        // Check if target type is EventBridgeEvent<TDetail>
        if (_targetType.IsGenericType && 
            _targetType.GetGenericTypeDefinition() == typeof(EventBridgeEvent<>))
        {
            // Create the generic version with the detail deserialized
            var detailType = _targetType.GetGenericArguments()[0];
            var genericEvent = CreateGenericEvent(_event, detailType);
            return Task.FromResult(genericEvent);
        }

        // Try to deserialize the full event to the target type
        try
        {
            var json = JsonSerializer.Serialize(_event, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var result = JsonSerializer.Deserialize(json, _targetType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize EventBridge event to {_targetType.Name}");
            }

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize EventBridge event to {_targetType.Name}: {ex.Message}", ex);
        }
    }

    private object CreateGenericEvent(EventBridgeEvent sourceEvent, Type detailType)
    {
        var genericType = typeof(EventBridgeEvent<>).MakeGenericType(detailType);
        var instance = Activator.CreateInstance(genericType)!;

        // Copy base properties
        genericType.GetProperty("Version")?.SetValue(instance, sourceEvent.Version);
        genericType.GetProperty("Id")?.SetValue(instance, sourceEvent.Id);
        genericType.GetProperty("Source")?.SetValue(instance, sourceEvent.Source);
        genericType.GetProperty("Account")?.SetValue(instance, sourceEvent.Account);
        genericType.GetProperty("Region")?.SetValue(instance, sourceEvent.Region);
        genericType.GetProperty("Time")?.SetValue(instance, sourceEvent.Time);
        genericType.GetProperty("DetailType")?.SetValue(instance, sourceEvent.DetailType);
        genericType.GetProperty("Resources")?.SetValue(instance, sourceEvent.Resources);

        // Deserialize detail to the specific type
        if (sourceEvent.Detail != null)
        {
            var detail = JsonSerializer.Deserialize(sourceEvent.Detail.Value.GetRawText(), detailType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            genericType.GetProperty("Detail")?.SetValue(instance, detail);
        }

        return instance;
    }

    public string ToInvokeString()
    {
        return $"EventBridge Event: {_event.Id} from {_event.Source}";
    }
}
