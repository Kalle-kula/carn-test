# Spec Delta

## MODIFIED Requirements

### Requirement: Read-through persistence

The system SHALL persist aggregated KYC data on first retrieval and serve
subsequent requests for the same SSN from the local store, including after an
application restart, without re-fetching from the upstream API.

#### Scenario: First request populates the store

- **WHEN** an SSN is requested that is not yet in the local store
- **THEN** the system fetches from the upstream API, persists the aggregated
  record, and returns it

#### Scenario: Subsequent request served from the store

- **WHEN** a previously requested SSN is requested again, including after the
  application has restarted
- **THEN** the system returns the stored record without calling the upstream API

#### Scenario: Upstream data changes after the record is stored

- **WHEN** the upstream data for a stored SSN changes after it was first fetched
- **THEN** the system keeps returning the stored record, and the upstream change
  is not reflected
