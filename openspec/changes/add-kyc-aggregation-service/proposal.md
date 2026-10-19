# Proposal

## Why

Compliance workflows need a single, trustworthy view of a customer's KYC data, but
that data is scattered across three separate upstream endpoints (personal details,
contact details, and the point-in-time KYC form) with inconsistent shapes and gaps.
We need one endpoint that aggregates and validates this data, serves it securely,
and avoids hammering the upstream API on every request.

## What Changes

- Add a backend service exposing `GET /kyc-data/{ssn}` that returns a single
  `AggregatedKycData` record built from three upstream Customer Data API calls.
- Normalize the three upstream shapes into one model (camelCase upstream, mixed
  key casing in the KYC form, pre-formatted address, string income).
- Validate the required KYC field set and **fail as a serious, logged condition**
  when any required field (e.g. `tax_country`) is missing rather than fabricating
  or silently omitting it.
- Persist aggregated results in a local store as a read-through cache: fetch and
  persist on first request, serve from the store on subsequent requests, surviving
  application restarts.
- Secure the endpoint and its PII: API-key authentication, encryption at rest for
  persisted PII, rate limiting against SSN enumeration, strict SSN input
  validation before any upstream call, PII-safe logging with an access audit, and
  non-leaking error responses.
- Define a client error taxonomy: `400` malformed SSN, `404` customer not found,
  `422` incomplete KYC (serious), `500` unexpected.

## Capabilities

### New Capabilities
- `kyc-aggregation`: Aggregating a customer's KYC data from the upstream Customer
  Data API into one validated record, persisting it as a read-through cache, and
  serving it over a secured, PII-aware HTTP endpoint.

### Modified Capabilities
<!-- None: greenfield project, no existing specs. -->

## Impact

- **New service**: one .NET solution with a Web API project and a unit-test
  project (map-and-validate module under test).
- **New dependency (upstream)**: Customer Data API
  (`/personal-details/{ssn}`, `/contact-details/{ssn}`, `/kyc-form/{ssn}/{asOfDate}`).
- **New dependencies (libraries)**: an ORM/persistence provider (EF Core + SQLite)
  and ASP.NET Core security middleware (auth, rate limiting, data protection).
- **Data at rest**: a local persistent store holding customer PII (encrypted).
- **Operational config**: upstream base URL, API key(s), and encryption keys
  supplied via configuration/secrets, not hardcoded.
