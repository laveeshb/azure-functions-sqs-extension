// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using Microsoft.Azure.WebJobs.Description;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon SNS HTTP webhook notifications.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class SnsTriggerAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the ARN of the SNS topic to accept messages from.
    /// If specified, messages from other topics will be rejected.
    /// </summary>
    [AutoResolve]
    public string? TopicArn { get; set; }

    /// <summary>
    /// Gets or sets the HTTP route for the webhook endpoint.
    /// If not specified, defaults to "sns/{functionName}".
    /// </summary>
    [AutoResolve]
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets whether to verify SNS message signatures. Default is true.
    /// Signature verification confirms messages are authentically from AWS SNS.
    /// </summary>
    public bool VerifySignature { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically confirm subscription requests. Default is true.
    /// </summary>
    public bool AutoConfirmSubscription { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional subject filter pattern.
    /// Only messages with matching subjects will trigger the function.
    /// </summary>
    [AutoResolve]
    public string? SubjectFilter { get; set; }
}
