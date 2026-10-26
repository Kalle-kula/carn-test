# Design

## Context

See proposal.md - Why. The `KycAggregation.Api` project exposes
`GET /kyc-data/{ssn}` guarded by a **global** `ApiKeyMiddleware` (every request
needs `X-Api-Key`), a fixed-window rate limiter, and SSN format validation. The
response is either an `AggregatedKycData` JSON object (snake_case) or an error
envelope `{ "error": "..." }` with status `400 / 404 / 422 / 429 / 500`, plus
`401` from the key gate. There is no CORS policy and no static-file serving today.

## Goals / Non-Goals

**Goals:**
- Serve a React SPA from the API origin so no CORS setup is needed.
- Keep the SPA assets public while `/kyc-data/*` stays key-protected.
- Let the user supply the API key at runtime (no key baked into the bundle).
- Look up a list of SSNs and render each result or error independently.

**Non-Goals:**
- No server-side proxy/backend-for-frontend to hide the API key (the key remains
  in the browser at runtime; acceptable for a local/demo client).
- No change to the KYC endpoint contract, aggregation logic, or persistence.
- No authentication beyond the existing API key (no user accounts/sessions).

## Decisions

### Decision: Serve the SPA from the API (same origin) instead of a separate dev server

Option B from exploration. The SPA build output is published into the API's
`wwwroot` and served by `UseStaticFiles` + a `MapFallbackToFile("index.html")`
SPA fallback. Same origin means the browser calls `/kyc-data/{ssn}` with no CORS
preflight and no `AddCors` policy to maintain.

- **Alternative - separate Vite dev server + CORS**: rejected as the default; it
  requires adding and maintaining a CORS policy and a dev proxy. Vite's dev server
  can still be used during development, proxying `/kyc-data` to the API, but the
  shipped artifact is the static build served by the API.

### Decision: Scope the API-key check to the data path so the SPA shell loads ungated

The key middleware is global and runs before endpoint execution, so a SPA
fallback registered as an endpoint would itself be gated - a deep link or the
root URL would return `401` instead of the client. To keep the SPA shell and its
static assets reachable without a key while `/kyc-data/*` stays protected, the
`ApiKeyMiddleware` only enforces the key for requests under `/kyc-data`:

```
app.UseStaticFiles();                 // real SPA assets (JS/CSS), no key required
app.UseRateLimiter();
app.UseApiKeyAuthentication(apiKeys); // enforces key only for /kyc-data/*
app.MapKycEndpoints();
app.MapFallbackToFile("index.html");  // serves the client shell, ungated
```

Only inert assets (HTML/JS/CSS) and the client shell are reachable without a key;
no PII endpoint is unprotected. Existing security tests all target `/kyc-data`
and continue to pass. `UseHsts`/`UseHttpsRedirection` remain production-only as
today.

- **Alternative - rely on middleware ordering alone (static + fallback before the
  gate)**: insufficient, because endpoints execute after middleware, so the
  fallback stays gated. A path-scoped check is the minimal correct fix.
- **Alternative - move auth onto the endpoint via a route group / endpoint
  filter**: larger refactor of existing, tested middleware; unnecessary here.

### Decision: API key entered in a UI field, sent per request

A text field holds the key in app state and is attached as `X-Api-Key` on each
lookup. Nothing is hardcoded in the bundle, so the artifact carries no credential.

- **Alternative - hardcode the dev key**: rejected; bakes a credential into the
  build and is easy to leak.

### Decision: List-of-SSNs UX, one request per SSN

The endpoint is one-SSN-per-call. The client keeps an editable list, and on
"look up all" issues one request per SSN, rendering each row independently so one
SSN's `400/404/422` does not hide another's success. Maps snake_case fields
straight to table columns.

### Decision: Vite + React tooling

Vite for a fast, standalone build producing static assets. The app lives in a new
source directory (e.g. `web/`) and its `build` output is copied/published into the
API's `wwwroot`.

## Risks / Trade-offs

- **API key visible in the browser** → Accepted for local/demo use; documented in
  the proposal. A server-side proxy is the path if the client is ever shared.
- **Static middleware placed after the key gate by mistake** → The SPA would fail
  to load (401 on assets). Mitigation: ordering is explicit in design and covered
  by the implementation tasks.
- **Build integration drift** (SPA build not published into `wwwroot`) → The API
  serves a stale or missing client. Mitigation: wire the frontend build into the
  project's build/publish step and document the manual build command.
- **Rate limiter + batch lookups** → Looking up a large list can trip the global
  `429`. Acceptable; rows surface the `429` state like any other error.

## Open Questions

- Exact source directory name (`web/` vs `clientapp/`) and whether the frontend
  build is wired into `dotnet build`/`publish` or run as a separate documented
  step - resolved during implementation without affecting the specs.
