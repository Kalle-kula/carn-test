# Tasks

## 1. Solution and project scaffolding

- [x] 1.1 Initialize a git repository at the solution root and verify `git status` runs and an initial commit can be made
- [x] 1.2 Create one solution with a Web API project and an xUnit test project; verify `dotnet build` and `dotnet test` succeed on the empty skeleton
- [x] 1.3 Add dependencies (EF Core + SQLite provider, ASP.NET Core rate limiting, Data Protection) and verify the solution restores and builds

## 2. Upstream Customer Data API client

- [x] 2.1 Add typed DTOs for the three upstream responses (personal-details, contact-details, kyc-form items) using the observed camelCase/root-level shapes; verify a deserialization unit test maps the documented sample JSON
- [x] 2.2 Implement a typed `HttpClient` that calls `/personal-details/{ssn}`, `/contact-details/{ssn}`, and `/kyc-form/{ssn}/{today}`, with base URL from configuration; verify it returns data for a known test SSN and surfaces upstream 404 distinctly
- [x] 2.3 Add a unit test asserting an upstream 404 is translated to a not-found result rather than an exception

## 3. Mapping and validation module (unit-tested core)

- [x] 3.1 Implement the pure map function from the three upstream DTOs to `AggregatedKycData` (address from personal details, preferred-else-first email/phone, case-insensitive KYC keys, `annual_income` string→int); verify with unit tests over the five test SSNs' shapes
- [x] 3.2 Implement required-field validation over the full required set (`ssn, first_name, last_name, address, tax_country`) returning a distinct "incomplete" result; verify unit tests for a complete record (passes) and a missing `tax_country` record (flags incomplete)
- [x] 3.3 Verify a unit test confirms a missing optional field (e.g. `income`) still yields a valid complete result with that field null

## 4. Persistence (read-through cache)

- [x] 4.1 Add an EF Core `DbContext` + entity for aggregated records keyed by SSN against a file-based SQLite database; verify a migration/`EnsureCreated` produces the schema and a row round-trips
- [x] 4.2 Apply app-level field encryption (Data Protection) to sensitive PII fields before persistence; verify a test shows the stored column is not plaintext and decrypts back to the original value
- [x] 4.3 Wire the read-through flow: on cache miss fetch+map+validate+persist, on hit serve from the store; verify a test (and manual restart) shows the second request for an SSN does not call the upstream client

## 5. Endpoint and error handling

- [x] 5.1 Implement `GET /kyc-data/{ssn}` returning `AggregatedKycData` and wire it to the read-through flow; verify `200` with the expected body for a complete test SSN
- [x] 5.2 Add SSN format validation rejecting malformed input before any upstream call; verify a malformed SSN returns `400` and makes no upstream request
- [x] 5.3 Add centralized exception-handling middleware implementing the one-place error taxonomy — `404` not found, `422` incomplete KYC (logged at serious level with the missing field), `500` generic non-leaking — ; verify tests for the `404`, `422`, and `500` paths and that no stack trace reaches the client

## 6. Security hardening

- [x] 6.1 Add API-key authentication middleware protecting the endpoint; verify a request without a valid key is rejected as unauthorized and a valid key succeeds
- [x] 6.2 Enable HTTPS redirection + HSTS and the ASP.NET Core rate limiter on the endpoint; verify exceeding the configured rate is throttled
- [x] 6.3 Add PII-safe structured logging (SSN/PII masked) and a per-lookup access-audit entry without PII; verify a test/log inspection shows no plaintext SSN and an audit entry is produced
- [x] 6.4 Move upstream base URL, API key(s), and encryption keys to configuration/secrets; verify no secrets are hardcoded and the app reads them from configuration

## 7. Integration verification

- [x] 7.1 Run the full flow end-to-end against all five test SSNs; verify complete ones return `200` and `19800115-1234` (missing `tax_country`) returns `422` with a serious log entry
- [x] 7.2 Verify persistence across restart: request an SSN, restart the app, request again, and confirm it is served from the store without an upstream call
