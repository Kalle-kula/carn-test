# Reflection

## Decisions

- **Persistence:** EF Core with a SQLite file (`kyc.db`), used as a read-through
  cache. The first request for an SSN fetches from the Customer Data API and
  stores the result; later requests, including after a restart, are served from
  the database. SQLite was chosen over Redis because it needs no extra
  infrastructure.
- **Error handling:** every error response has the same shape,
  `{ "error": "..." }`, defined in one place. Users get short, clear messages.
  Unexpected exceptions are caught by a global handler, logged in full on the
  server, and returned as a generic 500 with no internal details. Upstream calls
  are retried on transient failures.
- **Status codes beyond the spec:** the spec lists 200, 404 and 500. The service
  also returns 400 for a malformed SSN (rejected before any upstream call), 401
  for a missing or wrong API key, 429 when rate limited, and 422 when a required
  field is missing upstream. 422 is used instead of returning partial or invented
  data; the missing field is logged as an error.
- **Security:** the spec has no authentication, but the endpoint returns personal
  data for any SSN, so an API key is required. Requests are rate limited to make
  SSN enumeration harder. Personal fields are encrypted in the database, the SSN
  is stored only as a keyed hash, and logs never contain SSNs.
- **Upstream differences:** the Customer Data API differs from its
  documentation. Its routes have no `/api` prefix, `personal-details` already
  returns a formatted address, and some KYC form items use `Key`/`Value` instead
  of `key`/`value`. The code follows what the API actually returns.
- **KYC form date:** the Customer Data API requires a date, so the form is
  requested as of today. The endpoint in the task takes no date, and the test
  data is the same for any date.
- **Known limitations:** stored records are never refreshed, so they can go out
  of date. Incomplete records (422) are not stored, so those requests always go
  to the Customer Data API.
  Never refreshing is accepted because the assignment requires serving from the
  local store, and the Customer Data API sends no `ETag` or `Last-Modified`
  header, so a change cannot be detected without fetching everything again.
- **Tests:** the mapping module (`KycMapperTests`) is the module covered by
  focused unit tests. Other tests cover the endpoint, persistence across
  restarts, security, the upstream client and the web client hosting.
- **Web client:** optional. The API is the product; the client is a convenience
  for trying it in a browser.

## Improve with more time

- Refresh stored records (expiry or invalidation) instead of keeping them
  forever.
- Support historical lookups: accept an as-of date instead of always using
  today.
- Rate limit per API key instead of one global limit shared by all clients.
- Stronger authentication than a single API key, and a small backend for the web
  client so the key is not handled in the browser.
- Encrypt the Data Protection key ring at rest (ASP.NET warns it is stored
  unencrypted) and remove the old key from git history.
- Publish an OpenAPI document.
