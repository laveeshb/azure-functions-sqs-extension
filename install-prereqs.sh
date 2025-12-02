#!/bin/bash
set -e

# Script to install prerequisites for Azure Functions SQS Extension development

echo "=== Installing Prerequisites ==="
echo ""

# Check if running on supported OS
if [[ "$OSTYPE" != "linux-gnu"* && "$OSTYPE" != "darwin"* ]]; then
    echo "Warning: This script is designed for Linux/macOS. Windows users should install manually."
fi

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Install .NET SDK
if command_exists dotnet; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✓ .NET SDK already installed: $DOTNET_VERSION"
else
    echo "Installing .NET SDK..."
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        /tmp/dotnet-install.sh --channel 8.0 --install-dir ~/.dotnet
        echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
        export PATH="$HOME/.dotnet:$PATH"
        rm /tmp/dotnet-install.sh
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        brew install dotnet-sdk
    fi
    echo "✓ .NET SDK installed"
fi

# Install Azure Functions Core Tools
if command_exists func; then
    FUNC_VERSION=$(func --version)
    echo "✓ Azure Functions Core Tools already installed: $FUNC_VERSION"
else
    echo "Installing Azure Functions Core Tools..."
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        sudo apt-get update
        sudo apt-get install -y azure-functions-core-tools-4
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        brew tap azure/functions
        brew install azure-functions-core-tools@4
    fi
    echo "✓ Azure Functions Core Tools installed"
fi

# Install AWS CLI (optional but recommended)
if command_exists aws; then
    AWS_VERSION=$(aws --version)
    echo "✓ AWS CLI already installed: $AWS_VERSION"
else
    echo "AWS CLI not found. Install? (y/n)"
    read -r INSTALL_AWS
    if [[ "$INSTALL_AWS" == "y" ]]; then
        echo "Installing AWS CLI..."
        if [[ "$OSTYPE" == "linux-gnu"* ]]; then
            curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "/tmp/awscliv2.zip"
            unzip -q /tmp/awscliv2.zip -d /tmp
            sudo /tmp/aws/install
            rm -rf /tmp/aws /tmp/awscliv2.zip
        elif [[ "$OSTYPE" == "darwin"* ]]; then
            brew install awscli
        fi
        echo "✓ AWS CLI installed"
    else
        echo "⊘ Skipping AWS CLI installation"
    fi
fi

# Install Azurite (optional, for local Azure Storage emulation)
if command_exists azurite; then
    echo "✓ Azurite already installed"
else
    echo "Azurite not found. Install? (y/n)"
    read -r INSTALL_AZURITE
    if [[ "$INSTALL_AZURITE" == "y" ]]; then
        if command_exists npm; then
            echo "Installing Azurite..."
            sudo npm install -g azurite
            echo "✓ Azurite installed"
        else
            echo "⊘ npm not found. Install Node.js first to use Azurite."
        fi
    else
        echo "⊘ Skipping Azurite installation"
    fi
fi

echo ""
echo "=== Prerequisites Installation Complete ==="
echo ""
echo "Installed components:"
dotnet --version 2>/dev/null && echo "  ✓ .NET SDK: $(dotnet --version)"
func --version 2>/dev/null && echo "  ✓ Azure Functions Core Tools: $(func --version)"
aws --version 2>/dev/null && echo "  ✓ AWS CLI: $(aws --version)"
azurite --version 2>/dev/null && echo "  ✓ Azurite: $(azurite --version)"
echo ""
echo "Next steps:"
echo "  1. Configure AWS credentials: aws configure"
echo "  2. Build extensions: ./build.sh"
echo "  3. Run tests: ./test.sh"
