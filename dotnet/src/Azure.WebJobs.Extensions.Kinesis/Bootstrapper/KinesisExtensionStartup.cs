// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.WebJobs.Extensions.Kinesis;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(KinesisExtensionStartup))]

namespace Azure.WebJobs.Extensions.Kinesis;

/// <summary>
/// Startup class that registers the Kinesis extension with the Azure Functions host.
/// </summary>
public class KinesisExtensionStartup : IWebJobsStartup
{
    /// <summary>
    /// Configures the WebJobs host to use the Kinesis extension.
    /// </summary>
    public void Configure(IWebJobsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register services for the Kinesis trigger
        builder.Services.AddSingleton<KinesisTriggerBindingProvider>();

        builder.AddExtension<KinesisExtensionProvider>();
    }
}
