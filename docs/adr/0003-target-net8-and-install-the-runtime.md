# 0003. Keep targeting net8.0; install the .NET 8 runtime rather than retargeting

Date: 2026-08-01

## Status

Accepted

## Context

All four projects (`BlazorApp`, `TeamHub.Server`, and both test projects)
target `net8.0`. Dev machines have had only the .NET 10 SDK/runtime
installed (`dotnet --list-sdks` → `10.0.101` only). `dotnet build` succeeds
regardless (the SDK can compile against older target framework reference
assemblies), but `dotnet run` and `dotnet test` failed with a framework-
version-mismatch error, because there was no matching `net8.0` shared
runtime to actually execute against. This had never been fixed on any
machine this project had been touched on, which meant nobody had actually
exercised `dotnet run` end-to-end before — see the bug this uncovered in
[0002](0002-in-memory-database-for-now.md).

Two ways to close the gap: retarget every project to `net10.0` (whatever
happens to be installed), or install the missing `net8.0` runtime and keep
the target framework as-is.

## Decision

Install the .NET 8 runtime; keep `TargetFramework` at `net8.0` everywhere.

On macOS with Homebrew, `brew install dotnet@8` is keg-only and, on a
machine where a non-Homebrew `dotnet` (e.g. the official installer's
`/usr/local/share/dotnet`) already sits earlier on `PATH`, does **not**
automatically become the default `dotnet` — and merging its shared
framework files into the primary install's `shared/` directory requires
`sudo`, which isn't always available non-interactively. The workaround:
`dotnet@8`'s Homebrew keg is a fully self-contained install (own SDK +
runtime), so its own `dotnet` binary
(`$(brew --prefix dotnet@8)/libexec/dotnet`) can be invoked directly for
`run`/`test`, while the existing default `dotnet` keeps handling `build`
(which doesn't need a matching runtime). `scripts/setup-dev.sh` automates
detecting and using this; `scripts/start-dev.sh` and `scripts/run-tests.sh`
accept a `DOTNET=` override for the same reason.

## Consequences

- **Easier**: no risk of subtle net8.0 → net10.0 behavior differences (API
  removals, analyzer changes, package version bumps) being introduced as a
  side effect of just trying to get `dotnet run` working. The whole
  team/CI stays on one well-understood target.
- **Harder**: local setup has one more moving part (install a second .NET
  version) than retargeting would have, and the Homebrew-keg-only quirk
  above means the "right" `dotnet` executable to use for `run`/`test` isn't
  always just `dotnet` on `PATH` — `scripts/setup-dev.sh` exists specifically
  to paper over that so this doesn't have to be rediscovered per machine.
- **Revisit when**: there's a real reason to move off net8.0 (EOL
  approaching, a needed feature/package only available on a newer TFM) —
  at that point retarget deliberately, with the version bump tested across
  all five projects at once, not as an incidental fix for a broken `dotnet
  run`.
