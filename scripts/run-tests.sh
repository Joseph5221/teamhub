#!/bin/bash
# Runs both test projects via the root solution.
# Override the dotnet executable if your default one can't run net8.0 (see
# scripts/setup-dev.sh): DOTNET=/path/to/dotnet8/dotnet scripts/run-tests.sh
set -e

DOTNET="${DOTNET:-dotnet}"

cd "$(dirname "$0")/.."

echo "Running TeamHub Tests..."
"$DOTNET" test teamhub.sln --logger "console;verbosity=detailed"
