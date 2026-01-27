#!/bin/bash
# test.sh - Run all tests

echo "Running TeamHub Tests..."

# Unit Tests
echo "Running Unit Tests..."
dotnet test TeamHub.Server.Tests --logger "console;verbosity=detailed"

# Integration Tests
echo "Running Integration Tests..."
dotnet test TeamHub.Server.IntegrationTests --logger "console;verbosity=detailed"

# Coverage Report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

echo "All tests complete!"