// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

/// <summary>
/// Value provider for SNS notifications.
/// Supports automatic deserialization to target types (e.g., S3EventNotification).
/// </summary>
public class SnsNotificationValueProvider : IValueProvider
{
    private readonly SnsNotification _notification;
    private readonly Type _targetType;

    public Type Type => _targetType;

    public SnsNotificationValueProvider(SnsNotification notification, Type targetType)
    {
        _notification = notification ?? throw new ArgumentNullException(nameof(notification));
        _targetType = targetType ?? typeof(SnsNotification);
    }

    public SnsNotificationValueProvider(SnsNotification notification) 
        : this(notification, typeof(SnsNotification))
    {
    }

    public Task<object> GetValueAsync()
    {
        // If target type is SnsNotification, return as-is
        if (_targetType == typeof(SnsNotification) || _targetType == typeof(object))
        {
            return Task.FromResult<object>(_notification);
        }

        // Get the message body (the actual payload from SNS)
        var messageBody = _notification.Message;

        if (string.IsNullOrEmpty(messageBody))
        {
            throw new InvalidOperationException("SNS message body is null or empty");
        }

        // If target type is string, return the message body
        if (_targetType == typeof(string))
        {
            return Task.FromResult<object>(messageBody);
        }

        // Try to deserialize to the target type
        // This enables binding directly to S3EventNotification when S3 sends events via SNS
        try
        {
            var result = JsonSerializer.Deserialize(messageBody, _targetType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize SNS message to {_targetType.Name}");
            }

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize SNS message to {_targetType.Name}: {ex.Message}", ex);
        }
    }

    public string ToInvokeString()
    {
        return $"SNS Message: {_notification.MessageId}";
    }
}
