# Spec Delta

## ADDED Requirements

### Requirement: Personal AI provider credentials
AI tailoring SHALL use only the authenticated user's saved provider credential and selected model; missing or unreadable credentials SHALL return an actionable settings error without falling back to a global key. Manual editing SHALL remain available.

#### Scenario: No personal key
- **WHEN** a user requests AI tailoring without saved credentials
- **THEN** the system directs them to AI settings without contacting a provider

#### Scenario: Concurrent accounts
- **WHEN** different accounts generate suggestions simultaneously
- **THEN** each request uses its owner's provider, key and model

### Requirement: Minimized provider payload and usage metadata
The system SHALL exclude unnecessary contact details, personal links and credential identifiers from AI inputs, SHALL bound request input size, and SHALL store sanitized usage metadata rather than logging prompts or credentials.

#### Scenario: Resume contains contact details
- **WHEN** tailoring sends a resume to a provider
- **THEN** structured contact fields and unrelated personal links are excluded

## MODIFIED Requirements

### Requirement: AI request limits and failure reporting
The system SHALL enforce persistent per-user minute/day limits for billable tailoring and model tests, SHALL prevent concurrent duplicate provider calls for the same user, and SHALL report provider failures with a specific safe reason. Limits SHALL survive restarts and credential replacement or removal.

#### Scenario: Limit reached
- **WHEN** a user exceeds the configured number of provider requests
- **THEN** the system responds with 429 and a retry delay

#### Scenario: Concurrent request
- **WHEN** a user starts another paid request while one is active
- **THEN** the second request is rejected before contacting the provider

#### Scenario: Provider failure
- **WHEN** the provider is unreachable, times out, rejects the key, or returns a truncated or unreadable reply
- **THEN** the system responds with a safe actionable reason without echoing resume content
