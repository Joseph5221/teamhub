#!/bin/bash

echo "Resetting database..."

cd TeamHub.Server

# Remove existing migrations (optional)
# rm -rf Migrations/

# Create fresh migration
dotnet ef migrations add InitialCreate --force

# Apply migrations
dotnet ef database update

echo "Database reset complete"