// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.WebJobs.Extensions.SNS;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(SnsExtensionStartup))]

namespace Azure.WebJobs.Extensions.SNS;

/// <summary>
/// Startup class that registers the SNS extension with the Azure Functions host.
/// </summary>
public class SnsExtensionStartup : IWebJobsStartup
{
    /// <summary>
    /// Configures the WebJobs host to use the SNS extension.
    /// </summary>
    public void Configure(IWebJobsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        // Register HttpClient factory for SNS operations
        builder.Services.AddHttpClient("SNS");
        
        // Register options (can be configured via host.json)
        builder.Services.AddOptions<SnsWebhookOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                var section = config.GetSection("extensions:sns");
                if (section.GetChildren().Any())
                {
                    ConfigurationBinder.Bind(section, options);
                }
            });
        
        builder.AddExtension<SnsExtensionProvider>();
    }
}
