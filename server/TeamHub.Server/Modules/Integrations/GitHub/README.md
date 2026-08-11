# GitHub Connector

**Status:** implemented and verified against the real GitHub REST API (not
just mocked in tests — see "Manual verification" below).

## What it does

`GitHubConnector : IModuleConnector` (`Type => IntegrationType.VersionControl`)
lists an organization's repositories via
`GET /orgs/{org}/repos` on the real GitHub REST API, normalized into
`ModuleDataItem`s (title = `full_name`, description, URL, last-updated,
metadata: language/stars/open issues/visibility).

## Auth

Per-team, not global: `Integration.ConfigurationData` (JSON, set via
`POST`/`PUT /api/teams/{teamId}/integrations`) holds:

```json
{ "organization": "octokit", "personalAccessToken": "optional" }
```

`personalAccessToken` is optional — GitHub allows unauthenticated reads of
public repos at a lower rate limit (60 req/hour/IP), which is enough for
dev/demo use. When present, it's sent as `Authorization: Bearer <token>` on
each request (not stored on the shared `HttpClient` — see
`GitHubApiClient.GetOrganizationRepositoriesAsync`, which builds the header
per-request precisely so one team's token can never leak onto another
team's call through a pooled/shared client instance).

`GetConfigAsync` reports `hasPersonalAccessToken` as a boolean only — the
token itself is never returned to a client.

## Files

- `IGitHubApiClient` / `GitHubApiClient` — thin HTTP wrapper, maps GitHub
  error responses (404 → `GitHub.OrganizationNotFound`, 401/403 →
  `GitHub.Unauthorized`, other non-success → `GitHub.ApiError`) to
  `ModuleConnectorException`. Kept separate from `GitHubConnector` so the
  HTTP/error-mapping logic is unit-testable independent of config-loading
  (see `GitHubApiClientTests`, which stub the `HttpMessageHandler` — no
  network calls in the test suite).
- `GitHubConnector` — loads a team's config from the `Integration` row
  (`AppDbContext`), calls `IGitHubApiClient`, and implements the `sync`
  action (re-fetches data to confirm the config still works, then sets
  `Integration.Status = Connected` and stamps `LastSyncedAt`). Any other
  action throws `GitHub.UnsupportedAction`.
- `GitHubDtos.cs` — `GitHubRepositoryDto`, mapped 1:1 to the subset of
  GitHub's repo JSON we use.
- `GitHubResiliencePolicies.cs` — Polly policies (see below).

## Resilience (Polly)

Per `docs/ARCHITECTURE.md` "Resilience & Testing", the `IGitHubApiClient`
typed `HttpClient` (registered in
`ServiceCollectionExtensions.AddFeatures`) is wrapped with:

- **Retry**: 3 attempts, exponential backoff (2s/4s/8s), on transient HTTP
  errors (5xx, request timeout) or a `429` rate-limit response.
- **Circuit breaker**: opens for 30s after 5 consecutive transient
  failures, so a struggling/rate-limited GitHub doesn't get hammered by
  every team's dashboard load.

## Sample data for frontend work

`Infrastructure/Data/DbInitializer.cs` seeds the "Growth" team's GitHub
integration already pointed at a real, small, public GitHub org
(`octokit` — GitHub's own API client libraries org) with no token, so
`GET /api/teams/{growthTeamId}/integrations/{integrationId}/data` returns
real repos (octokit.net, octokit.js, etc.) out of the box — no PAT needed
to start on the frontend widget.

## Manual verification

Built and ran the server locally (`dotnet run`, .NET 8 runtime per
[ADR 0003](../../../../docs/adr/0003-target-net8-and-install-the-runtime.md)),
logged in as a seeded user, created a team + a GitHub integration
(`{"organization":"octokit"}`), and confirmed against the live API:

- `GET .../data` returned real `octokit` org repos with correct
  title/description/URL/stars/language.
- `POST .../actions` with `{"action":"sync"}` returned `204` and flipped
  the integration to `Status: Connected` with a real `LastSyncedAt`.
- Requesting data for an unimplemented type (e.g. `ProjectManagement`/Jira)
  returned `501 Integration.NotSupported`, not a crash.

## Not done / left for later

- No GitHub App auth (PAT only) — fine for a single-team-configures-its-own-token
  model; revisit if TeamHub needs to act on a user's behalf across many orgs.
- No pagination beyond `per_page=100` (GitHub's max) — fine for demo/small
  orgs, would need `Link` header pagination for large orgs.
- Doesn't yet feed into `Modules/Projects` (still a stub) — see
  [ADR 0006](../../../../docs/adr/0006-projects-definition.md).
