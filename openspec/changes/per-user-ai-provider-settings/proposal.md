# Proposal

## Why

The current tailoring integration uses a server-owned credential and model for every user. A hosted Resumaire needs account-owned provider connections so users pay their own AI provider, discover suitable models, and keep their credentials private.

## What Changes

- Add Settings → AI with OpenAI and OpenCode Zen connections, discovery, selected-model testing, encrypted saving, refresh, replacement, and deletion.
- **BREAKING**: AI tailoring requires the authenticated user's saved key and selected model; global developer credentials are never a fallback.
- Add request-scoped provider credentials, durable limits and concurrency leases, and sanitized usage records.
- Minimize resume data sent to providers and disable response storage where supported.
- Prepare a container deployment with frontend routing, PostgreSQL, Typst, trusted proxies, and protected persistent Data Protection keys.

## Capabilities

### New Capabilities
- `ai-provider-settings`: Account-scoped encrypted provider settings, automatic compatible model discovery, and explicit connection testing.
- `production-deployment`: Reproducible hosting configuration and protected persistent credentials.

### Modified Capabilities
- `resume-tailoring`: Personal credentials, durable request limits, concurrency protection, and minimized provider payloads.

## Impact

ASP.NET configuration, EF Core entities/migration, provider HTTP client and tailoring endpoints; React API/types/navigation/settings and error handling; backend/frontend tests; Docker deployment and README. Existing review guardrails, manual editing, Identity bearer sessions, and resume export behavior remain in place. No public deployment or real provider calls are performed as part of automated tests.
