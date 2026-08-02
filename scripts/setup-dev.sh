#!/bin/bash
# One-time (and safe-to-rerun) local dev setup for TeamHub.
# See server/TeamHub.Server/README.md and docs/ROADMAP.md for background.
set -e

cd "$(dirname "$0")/.."

echo "== TeamHub dev environment setup =="

# 1. Make sure a .NET 8 ASP.NET Core runtime is reachable — the solution
#    targets net8.0 (see docs/adr/0002-target-net8-and-install-the-runtime.md).
#    `dotnet build` works with any newer SDK, but `dotnet run`/`dotnet test`
#    need an actual net8.0 runtime present.
DOTNET_RUN_CMD="dotnet"
if ! dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App 8\."; then
  echo "No .NET 8 ASP.NET Core runtime found on 'dotnet'."
  if command -v brew >/dev/null 2>&1; then
    if ! brew list dotnet@8 >/dev/null 2>&1; then
      echo "Installing via Homebrew (brew install dotnet@8)..."
      brew install dotnet@8
    fi
    DOTNET8_KEG="$(brew --prefix dotnet@8 2>/dev/null)/libexec/dotnet"
    if [ -x "$DOTNET8_KEG" ]; then
      # dotnet@8 is keg-only and, on machines that already have a non-brew
      # 'dotnet' earlier on PATH, won't get symlinked/merged automatically
      # (merging it into the primary install requires sudo). Simplest fix:
      # use its own standalone dotnet executable directly for run/test.
      echo "Using Homebrew-installed .NET 8 at: $DOTNET8_KEG"
      echo "(build still uses your default 'dotnet'; only run/test need this one)"
      DOTNET_RUN_CMD="$DOTNET8_KEG"
    fi
  else
    echo "Homebrew not found. Install the .NET 8 SDK manually:"
    echo "  https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
  fi
fi

# 2. Dev JWT secrets (required — the app throws at startup without them).
pushd server/TeamHub.Server >/dev/null
if ! dotnet user-secrets list 2>/dev/null | grep -q "^Jwt:Secret"; then
  echo "Setting dev JWT user-secrets..."
  dotnet user-secrets init
  dotnet user-secrets set "Jwt:Secret" "dev-only-$(openssl rand -hex 32)"
  dotnet user-secrets set "Jwt:Issuer" "TeamHub-Dev"
  dotnet user-secrets set "Jwt:Audience" "TeamHub-Dev"
else
  echo "JWT user-secrets already set, leaving them alone."
fi
popd >/dev/null

# 3. Build everything.
echo "Building teamhub.sln..."
dotnet build teamhub.sln

cat <<EOF

Setup complete.

Run the API:
  cd server/TeamHub.Server && $DOTNET_RUN_CMD run

Run the frontend (separate terminal):
  dotnet run --project frontend/BlazorApp

Run the tests:
  $DOTNET_RUN_CMD test teamhub.sln

Or just: scripts/start-dev.sh
EOF
