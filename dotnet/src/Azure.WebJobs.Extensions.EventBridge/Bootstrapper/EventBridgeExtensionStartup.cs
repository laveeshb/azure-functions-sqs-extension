// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.WebJobs.Extensions.EventBridge;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(EventBridgeExtensionStartup))]

namespace Azure.WebJobs.Extensions.EventBridge;

/// <summary>
/// Startup class that registers the EventBridge extension with the Azure Functions host.
/// </summary>
public class EventBridgeExtensionStartup : IWebJobsStartup
{
    /// <summary>
    /// Configures the WebJobs host to use the EventBridge extension.
    /// </summary>
    public void Configure(IWebJobsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register services for the EventBridge trigger
        builder.Services.AddSingleton<EventBridgeWebhookHandler>();
        builder.Services.AddSingleton<EventBridgeTriggerBindingProvider>();

        builder.AddExtension<EventBridgeExtensionProvider>();
    }
}
