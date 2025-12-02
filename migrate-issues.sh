#!/bin/bash

# Script to help migrate SQS-related issues from azure-function-extensions-net
# to azure-functions-sqs-extension repository

# Source repository
SOURCE_REPO="laveeshb/azure-function-extensions-net"
TARGET_REPO="laveeshb/azure-functions-sqs-extension"

echo "📋 SQS Issues Migration Guide"
echo "================================"
echo ""
echo "Found the following SQS-related issues in $SOURCE_REPO:"
echo ""

# List SQS issues from source repo
gh issue list --repo "$SOURCE_REPO" --label "sqs" --state all --limit 100 --json number,title,state,url | \
  jq -r '.[] | "  #\(.number) [\(.state)] \(.title)\n    → \(.url)"'

echo ""
echo "================================"
echo ""
echo "Options for migration:"
echo ""
echo "1. TRANSFER (Recommended for open issues):"
echo "   - Preserves issue numbers, comments, reactions"
echo "   - Requires repository admin access"
echo "   - Command: gh issue transfer <issue-number> $TARGET_REPO --repo $SOURCE_REPO"
echo ""
echo "2. REFERENCE (For closed issues):"
echo "   - Add a comment to closed issues pointing to the new repository"
echo "   - Command: gh issue comment <issue-number> --repo $SOURCE_REPO --body 'This extension has moved to $TARGET_REPO'"
echo ""
echo "3. RECREATE (Manual):"
echo "   - Create new issues in target repo with reference to original"
echo "   - Useful for historical reference"
echo ""
echo "To transfer open SQS issues (if you have admin access):"
echo ""

gh issue list --repo "$SOURCE_REPO" --label "sqs" --state open --limit 100 --json number | \
  jq -r '.[] | "  gh issue transfer \(.number) '"$TARGET_REPO"' --repo '"$SOURCE_REPO"

echo ""
echo "To add migration notices to closed issues:"
echo ""

gh issue list --repo "$SOURCE_REPO" --label "sqs" --state closed --limit 100 --json number | \
  jq -r '.[] | "  gh issue comment \(.number) --repo '"$SOURCE_REPO"' --body \"📦 The SQS extension has been moved to a dedicated repository: https://github.com/'"$TARGET_REPO"'\""'
