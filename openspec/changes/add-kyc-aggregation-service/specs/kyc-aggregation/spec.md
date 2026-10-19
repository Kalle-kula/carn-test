# Spec Delta

## Purpose

Provides a single secured endpoint that aggregates a customer's scattered KYC
data from the upstream Customer Data API into one validated record, caches it
locally, and protects the personally identifiable information it handles.

## ADDED Requirements

### Requirement: Aggregated KYC retrieval
The system SHALL expose `GET /kyc-data/{ssn}` that returns a single
`AggregatedKycData` record for the given SSN, combining the customer's personal
details, contact details, and current KYC form into one response.

#### Scenario: Customer with complete data
- **WHEN** an authenticated client requests `/kyc-data/{ssn}` for an SSN whose
  upstream data contains all required fields
- **THEN** the system responds `200` with `ssn`, `first_name`, `last_name`,
  `address`, `tax_country`, and the optional `phone_number`, `email`, and
  `income` fields populated from the upstream sources

### Requirement: Upstream field mapping and normalization
The system SHALL map the three upstream sources into the aggregated model,
tolerating their shape differences: a pre-formatted address string, case-variant
KYC form keys, and string-encoded numeric values.

#### Scenario: Preferred contact values selected
- **WHEN** contact details contain multiple emails or phone numbers
- **THEN** the system selects the entry marked preferred, falling back to the
  first entry when none is marked preferred

#### Scenario: Income parsed from string
- **WHEN** the KYC form provides `annual_income` as a numeric string
- **THEN** the system returns `income` as an integer

#### Scenario: KYC form keys matched case-insensitively
- **WHEN** a KYC form item uses variant key casing (e.g. `Key`/`Value`)
- **THEN** the system still resolves the corresponding field

### Requirement: Required-field validation flags incomplete KYC
The system SHALL validate that every required field (`ssn`, `first_name`,
`last_name`, `address`, `tax_country`) is present after aggregation, and MUST
treat a missing required field as a serious, logged failure rather than
returning partial data or a fabricated value.

#### Scenario: Missing required field
- **WHEN** the aggregated record lacks a required field such as `tax_country`
- **THEN** the system responds `422`, does not return the partial record, and
  logs the failure at a serious level identifying the missing field

#### Scenario: Missing optional field is allowed
- **WHEN** an optional field such as `income` is absent upstream
- **THEN** the system responds `200` with that field null and all required
  fields present

### Requirement: SSN input validation
The system SHALL validate the SSN format before making any upstream call and
reject malformed input.

#### Scenario: Malformed SSN rejected early
- **WHEN** a client requests `/kyc-data/{ssn}` with an SSN that does not match
  the expected format
- **THEN** the system responds `400` with an invalid-SSN error and makes no
  upstream request

### Requirement: Customer not found
The system SHALL report when no customer exists for a validly formatted SSN.

#### Scenario: Unknown customer
- **WHEN** the upstream API has no data for a validly formatted SSN
- **THEN** the system responds `404` with a customer-not-found error

### Requirement: Non-leaking error handling
The system SHALL return clear, user-appropriate error responses without exposing
internal details, and SHALL log system-level errors to a suitable logging
mechanism.

#### Scenario: Unexpected failure
- **WHEN** an unexpected or upstream/network error occurs while processing a
  request
- **THEN** the system responds `500` with a generic error message and records
  the underlying error in the logs, without returning stack traces or internal
  detail to the client

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

### Requirement: Endpoint authentication
The system SHALL require a valid API key to access the KYC endpoint and reject
unauthenticated requests.

#### Scenario: Missing or invalid API key
- **WHEN** a client calls `/kyc-data/{ssn}` without a valid API key
- **THEN** the system rejects the request as unauthorized and returns no KYC data

### Requirement: Enumeration protection
The system SHALL rate-limit requests to the KYC endpoint to impede bulk SSN
enumeration.

#### Scenario: Excessive requests throttled
- **WHEN** a client exceeds the configured request rate
- **THEN** the system throttles further requests rather than serving them

### Requirement: PII protection at rest
The system SHALL protect persisted personally identifiable information so that it
is not stored in plaintext.

#### Scenario: Stored PII is encrypted
- **WHEN** aggregated KYC data is persisted to the local store
- **THEN** the sensitive personal fields are encrypted at rest

### Requirement: PII-safe logging and access audit
The system SHALL avoid writing SSNs and other PII in plaintext to logs, and SHALL
record an access audit entry for each KYC lookup without including the PII itself.

#### Scenario: Access logged without PII
- **WHEN** a KYC lookup is processed
- **THEN** an audit entry records that a lookup occurred without exposing the SSN
  or other PII in plaintext
