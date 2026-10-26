# Proposal

## Why

The KYC aggregation API (`GET /kyc-data/{ssn}`) can only be exercised today with
curl or a REST client, which makes it slow to demo and to eyeball results. A small
browser UI that looks up one or more SSNs and shows the aggregated record (or the
per-SSN error) turns the service into something anyone can try without crafting
HTTP requests by hand.

## What Changes

- Add a small Vite + React single-page app that lets the user enter an API key,
  build a list of SSNs, look them all up, and view the results in a table.
- Serve the built SPA as static files from the existing `KycAggregation.Api`
  project (same origin as the API), so no CORS configuration is required.
- Adjust the API middleware pipeline so SPA assets (`index.html`, JS, CSS) are
  served **without** an API key, while `/kyc-data/*` stays protected by the
  existing `ApiKeyMiddleware`.
- The SPA calls the existing endpoint once per SSN and renders either the
  `AggregatedKycData` fields or the `{ "error": "..." }` envelope's status
  (`400 / 404 / 422 / 429 / 401 / 500`) as that row's state. No API contract
  change.

## Capabilities

### New Capabilities
- `kyc-client-ui`: A browser client that collects an API key and a list of SSNs,
  looks each one up against the KYC aggregation endpoint, and presents the
  aggregated record or the per-SSN error outcome.

### Modified Capabilities
<!-- None: the API contract and kyc-aggregation behavior are unchanged. The
     middleware ordering change is an implementation detail covered in design,
     not a spec-level behavior change to the existing capability. -->

## Impact

- **New frontend**: a Vite + React app (new source directory, e.g. `web/` or
  `clientapp/`) producing a static build.
- **Modified backend wiring** (`src/KycAggregation.Api/Program.cs`): add
  `UseStaticFiles`, a SPA fallback to `index.html`, both placed before the API-key
  gate so the SPA loads while the data endpoint stays protected.
- **Build/output**: the SPA build output is published into the API's `wwwroot`.
- **New dev dependency**: Node.js tooling (Vite) for building the frontend;
  no new runtime dependency for the .NET service beyond static file serving.
- **Security note**: the API key is entered at runtime in the UI and sent from the
  browser on each request, so it is visible in browser devtools. Accepted for a
  local/demo client; a server-side proxy would be needed before sharing it.
