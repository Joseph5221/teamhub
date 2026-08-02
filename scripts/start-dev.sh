#!/bin/bash
# Starts the API and Blazor frontend together for local dev.
# Run scripts/setup-dev.sh first if you haven't (JWT secrets, runtime check).
#
# If your default 'dotnet' can't run net8.0 apps (see setup-dev.sh output),
# override which dotnet executable to use for `run`:
#   DOTNET=/path/to/dotnet8/dotnet scripts/start-dev.sh
set -e

DOTNET="${DOTNET:-dotnet}"

cd "$(dirname "$0")/.."

echo "Starting TeamHub Development Environment..."

echo "Starting API..."
(cd server/TeamHub.Server && "$DOTNET" run) &
API_PID=$!

echo "Waiting for API to start..."
sleep 5

echo "Starting Frontend..."
(cd frontend/BlazorApp && "$DOTNET" run) &
FRONTEND_PID=$!

echo "TeamHub is running!"
echo "API: https://localhost:7073 (Swagger at /swagger), http fallback http://localhost:5069"
echo "Frontend: https://localhost:7069, http fallback http://localhost:5032"
echo ""
echo "Press Ctrl+C to stop all services"

trap 'kill $API_PID $FRONTEND_PID 2>/dev/null' EXIT
wait $API_PID $FRONTEND_PID
