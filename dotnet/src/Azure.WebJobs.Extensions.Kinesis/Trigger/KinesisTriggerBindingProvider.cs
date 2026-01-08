// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Trigger binding provider for Kinesis stream triggers.
/// </summary>
public class KinesisTriggerBindingProvider : ITriggerBindingProvider
{
    private readonly ILoggerFactory _loggerFactory;

    public KinesisTriggerBindingProvider(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<KinesisTriggerAttribute>();

        if (attribute == null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }

        var binding = new KinesisTriggerBinding(
            parameterInfo: parameter,
            attribute: attribute,
            loggerFactory: _loggerFactory);

        return Task.FromResult<ITriggerBinding?>(binding);
    }
}
