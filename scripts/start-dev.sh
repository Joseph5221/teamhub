#!/bin/bash

echo "Starting TeamHub Development Environment..."

# Start API
echo "Starting API..."
cd TeamHub.Server
dotnet run &
API_PID=$!
cd ..

# Wait for API to be ready
echo "Waiting for API to start..."
sleep 5

# Start Frontend
echo "Starting Frontend..."
cd teamhub-frontend  # or your frontend directory
npm run dev &
FRONTEND_PID=$!
cd ..

echo "TeamHub is running!"
echo "API: https://localhost:5001"
echo "Frontend: http://localhost:3000"
echo ""
echo "Press Ctrl+C to stop all services"

# Wait for user to stop
wait $API_PID $FRONTEND_PID