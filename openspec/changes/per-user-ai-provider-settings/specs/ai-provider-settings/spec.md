# Spec Delta

## Purpose

Allow each signed-in user to connect their own AI provider, discover compatible models, and privately manage the credential used for resume tailoring.

## ADDED Requirements

### Requirement: Account-owned encrypted credentials
The system SHALL store at most one AI provider connection per account with an encrypted API key and selected model, SHALL derive ownership from authentication, and SHALL never return the full saved key.

#### Scenario: Save a tested connection
- **WHEN** a signed-in user saves a successfully tested provider/key/model combination
- **THEN** the system encrypts that user's key and returns only masked connection metadata

#### Scenario: Another account reads settings
- **WHEN** another user requests AI settings
- **THEN** only that user's own settings are returned

#### Scenario: Replace or remove a key
- **WHEN** the owner replaces or removes a credential
- **THEN** the previous credential cannot be used by future requests and its discovered-model cache is invalidated without resetting usage limits

### Requirement: Provider-specific model discovery
The system SHALL discover models using only the user-selected trusted provider, SHALL present compatible choices and a recommended economical model, and SHALL offer a fallback when discovery is forbidden or unsupported.

#### Scenario: Discover models
- **WHEN** the user connects a supported provider with a key
- **THEN** the backend fetches that provider's model catalog and identifies supported resume-tailoring models

#### Scenario: Discovery permission missing
- **WHEN** the provider refuses catalog access while generation may still be permitted
- **THEN** the system offers known-compatible choices and explains that availability must be tested

### Requirement: Explicit model testing and safe errors
The system SHALL test the selected model using synthetic structured-output input after an explicit user action, SHALL disclose possible charges and training behavior, and SHALL distinguish authentication, permissions, quota, timeout and unsupported-model failures without echoing secrets or provider response text.

#### Scenario: Successful exact-model test
- **WHEN** a synthetic test succeeds for the selected key and model
- **THEN** the user can save that combination and changing any part requires a new test

#### Scenario: No unapproved destinations
- **WHEN** a caller supplies an unknown provider or model
- **THEN** the server rejects it before transmitting the credential
