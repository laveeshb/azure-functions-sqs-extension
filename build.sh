#!/bin/bash
set -e

# Script to build Azure Functions SQS Extensions

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_DIR="$SCRIPT_DIR/dotnet"

# Default options
CONFIGURATION="Debug"
CREATE_PACKAGE=false
CLEAN=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -p|--package)
            CREATE_PACKAGE=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./build.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -c, --configuration <Debug|Release>  Build configuration (default: Debug)"
            echo "  -p, --package                        Create NuGet packages"
            echo "  --clean                              Clean before building"
            echo "  -h, --help                           Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./build.sh                           # Debug build, no packages"
            echo "  ./build.sh -c Release -p             # Release build with packages"
            echo "  ./build.sh --clean -c Release -p     # Clean, then release build with packages"
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

echo "=== Building Azure Functions SQS Extensions ==="
echo "Configuration: $CONFIGURATION"
echo "Create packages: $CREATE_PACKAGE"
echo "Clean build: $CLEAN"
echo ""

# Clean if requested
if [ "$CLEAN" = true ]; then
    echo "Cleaning..."
    dotnet clean "$DOTNET_DIR/src/Azure.WebJobs.Extensions.SQS/Azure.WebJobs.Extensions.SQS.csproj" -c "$CONFIGURATION"
    dotnet clean "$DOTNET_DIR/src/Azure.Functions.Worker.Extensions.SQS/Azure.Functions.Worker.Extensions.SQS.csproj" -c "$CONFIGURATION"
    echo ""
fi

# Build WebJobs Extension
echo "Building Azure.WebJobs.Extensions.SQS..."
dotnet restore "$DOTNET_DIR/src/Azure.WebJobs.Extensions.SQS/Azure.WebJobs.Extensions.SQS.csproj"
if [ "$CREATE_PACKAGE" = true ]; then
    dotnet build "$DOTNET_DIR/src/Azure.WebJobs.Extensions.SQS/Azure.WebJobs.Extensions.SQS.csproj" \
        -c "$CONFIGURATION" \
        /p:GeneratePackageOnBuild=true
else
    dotnet build "$DOTNET_DIR/src/Azure.WebJobs.Extensions.SQS/Azure.WebJobs.Extensions.SQS.csproj" \
        -c "$CONFIGURATION" \
        /p:GeneratePackageOnBuild=false
fi
echo ""

# Build Worker Extension
echo "Building Azure.Functions.Worker.Extensions.SQS..."
dotnet restore "$DOTNET_DIR/src/Azure.Functions.Worker.Extensions.SQS/Azure.Functions.Worker.Extensions.SQS.csproj"
if [ "$CREATE_PACKAGE" = true ]; then
    dotnet build "$DOTNET_DIR/src/Azure.Functions.Worker.Extensions.SQS/Azure.Functions.Worker.Extensions.SQS.csproj" \
        -c "$CONFIGURATION" \
        /p:GeneratePackageOnBuild=true
else
    dotnet build "$DOTNET_DIR/src/Azure.Functions.Worker.Extensions.SQS/Azure.Functions.Worker.Extensions.SQS.csproj" \
        -c "$CONFIGURATION" \
        /p:GeneratePackageOnBuild=false
fi
echo ""

# Summary
echo "=== Build Complete ==="
echo ""
if [ "$CREATE_PACKAGE" = true ]; then
    echo "NuGet packages created:"
    find "$DOTNET_DIR/src" -name "*.nupkg" -path "*/$CONFIGURATION/*" -exec ls -lh {} \;
    echo ""
fi

echo "Build artifacts:"
echo "  WebJobs Extension:   $DOTNET_DIR/src/Azure.WebJobs.Extensions.SQS/bin/$CONFIGURATION/"
echo "  Worker Extension:    $DOTNET_DIR/src/Azure.Functions.Worker.Extensions.SQS/bin/$CONFIGURATION/"
echo ""
echo "Next steps:"
echo "  Run tests: ./test.sh"
if [ "$CREATE_PACKAGE" = false ]; then
    echo "  Create packages: ./build.sh -c Release -p"
fi
