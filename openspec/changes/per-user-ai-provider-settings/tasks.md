# Tasks

## 1. Backend credentials and providers

- [x] 1.1 Add encrypted account settings, independent usage budgets/records and EF migration; verify database model and encryption isolation tests.
- [x] 1.2 Implement trusted provider registry, cached model discovery/fallback and synthetic tests with exact-selection validation receipts; verify fake-provider tests and document settings API in README.
- [x] 1.3 Add authenticated read/save/discover/test/delete settings endpoints; verify ownership, replacement/deletion, invalid receipts and safe responses through integration tests.

## 2. Tailoring integration

- [x] 2.1 Resolve personal credentials per request, remove global fallback, sanitize and bound provider inputs and parse token usage; verify generator/privacy and existing guardrail tests.
- [x] 2.2 Enforce atomic persistent quotas and concurrency leases for tests/tailoring, recording sanitized outcomes; verify quota persistence, credential removal and concurrent-request rejection.

## 3. Frontend settings

- [x] 3.1 Add API types/client, Settings navigation and connect/discover/select/test/save/refresh/replace/remove UI with privacy and billing disclosure; verify component tests, TypeScript build and lint.
- [x] 3.2 Add actionable tailoring credential errors with Settings links while preserving manual editing; verify tailoring regression tests.

## 4. Deployment

- [x] 4.1 Configure production certificate-protected persistent Data Protection keys and trusted forwarded headers; verify startup validation and credential recovery across provider restarts.
- [x] 4.2 Add Docker backend with Typst, nginx frontend routing, PostgreSQL/persistent volumes, migration/deployment/backup documentation and production API defaults; verify available container tooling and configuration, frontend build and backend publish.

## 5. Integration verification

- [x] 5.1 Run backend tests, frontend tests/build/lint and OpenSpec strict validation, review final diff for secret leakage and report any environment-limited deployment checks.
