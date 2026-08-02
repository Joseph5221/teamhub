#!/bin/bash
# Resets and reseeds the dev database with test data.
#
# The database is in-memory (see docs/adr/0002-in-memory-database-for-now.md)
# and only exists inside the running API process — this script talks to the
# already-running dev server's /api/dev/reseed endpoint rather than touching
# a database file directly. Start the API first (scripts/start-dev.sh), and
# note the API already seeds itself once automatically on startup; this is
# for getting back to a clean slate without restarting the process.
set -e

API="${API_URL:-https://localhost:7073}"

echo "Reseeding dev database at $API ..."
curl -sk -X POST "$API/api/dev/reseed"
echo
echo "Done. Test users (any password): test@teamhub.com, bob@teamhub.com, carol@teamhub.com"
