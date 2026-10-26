# KYC Aggregation Service

Aggregates a customer's personal details, contact details and KYC form from the
Customer Data API into one record, served at `GET /kyc-data/{ssn}`.

## Prerequisites

- [.NET SDK 9](https://dotnet.microsoft.com/download) (version pinned in `global.json`)
- Node.js (optional, only needed for the web client)

## Run

```bash
dotnet run --project src/KycAggregation.Api
```

The API listens on http://localhost:5034.

## Tests

```bash
dotnet test
```

## Try it

Every request needs the `X-Api-Key` header. In development the key is
`dev-only-api-key-override-in-secrets`.

```bash
curl -H "X-Api-Key: dev-only-api-key-override-in-secrets" \
  http://localhost:5034/kyc-data/19900220-5678
```

The Customer Data API has data for these SSNs only; any other valid SSN returns 404.

| SSN             | Response                                                               |
| --------------- | ---------------------------------------------------------------------- |
| `19900220-5678` | 200                                                                    |
| `19751230-9101` | 200                                                                    |
| `19850505-4321` | 200                                                                    |
| `19951212-3456` | 200 (`phone_number`, `email` and `income` are null; they are optional) |
| `19800115-1234` | 422 (upstream has no `tax_country`, which is required)                 |

## Web client

Open http://localhost:5034/ for a small React client where you can enter the API
key and look up several SSNs at once. It is built automatically by `dotnet build`
or `dotnet run` when Node.js is installed. Without Node.js the API works the
same, and `/` returns 404.

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
- **KYC form date:** the form is requested as of today's date, which returns the
  latest version.
- **Known limitations:** stored records are never refreshed, so they can go out
  of date. Incomplete records (422) are not stored, so those requests always go
  to the Customer Data API.
- **Tests:** the mapping module (`KycMapperTests`) is the module covered by
  focused unit tests. Other tests cover the endpoint, persistence across
  restarts, security, the upstream client and the web client hosting.
- **Web client:** optional. The API is the product; the client is a convenience
  for trying it in a browser.
