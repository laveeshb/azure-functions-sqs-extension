// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

// Link this Worker extension to its corresponding WebJobs extension package.
// The host will load the WebJobs extension to execute the actual S3 API calls.
[assembly: ExtensionInformation("Extensions.Azure.WebJobs.S3", "1.0.0")]
