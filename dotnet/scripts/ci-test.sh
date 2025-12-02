#!/bin/bash
set -e

# Script to run Azure Functions SQS Extension tests

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_DIR="$SCRIPT_DIR/dotnet"

# Default options
RUN_INPROCESS=true
RUN_ISOLATED=true
CONFIGURATION="Debug"
AWS_ACCESS_KEY_ID_ARG=""
AWS_SECRET_ACCESS_KEY_ARG=""
AWS_REGION="us-east-1"
SQS_QUEUE_URL=""
SQS_OUTPUT_QUEUE_URL=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --inprocess-only)
            RUN_INPROCESS=true
            RUN_ISOLATED=false
            shift
            ;;
        --isolated-only)
            RUN_INPROCESS=false
            RUN_ISOLATED=true
            shift
            ;;
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --aws-access-key-id)
            AWS_ACCESS_KEY_ID_ARG="$2"
            shift 2
            ;;
        --aws-secret-access-key)
            AWS_SECRET_ACCESS_KEY_ARG="$2"
            shift 2
            ;;
        --aws-region)
            AWS_REGION="$2"
            shift 2
            ;;
        --queue-url)
            SQS_QUEUE_URL="$2"
            shift 2
            ;;
        --output-queue-url)
            SQS_OUTPUT_QUEUE_URL="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: ./test.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --inprocess-only                     Run only in-process (WebJobs) tests"
            echo "  --isolated-only                      Run only isolated worker tests"
            echo "  -c, --configuration <Debug|Release>  Build configuration (default: Debug)"
            echo "  --aws-access-key-id <key>            AWS Access Key ID"
            echo "  --aws-secret-access-key <key>        AWS Secret Access Key"
            echo "  --aws-region <region>                AWS Region (default: us-east-1)"
            echo "  --queue-url <url>                    SQS Queue URL for trigger tests"
            echo "  --output-queue-url <url>             SQS Queue URL for output tests"
            echo "  -h, --help                           Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./ci-test.sh                            # Run all tests (uses existing local.settings.json)"
            echo "  ./ci-test.sh --inprocess-only           # Run only in-process tests"
            echo "  ./ci-test.sh --queue-url https://sqs.us-east-1.amazonaws.com/123/my-queue \\"
            echo "            --aws-access-key-id AKIA... --aws-secret-access-key ..."
            echo ""
            echo "Environment Variables:"
            echo "  AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION can also be used"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# Ensure .NET is available
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Run ./install-prereqs.sh first."
    exit 1
fi

echo "=== Running Azure Functions SQS Extension Tests ==="
echo "Configuration: $CONFIGURATION"
echo ""

# Use command-line args or fall back to environment variables
if [ -n "$AWS_ACCESS_KEY_ID_ARG" ]; then
    export AWS_ACCESS_KEY_ID="$AWS_ACCESS_KEY_ID_ARG"
fi
if [ -n "$AWS_SECRET_ACCESS_KEY_ARG" ]; then
    export AWS_SECRET_ACCESS_KEY="$AWS_SECRET_ACCESS_KEY_ARG"
fi
if [ -n "$AWS_REGION" ]; then
    export AWS_DEFAULT_REGION="$AWS_REGION"
fi

# Check if AWS credentials are configured
if [ -z "$AWS_ACCESS_KEY_ID" ] && [ ! -f "$HOME/.aws/credentials" ]; then
    echo "⚠️  Warning: AWS credentials not configured."
    echo "   Provide credentials via:"
    echo "     - Command line: --aws-access-key-id and --aws-secret-access-key"
    echo "     - Environment: AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY"
    echo "     - AWS CLI: Run 'aws configure'"
    echo ""
fi

# Function to update local.settings.json
update_local_settings() {
    local settings_file=$1
    local temp_file="${settings_file}.tmp"
    
    if [ ! -f "$settings_file" ]; then
        echo "Creating $settings_file..."
        cat > "$settings_file" << 'EOF'
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "AWS_ACCESS_KEY_ID": "",
    "AWS_SECRET_ACCESS_KEY": "",
    "AWS_REGION": "us-east-1",
    "SQS_QUEUE_URL": "",
    "SQS_OUTPUT_QUEUE_URL": ""
  }
}
EOF
    fi
    
    # Update values if provided
    if [ -n "$AWS_ACCESS_KEY_ID" ] || [ -n "$SQS_QUEUE_URL" ] || [ -n "$SQS_OUTPUT_QUEUE_URL" ]; then
        echo "Updating $settings_file with provided values..."
        
        # Use jq if available, otherwise manual sed
        if command -v jq &> /dev/null; then
            jq \
                --arg access_key "${AWS_ACCESS_KEY_ID:-}" \
                --arg secret_key "${AWS_SECRET_ACCESS_KEY:-}" \
                --arg region "${AWS_DEFAULT_REGION:-us-east-1}" \
                --arg queue_url "${SQS_QUEUE_URL:-}" \
                --arg output_url "${SQS_OUTPUT_QUEUE_URL:-}" \
                '
                if $access_key != "" then .Values.AWS_ACCESS_KEY_ID = $access_key else . end |
                if $secret_key != "" then .Values.AWS_SECRET_ACCESS_KEY = $secret_key else . end |
                if $region != "" then .Values.AWS_REGION = $region else . end |
                if $queue_url != "" then .Values.SQS_QUEUE_URL = $queue_url else . end |
                if $output_url != "" then .Values.SQS_OUTPUT_QUEUE_URL = $output_url else . end
                ' "$settings_file" > "$temp_file" && mv "$temp_file" "$settings_file"
        else
            echo "  (jq not installed - skipping automatic update)"
            echo "  Please manually update: $settings_file"
        fi
    fi
}

# Check if Azurite is running
if ! pgrep -x "azurite" > /dev/null; then
    echo "⚠️  Warning: Azurite not running. Starting Azurite..."
    mkdir -p /tmp/azurite
    azurite --silent --location /tmp/azurite --blobPort 10000 --queuePort 10001 --tablePort 10002 &
    AZURITE_PID=$!
    sleep 2
    echo "✓ Azurite started (PID: $AZURITE_PID)"
    echo ""
else
    echo "✓ Azurite is running"
    echo ""
    AZURITE_PID=""
fi

EXIT_CODE=0

# Run in-process tests
if [ "$RUN_INPROCESS" = true ]; then
    echo "=== Building In-Process Test App ==="
    
    INPROCESS_DIR="$DOTNET_DIR/test/Extensions.SQS.Test.InProcess"
    INPROCESS_SETTINGS="$INPROCESS_DIR/local.settings.json"
    
    # Update local.settings.json with provided credentials/URLs
    update_local_settings "$INPROCESS_SETTINGS"
    
    dotnet build "$INPROCESS_DIR/Extensions.SQS.Test.InProcess.csproj" -c "$CONFIGURATION"
    
    echo ""
    echo "=== In-Process (WebJobs) Test App ==="
    echo "Configuration file: $INPROCESS_SETTINGS"
    echo ""
    echo "To run manually:"
    echo "  cd $INPROCESS_DIR"
    echo "  func start"
    echo ""
    echo "Test the trigger:"
    if [ -n "$SQS_QUEUE_URL" ]; then
        echo "  aws sqs send-message --queue-url $SQS_QUEUE_URL --message-body 'Test message'"
    else
        echo "  aws sqs send-message --queue-url <QUEUE_URL> --message-body 'Test message'"
    fi
    echo ""
    echo "Test the output:"
    echo "  curl 'http://localhost:7071/api/send-simple?message=Hello'"
    echo ""
fi

# Run isolated worker tests
if [ "$RUN_ISOLATED" = true ]; then
    echo "=== Building Isolated Worker Test App ==="
    
    ISOLATED_DIR="$DOTNET_DIR/test/Extensions.SQS.Test.Isolated"
    ISOLATED_SETTINGS="$ISOLATED_DIR/local.settings.json"
    
    # Update local.settings.json with provided credentials/URLs
    update_local_settings "$ISOLATED_SETTINGS"
    
    dotnet build "$ISOLATED_DIR/Extensions.SQS.Test.Isolated.csproj" -c "$CONFIGURATION"
    
    echo ""
    echo "=== Isolated Worker Test App ==="
    echo "Configuration file: $ISOLATED_SETTINGS"
    echo ""
    echo "To run manually:"
    echo "  cd $ISOLATED_DIR"
    echo "  func start"
    echo ""
    echo "Test the trigger:"
    if [ -n "$SQS_QUEUE_URL" ]; then
        echo "  aws sqs send-message --queue-url $SQS_QUEUE_URL --message-body 'Test message'"
    else
        echo "  aws sqs send-message --queue-url <QUEUE_URL> --message-body 'Test message'"
    fi
    echo ""
    echo "Test the output:"
    echo "  curl 'http://localhost:7071/api/SendSimpleMessage?message=Hello'"
    echo ""
fi

# Cleanup
if [ -n "$AZURITE_PID" ]; then
    echo "Stopping Azurite (PID: $AZURITE_PID)..."
    kill $AZURITE_PID 2>/dev/null || true
fi

echo "=== Test Build Complete ==="
echo ""
echo "Note: These are integration test apps, not unit tests."
echo "      They require AWS credentials and SQS queues to be configured."
echo ""
if [ -z "$AWS_ACCESS_KEY_ID" ] || [ -z "$SQS_QUEUE_URL" ]; then
    echo "Quick start - provide credentials and queue URL:"
    echo "  ./test.sh \\"
    echo "    --aws-access-key-id AKIA... \\"
    echo "    --aws-secret-access-key ... \\"
    echo "    --queue-url https://sqs.us-east-1.amazonaws.com/123/my-queue \\"
    echo "    --output-queue-url https://sqs.us-east-1.amazonaws.com/123/my-output-queue"
    echo ""
fi
echo "Configuration files:"
if [ "$RUN_INPROCESS" = true ]; then
    echo "  In-Process:  $DOTNET_DIR/test/Extensions.SQS.Test.InProcess/local.settings.json"
fi
if [ "$RUN_ISOLATED" = true ]; then
    echo "  Isolated:    $DOTNET_DIR/test/Extensions.SQS.Test.Isolated/local.settings.json"
fi

exit $EXIT_CODE
