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
