# Spec Delta

## Purpose

Provide a reproducible deployment of Resumaire with persistent account data, protected credentials, public routing, and PDF export dependencies.

## ADDED Requirements

### Requirement: Protected persistent credential storage
Production SHALL require persistent encryption keys protected at rest, SHALL preserve readable credentials after redeployment, and SHALL require explicitly trusted proxy configuration.

#### Scenario: Missing encryption configuration
- **WHEN** production starts without protected persistent encryption-key storage
- **THEN** startup fails with an actionable configuration error

#### Scenario: Redeploy application
- **WHEN** the application restarts with its persistent database, key ring and protecting certificate
- **THEN** existing credentials remain usable

### Requirement: Reproducible application deployment
The project SHALL provide frontend/API routing, database migration instructions, persistent database backups guidance, and a backend runtime containing its PDF renderer.

#### Scenario: Refresh a client route
- **WHEN** a visitor refreshes a frontend route on the configured deployment
- **THEN** the frontend loads and its API calls use the deployed backend
