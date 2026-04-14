// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.WebJobs.Extensions.S3;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(S3ExtensionStartup))]

namespace Azure.WebJobs.Extensions.S3;

/// <summary>
/// Startup class that registers the S3 extension with the Azure Functions host.
/// </summary>
public class S3ExtensionStartup : IWebJobsStartup
{
    /// <summary>
    /// Configures the WebJobs host to use the S3 extension.
    /// </summary>
    public void Configure(IWebJobsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddExtension<S3ExtensionProvider>();
    }
}
