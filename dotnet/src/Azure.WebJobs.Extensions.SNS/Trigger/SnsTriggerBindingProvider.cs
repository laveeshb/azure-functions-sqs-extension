// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Binding provider for SNS HTTP webhook triggers.
/// </summary>
public class SnsTriggerBindingProvider : ITriggerBindingProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly INameResolver _nameResolver;
    private readonly SnsSignatureValidator _signatureValidator;
    private readonly SnsWebhookHandler _webhookHandler;

    public SnsTriggerBindingProvider(
        ILoggerFactory loggerFactory,
        INameResolver nameResolver,
        SnsSignatureValidator signatureValidator,
        SnsWebhookHandler webhookHandler)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
    }

    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<SnsTriggerAttribute>();

        if (attribute == null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }

        // Resolve any %AppSetting% values
        if (!string.IsNullOrEmpty(attribute.TopicArn))
        {
            attribute.TopicArn = ResolveSettingValue(attribute.TopicArn);
        }

        if (!string.IsNullOrEmpty(attribute.Route))
        {
            attribute.Route = ResolveSettingValue(attribute.Route);
        }

        if (!string.IsNullOrEmpty(attribute.SubjectFilter))
        {
            attribute.SubjectFilter = ResolveSettingValue(attribute.SubjectFilter);
        }

        var binding = new SnsTriggerBinding(
            parameterInfo: parameter,
            attribute: attribute,
            loggerFactory: _loggerFactory,
            signatureValidator: _signatureValidator,
            webhookHandler: _webhookHandler);

        return Task.FromResult<ITriggerBinding?>(binding);
    }

    /// <summary>
    /// Resolves %AppSetting% syntax in attribute values.
    /// </summary>
    private string ResolveSettingValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Handle %SettingName% syntax
        if (value.StartsWith('%') && value.EndsWith('%') && value.Length > 2)
        {
            var settingName = value[1..^1];
            var resolved = _nameResolver.Resolve(settingName);
            return resolved ?? value;
        }

        return value;
    }
}
