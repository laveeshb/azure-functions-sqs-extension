// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

/// <summary>
/// Value provider for Kinesis records.
/// Supports automatic deserialization to target types.
/// </summary>
public class KinesisRecordValueProvider : IValueProvider
{
    private readonly KinesisRecord _record;
    private readonly Type _targetType;

    public Type Type => _targetType;

    public KinesisRecordValueProvider(KinesisRecord record, Type targetType)
    {
        _record = record ?? throw new ArgumentNullException(nameof(record));
        _targetType = targetType ?? typeof(KinesisRecord);
    }

    public KinesisRecordValueProvider(KinesisRecord record) 
        : this(record, typeof(KinesisRecord))
    {
    }

    public Task<object> GetValueAsync()
    {
        // If target type is KinesisRecord, return as-is
        if (_targetType == typeof(KinesisRecord) || _targetType == typeof(object))
        {
            return Task.FromResult<object>(_record);
        }

        // If target type is string, return the data as string
        if (_targetType == typeof(string))
        {
            return Task.FromResult<object>(_record.DataAsString ?? string.Empty);
        }

        // If target type is byte[], return the raw bytes
        if (_targetType == typeof(byte[]))
        {
            return Task.FromResult<object>(_record.DataBytes ?? Array.Empty<byte>());
        }

        // Try to deserialize the data to the target type
        if (_record.DataBytes == null)
        {
            throw new InvalidOperationException("Record data is null");
        }

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(_record.DataBytes);
            var result = JsonSerializer.Deserialize(json, _targetType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize Kinesis record to {_targetType.Name}");
            }

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize Kinesis record to {_targetType.Name}: {ex.Message}", ex);
        }
    }

    public string ToInvokeString()
    {
        return $"Kinesis Record: {_record.SequenceNumber} from shard {_record.ShardId}";
    }
}
