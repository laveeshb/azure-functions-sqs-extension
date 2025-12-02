#!/bin/bash
# Script to open the Azure Functions SQS Extension workspace in VS Code

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Open the dotnet directory as workspace
code "$SCRIPT_DIR/dotnet"

echo "Opening Azure Functions SQS Extension workspace in VS Code..."
echo "Projects: Extensions.SQS (src) and Extensions.SQS.Sample (test)"
