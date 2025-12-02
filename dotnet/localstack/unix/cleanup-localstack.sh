#!/bin/bash

# LocalStack Cleanup Script
# This script stops and removes LocalStack containers, networks, and volumes

set -e

echo "🧹 Cleaning up LocalStack resources..."

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Stop and remove containers
echo "🛑 Stopping and removing LocalStack containers..."
docker-compose -f "$SCRIPT_DIR/docker-compose.localstack.yml" down -v

# Remove LocalStack images (optional)
read -p "Do you want to remove LocalStack images as well? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "🗑️  Removing LocalStack images..."
    docker images | grep localstack | awk '{print $3}' | xargs -r docker rmi -f || echo "   No LocalStack images found"
fi

# Clean up any dangling volumes
echo "🧽 Cleaning up dangling volumes..."
docker volume prune -f

echo ""
echo "✅ LocalStack cleanup complete!"
echo ""
echo "To restart LocalStack, run:"
echo "   ./localstack/setup-localstack.sh"
