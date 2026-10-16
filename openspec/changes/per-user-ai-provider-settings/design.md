# Design

## Context

See proposal.md for motivation. React uses Identity bearer tokens and ASP.NET Minimal APIs. EF Core uses PostgreSQL in development and SQLite in integration tests. The existing generator consumes the Responses protocol and strict JSON schema; its guardrails and suggestion acceptance stay intact.

## Goals / Non-Goals

**Goals:** request-scoped credentials, account isolation, dynamic discovery with known-compatible model metadata, encrypted persistent credentials, atomic persistent quotas and active-request leases, deployment reproducibility.

**Non-Goals:** app-funded AI, arbitrary endpoints, multiple saved connections per account, new authentication protocol, actual public deployment.

## Decisions

- Use a server-owned provider registry for OpenAI and OpenCode Zen. Discover their catalogs through `/models`; use curated known-compatible fallback entries when discovery is forbidden or unsupported. Restrict selectable models to Responses/JSON-compatible entries; attach cost-category recommendation and training disclosure from maintained metadata. Unknown catalog models remain unavailable until supported, rather than inferring compatibility from their names alone.
- Keep draft keys only in the form and authenticated POST bodies during discovery/test. A short-lived protected validation receipt binds user, provider, model, and key fingerprint; saving requires a successful synthetic structured-output test for that exact selection. Existing saved credentials can be reused without returning plaintext.
- Add UserAiSettings with a protected credential and version. Use purpose-scoped Data Protection including user identity. Production requires persistent key storage plus certificate encryption; deployment mounts the certificate as a secret. Stable application name and key backups preserve decryption across restarts.
- Pass credentials explicitly to the generator rather than mutating global options or HttpClient default headers. Disable redirects on provider clients and suppress default HTTP logging. Use trusted HTTPS URLs exclusively.
- Persist independent AiUsageBudget rows, using a conditional atomic update to reserve minute/day capacity and one active lease per user. Removing/replacing settings never resets limits. Both synthetic tests and tailoring consume quota. Leases expire after the provider timeout plus a buffer and release in finally; failed or cancelled provider attempts still count. No automatic paid retries.
- Store sanitized AiUsageRecord metadata (operation, provider/model, status, duration and optional token usage), never prompts or keys. Existing suggestion storage is unchanged.
- Remove contact fields, personal links, project URLs and credential identifiers from generation inputs; filter keyword evidence to professional-content paths. Bound serialized input size. Set `store:false` and omit temperature for model portability.
- Use same-origin `/api` production requests through nginx, .NET/Typst backend, PostgreSQL and persistent volumes. Production validates encryption settings, processes only explicitly trusted proxy headers, and uses HTTPS at the public reverse proxy.

## Risks / Trade-offs

- Model listing does not prove inference access → exact-model synthetic test; missing listing permissions use documented fallback without labelling the key invalid.
- Free contributor models train on submitted text → explicit provider/model disclosure and acknowledgement; no default selection of contributor models.
- Secret-store theft or loss → certificate-protected key ring, restricted persistent volume, separately backed-up certificate, no API plaintext retrieval.
- Provider cancellation can still incur charges → count attempts, disclose this, no automatic retries.
- Catalogs and model capabilities change → refresh, short account/version-specific caching, compatible fallback and clear unsupported states.

## Migration Plan

Apply the additive EF migration before deploying the application. Existing accounts remain usable for manual tailoring; AI displays a connect-provider action until personal settings exist. Do not import the old global key. Production configuration fails early without protected persistent key storage. Rollback requires restoring the previous app and keeping new tables until a deliberate data migration; it would restore the prior shared-key behavior and must not be used to bypass BYOK.
