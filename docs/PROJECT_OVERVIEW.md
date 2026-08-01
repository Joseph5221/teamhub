# TeamHub — Project Overview

> Condensed from the original "TeamHub Project Proposal" document. See
> [docs/adr/](adr/) for architecture decisions and [ARCHITECTURE.md](ARCHITECTURE.md)
> for the technical design.

## Summary

TeamHub is a modular, team-focused dashboard that unifies key tools and data
into a single, customizable web interface. It's meant to let developers,
DevOps engineers, managers, and directors each see the metrics, tasks, and
team progress relevant to their role in one place.

It also doubles as a hands-on learning project for building scalable,
distributed-style systems in C#/.NET — modular architecture, third-party API
integration, and modern frontend development with Blazor.

## Core Features (planned)

- **Team Management** — users select which team(s) they belong to and get
  team-specific dashboards.
- **Jira Integration** — sprint progress, burndown charts, blockers.
- **Calendar Integration** — sync Google Calendar/Outlook for upcoming events
  and demos.
- **GitHub Integration** — open PRs, issues, repo activity.
- **CI/CD Monitoring** — pipeline status and deployment issues (GitHub
  Actions, Jenkins, etc.).
- **InfraWatch** — infrastructure health, resource utilization, uptime for
  DevOps visibility.
- **Slack Integration (planned)** — notify teams about sprint milestones,
  deployments, blockers.

## Target Users

- Software teams wanting unified visibility across dev, ops, and management.
- DevOps engineers wanting a lightweight infra dashboard.
- Managers who want a quick read on progress and blockers.

## Future Vision / Go-To-Market

Currently a learning/portfolio project. If it goes further, the intended
positioning is a customizable, modular team dashboard offering visibility
across dev/ops/management at a fraction of enterprise platform cost, launched
open-source-first (free hosted demo, build-in-public blog posts, GitHub/Reddit/
Product Hunt) with long-term monetization via premium integrations, managed
hosting, and a module marketplace. None of this affects near-term engineering
decisions — it's here for context, not as active requirements.

## Roadmap (original, high-level)

1. Core architecture (gateway/API + user + team).
2. Blazor frontend + authentication flow.
3. Jira and Google Calendar integrations.
4. InfraWatch service for CI/CD and system monitoring.
5. GitHub and Slack integrations.
6. User customization of module layout/visibility.

See [ROADMAP.md](ROADMAP.md) for the current, code-grounded status and
concrete next steps — this list is the original aspirational order, not a
tracked backlog.
