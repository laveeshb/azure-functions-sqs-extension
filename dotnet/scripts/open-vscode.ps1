#!/usr/bin/env pwsh

# Script to open the Azure Functions SQS Extension workspace in VS Code

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnetDir = Join-Path (Split-Path -Parent $scriptDir) ""

# Open the dotnet directory as workspace
code $dotnetDir

Write-Host "Opening Azure Functions SQS Extension workspace in VS Code..." -ForegroundColor Cyan
Write-Host "Projects: Extensions.SQS (src) and Extensions.SQS.Sample (test)" -ForegroundColor Gray
