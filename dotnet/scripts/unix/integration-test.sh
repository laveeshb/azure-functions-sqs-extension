#!/bin/bash

# Integration test script for AWS extensions
# Runs tests against LocalStack using Testcontainers (Docker required)

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$SCRIPT_DIR/../.."

echo "==================================================="
echo "Azure Functions AWS Extensions - Integration Tests"
echo "==================================================="
echo ""

# Check for Docker
if ! command -v docker &> /dev/null; then
    echo "ERROR: Docker is required for integration tests"
    echo "Please install Docker: https://docs.docker.com/get-docker/"
    exit 1
fi

# Check Docker is running
if ! docker info &> /dev/null; then
    echo "ERROR: Docker daemon is not running"
    echo "Please start Docker and try again"
    exit 1
fi

echo "Docker is available and running"
echo ""

# Navigate to root
cd "$ROOT_DIR"

echo "Building solution..."
dotnet build --configuration Release

echo ""
echo "Running integration tests..."
echo "Note: This will automatically start LocalStack container via Testcontainers"
echo ""

# Run integration tests with detailed output
dotnet test test/Extensions.AWS.IntegrationTests/Extensions.AWS.IntegrationTests.csproj \
    --configuration Release \
    --logger "console;verbosity=detailed" \
    --no-build

echo ""
echo "==================================================="
echo "Integration tests completed successfully!"
echo "==================================================="
