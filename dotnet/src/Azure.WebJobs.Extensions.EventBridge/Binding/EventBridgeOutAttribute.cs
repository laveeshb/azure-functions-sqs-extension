// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using Microsoft.Azure.WebJobs.Description;

/// <summary>
/// Attribute used to configure an Amazon EventBridge output binding for in-process Azure Functions.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class EventBridgeOutAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name or ARN of the EventBridge event bus.
    /// </summary>
    [AutoResolve]
    public string? EventBusName { get; set; }

    /// <summary>
    /// Gets or sets the AWS Access Key ID. If not specified, uses AWS credential chain.
    /// </summary>
    [AutoResolve]
    public string? AWSKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS Secret Access Key. If not specified, uses AWS credential chain.
    /// </summary>
    [AutoResolve]
    public string? AWSAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the AWS Region (e.g., "us-east-1"). If not specified, uses AWS credential chain.
    /// </summary>
    [AutoResolve]
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the default source for events. Can be overridden per message.
    /// </summary>
    [AutoResolve]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the default detail type for events. Can be overridden per message.
    /// </summary>
    [AutoResolve]
    public string? DetailType { get; set; }
}
