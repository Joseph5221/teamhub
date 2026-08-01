# TeamHub Documentation

This folder holds the durable planning and architecture documentation for TeamHub —
the things that are true independent of any single commit, as opposed to code
comments (true of one line) or commit messages (true of one change).

## Why a `docs/` folder, and why structured this way

This layout follows conventions common across modern OSS and industry .NET/web
projects (e.g. how the .NET runtime, Azure SDKs, and most Backstage/ADR-adopting
teams organize planning docs):

| Location | Purpose | Lifespan |
|---|---|---|
| `README.md` (repo root) | First thing a visitor/dev sees: what the project is, how to run it | Kept current always |
| `CLAUDE.md` (repo root) | Machine-readable-ish project brief for AI coding agents (Claude Code): build commands, conventions, current state | Kept current always |
| `docs/PROJECT_OVERVIEW.md` | Product vision, features, target users, GTM — the "why" | Changes rarely |
| `docs/ARCHITECTURE.md` | Technical architecture — the "how" | Changes when architecture changes |
| `docs/adr/` | Architecture Decision Records — one immutable file per significant decision | Append-only, never edited after acceptance |
| `docs/ROADMAP.md` | Current build status + next steps | Changes often — update at the start/end of each work session |

This mirrors the split recommended by the [C4 model](https://c4model.com) /
[arc42](https://arc42.org) documentation communities: separate the stable
"why/vision" docs from the volatile "status/next steps" docs, and use ADRs so
past decisions aren't lost or silently rewritten when someone changes their mind
later. Each module folder under `server/TeamHub.Server/` also gets its own
lightweight `README.md` for module-local notes — that convention (a README next
to the code it describes) is what most polyglot monorepos do instead of trying
to centralize everything in one giant doc.

## Architecture Decision Records (ADRs)

`docs/adr/` uses the lightweight format popularized by Michael Nygard
("Documenting Architecture Decisions", 2011) and now the de facto standard
(adopted by ThoughtWorks, AWS, Azure Architecture Center, etc.). Each ADR is a
short, numbered, immutable Markdown file: `NNNN-title-with-dashes.md`. When a
decision changes, you don't edit the old ADR — you write a new one that
supersedes it and mark the old one as superseded. This gives you a timeline of
*why* the architecture looks the way it does, which a single living document
can't preserve.

Use `docs/adr/template.md` to start a new one.

## Relationship to `server/TeamHub.Server/README.md`

That file is a **dev-quickstart doc for one project**: how to run the API,
test credentials, endpoint list, "add a new feature" recipe. It overlaps in
spirit with this `docs/` folder but at a narrower scope — this folder is the
whole-repo planning view (why, architecture, status across frontend +
backend), that README is the "how do I run this one thing today" view. When
priorities or status change, update both rather than letting one go stale;
`docs/ROADMAP.md` is the source of truth for *what's next*, that README is
the source of truth for *how to run what exists*.

## How these docs relate to the original planning PDFs

The two source documents used to bootstrap this (`TeamHub Project Proposal` and
`TeamHub High-Level Architecture`) have been folded into `PROJECT_OVERVIEW.md`
and `ARCHITECTURE.md` respectively, with one important addition: a **"Where the
code has diverged from this plan"** section in `ARCHITECTURE.md`, since the
actual repository structure today does not match the originally planned
`/Modules/<Domain>` nesting. Treat the PDFs as historical input, not as the
source of truth — these docs and the ADRs are now the source of truth going
forward.
