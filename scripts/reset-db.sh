#!/bin/bash
# TeamHub currently uses EF Core's in-memory database provider (see
# docs/adr/0002-in-memory-database-for-now.md) — there's no persistent store
# and no migrations to run yet. The "database" resets itself every time the
# API process restarts.
#
# This script is a placeholder for when a real (Postgres) database is wired
# up: it'll run `dotnet ef migrations add` / `dotnet ef database update`
# against server/TeamHub.Server at that point.

echo "Database is in-memory — nothing to reset. Just restart the API."
