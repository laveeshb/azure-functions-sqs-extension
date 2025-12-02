@echo off
REM Script to open the Azure Functions SQS Extension workspace in VS Code (Windows)

set SCRIPT_DIR=%~dp0

code "%SCRIPT_DIR%dotnet"

echo Opening Azure Functions SQS Extension workspace in VS Code...
echo Projects: Extensions.SQS (src) and Extensions.SQS.Sample (test)
