#!/usr/bin/env pwsh

# Script to run Azure Functions SQS Extension tests

param(
    [switch]$InProcessOnly,
    
    [switch]$IsolatedOnly,
    
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [string]$AwsAccessKeyId,
    
    [string]$AwsSecretAccessKey,
    
    [string]$AwsRegion = "us-east-1",
    
    [string]$QueueUrl,
    
    [string]$OutputQueueUrl,
    
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\ci-test.ps1 [OPTIONS]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -InProcessOnly                     Run only in-process (WebJobs) tests"
    Write-Host "  -IsolatedOnly                      Run only isolated worker tests"
    Write-Host "  -Configuration <Debug|Release>     Build configuration (default: Debug)"
    Write-Host "  -AwsAccessKeyId <key>              AWS Access Key ID"
    Write-Host "  -AwsSecretAccessKey <key>          AWS Secret Access Key"
    Write-Host "  -AwsRegion <region>                AWS Region (default: us-east-1)"
    Write-Host "  -QueueUrl <url>                    SQS Queue URL for trigger tests"
    Write-Host "  -OutputQueueUrl <url>              SQS Queue URL for output tests"
    Write-Host "  -Help                              Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\ci-test.ps1                            # Run all tests (uses existing local.settings.json)"
    Write-Host "  .\ci-test.ps1 -InProcessOnly             # Run only in-process tests"
    Write-Host "  .\ci-test.ps1 -QueueUrl https://sqs.us-east-1.amazonaws.com/123/my-queue ``"
    Write-Host "            -AwsAccessKeyId AKIA... -AwsSecretAccessKey ..."
    Write-Host ""
    Write-Host "Environment Variables:" -ForegroundColor Yellow
    Write-Host "  AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION can also be used"
    exit 0
}

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$scriptsDir = Split-Path -Parent $scriptDir
$dotnetDir = Split-Path -Parent $scriptsDir

# Default options
$RUN_INPROCESS = $true
$RUN_ISOLATED = $true

if ($InProcessOnly) {
    $RUN_INPROCESS = $true
    $RUN_ISOLATED = $false
}

if ($IsolatedOnly) {
    $RUN_INPROCESS = $false
    $RUN_ISOLATED = $true
}

# Ensure .NET is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET SDK not found. Run .\install-prereqs.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "=== Running Azure Functions SQS Extension Tests ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

# Use command-line args or fall back to environment variables
if ($AwsAccessKeyId) {
    $env:AWS_ACCESS_KEY_ID = $AwsAccessKeyId
}
if ($AwsSecretAccessKey) {
    $env:AWS_SECRET_ACCESS_KEY = $AwsSecretAccessKey
}
if ($AwsRegion) {
    $env:AWS_DEFAULT_REGION = $AwsRegion
}

# Check if AWS credentials are configured
if (-not $env:AWS_ACCESS_KEY_ID -and -not (Test-Path "$env:USERPROFILE\.aws\credentials")) {
    Write-Host "⚠️  Warning: AWS credentials not configured." -ForegroundColor Yellow
    Write-Host "   Provide credentials via:" -ForegroundColor Gray
    Write-Host "     - Command line: -AwsAccessKeyId and -AwsSecretAccessKey"
    Write-Host "     - Environment: `$env:AWS_ACCESS_KEY_ID and `$env:AWS_SECRET_ACCESS_KEY"
    Write-Host "     - AWS CLI: Run 'aws configure'"
    Write-Host ""
}

# Function to update local.settings.json
function Update-LocalSettings {
    param(
        [string]$SettingsFile
    )
    
    if (-not (Test-Path $SettingsFile)) {
        Write-Host "Creating $SettingsFile..." -ForegroundColor Yellow
        $settings = @{
            IsEncrypted = $false
            Values = @{
                AzureWebJobsStorage = "UseDevelopmentStorage=true"
                FUNCTIONS_WORKER_RUNTIME = "dotnet"
                AWS_ACCESS_KEY_ID = ""
                AWS_SECRET_ACCESS_KEY = ""
                AWS_REGION = "us-east-1"
                SQS_QUEUE_URL = ""
                SQS_OUTPUT_QUEUE_URL = ""
            }
        }
        $settings | ConvertTo-Json | Set-Content $SettingsFile
    }
    
    # Update values if provided
    if ($env:AWS_ACCESS_KEY_ID -or $QueueUrl -or $OutputQueueUrl) {
        Write-Host "Updating $SettingsFile with provided values..." -ForegroundColor Yellow
        
        try {
            $settings = Get-Content $SettingsFile | ConvertFrom-Json
            
            if ($env:AWS_ACCESS_KEY_ID) {
                $settings.Values.AWS_ACCESS_KEY_ID = $env:AWS_ACCESS_KEY_ID
            }
            if ($env:AWS_SECRET_ACCESS_KEY) {
                $settings.Values.AWS_SECRET_ACCESS_KEY = $env:AWS_SECRET_ACCESS_KEY
            }
            if ($env:AWS_DEFAULT_REGION) {
                $settings.Values.AWS_REGION = $env:AWS_DEFAULT_REGION
            }
            if ($QueueUrl) {
                $settings.Values.SQS_QUEUE_URL = $QueueUrl
            }
            if ($OutputQueueUrl) {
                $settings.Values.SQS_OUTPUT_QUEUE_URL = $OutputQueueUrl
            }
            
            $settings | ConvertTo-Json | Set-Content $SettingsFile
        } catch {
            Write-Host "  (Could not update automatically)" -ForegroundColor Gray
            Write-Host "  Please manually update: $SettingsFile" -ForegroundColor Yellow
        }
    }
}

# Check if Azurite is running
$azuriteRunning = Get-Process -Name "azurite" -ErrorAction SilentlyContinue
if (-not $azuriteRunning) {
    Write-Host "⚠️  Warning: Azurite not running. Starting Azurite..." -ForegroundColor Yellow
    $azuriteDir = "$env:TEMP\azurite"
    if (-not (Test-Path $azuriteDir)) {
        New-Item -ItemType Directory -Path $azuriteDir | Out-Null
    }
    $azuriteProcess = Start-Process azurite -ArgumentList "--silent --location $azuriteDir --blobPort 10000 --queuePort 10001 --tablePort 10002" -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 2
    Write-Host "✓ Azurite started (PID: $($azuriteProcess.Id))" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "✓ Azurite is running" -ForegroundColor Green
    Write-Host ""
    $azuriteProcess = $null
}

$EXIT_CODE = 0

# Run in-process tests
if ($RUN_INPROCESS) {
    Write-Host "=== Building In-Process Test App ===" -ForegroundColor Cyan
    
    $INPROCESS_DIR = "$dotnetDir\test\Extensions.SQS.Test.InProcess"
    $INPROCESS_SETTINGS = "$INPROCESS_DIR\local.settings.json"
    
    # Update local.settings.json with provided credentials/URLs
    Update-LocalSettings -SettingsFile $INPROCESS_SETTINGS
    
    dotnet build "$INPROCESS_DIR\Extensions.SQS.Test.InProcess.csproj" -c $Configuration
    
    Write-Host ""
    Write-Host "=== In-Process (WebJobs) Test App ===" -ForegroundColor Green
    Write-Host "Configuration file: $INPROCESS_SETTINGS" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To run manually:" -ForegroundColor Yellow
    Write-Host "  cd $INPROCESS_DIR"
    Write-Host "  func start"
    Write-Host ""
    Write-Host "Test the trigger:" -ForegroundColor Yellow
    if ($QueueUrl) {
        Write-Host "  aws sqs send-message --queue-url $QueueUrl --message-body 'Test message'"
    } else {
        Write-Host "  aws sqs send-message --queue-url <QUEUE_URL> --message-body 'Test message'"
    }
    Write-Host ""
    Write-Host "Test the output:" -ForegroundColor Yellow
    Write-Host "  curl 'http://localhost:7071/api/send-simple?message=Hello'"
    Write-Host ""
}

# Run isolated worker tests
if ($RUN_ISOLATED) {
    Write-Host "=== Building Isolated Worker Test App ===" -ForegroundColor Cyan
    
    $ISOLATED_DIR = "$dotnetDir\test\Extensions.SQS.Test.Isolated"
    $ISOLATED_SETTINGS = "$ISOLATED_DIR\local.settings.json"
    
    # Update local.settings.json with provided credentials/URLs
    Update-LocalSettings -SettingsFile $ISOLATED_SETTINGS
    
    dotnet build "$ISOLATED_DIR\Extensions.SQS.Test.Isolated.csproj" -c $Configuration
    
    Write-Host ""
    Write-Host "=== Isolated Worker Test App ===" -ForegroundColor Green
    Write-Host "Configuration file: $ISOLATED_SETTINGS" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To run manually:" -ForegroundColor Yellow
    Write-Host "  cd $ISOLATED_DIR"
    Write-Host "  func start"
    Write-Host ""
    Write-Host "Test the trigger:" -ForegroundColor Yellow
    if ($QueueUrl) {
        Write-Host "  aws sqs send-message --queue-url $QueueUrl --message-body 'Test message'"
    } else {
        Write-Host "  aws sqs send-message --queue-url <QUEUE_URL> --message-body 'Test message'"
    }
    Write-Host ""
    Write-Host "Test the output:" -ForegroundColor Yellow
    Write-Host "  curl 'http://localhost:7071/api/SendSimpleMessage?message=Hello'"
    Write-Host ""
}

# Cleanup
if ($azuriteProcess) {
    Write-Host "Stopping Azurite (PID: $($azuriteProcess.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $azuriteProcess.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "=== Test Build Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Note: These are integration test apps, not unit tests." -ForegroundColor Gray
Write-Host "      They require AWS credentials and SQS queues to be configured." -ForegroundColor Gray
Write-Host ""
if (-not $env:AWS_ACCESS_KEY_ID -or -not $QueueUrl) {
    Write-Host "Quick start - provide credentials and queue URL:" -ForegroundColor Yellow
    Write-Host "  .\ci-test.ps1 ``"
    Write-Host "    -AwsAccessKeyId AKIA... ``"
    Write-Host "    -AwsSecretAccessKey ... ``"
    Write-Host "    -QueueUrl https://sqs.us-east-1.amazonaws.com/123/my-queue ``"
    Write-Host "    -OutputQueueUrl https://sqs.us-east-1.amazonaws.com/123/my-output-queue"
    Write-Host ""
}
Write-Host "Configuration files:" -ForegroundColor Cyan
if ($RUN_INPROCESS) {
    Write-Host "  In-Process:  $dotnetDir\test\Extensions.SQS.Test.InProcess\local.settings.json" -ForegroundColor Gray
}
if ($RUN_ISOLATED) {
    Write-Host "  Isolated:    $dotnetDir\test\Extensions.SQS.Test.Isolated\local.settings.json" -ForegroundColor Gray
}

exit $EXIT_CODE
