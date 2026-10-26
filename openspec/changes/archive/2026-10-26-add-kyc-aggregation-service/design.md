# Design

## Context

See proposal.md (Why) for motivation and specs/kyc-aggregation/spec.md for the
behavior contract. This is a greenfield .NET service. The upstream Customer Data
API was probed directly and differs from its documented spec: routes are
root-level (not `/api`), JSON is camelCase, `personal-details` already returns a
pre-formatted `address` string, KYC form keys appear with variant casing, and one
valid test SSN permanently lacks `tax_country`. The design is grounded in that
observed reality, not the documented spec. The overriding constraint is to keep
the codebase minimal and maintainable.

## Goals / Non-Goals

**Goals:**
- One cohesive aggregation path: validate input, fan out to three upstream calls,
  map, validate required fields, persist, serve.
- Read-through cache that survives restart with no external infrastructure.
- A single, isolated map-and-validate unit that is pure and easily unit-tested.
- Proportionate security for a PII/SSN endpoint, implemented as thin, idiomatic
  middleware rather than bespoke frameworks.

**Non-Goals:**
- No distributed cache, message bus, or multi-node concerns.
- No full identity provider; authentication is a single API-key check.
- No historical/point-in-time KYC queries — only the current (as-of-today) form.
- No layered Domain/Application/Infrastructure project ceremony.

## Decisions

- **Persistence: EF Core + SQLite, aggregate keyed by SSN.** A file-based SQLite
  database is self-contained, survives restart, and needs no running service.
  We persist the final `AggregatedKycData` (one row per SSN) as a read-through
  cache rather than raw per-source payloads — simplest thing that meets the
  requirement. *Alternative:* Redis — rejected, adds external infrastructure for
  no benefit at this scale.

- **Address from `personal-details.address`.** The upstream already returns the
  address in exactly the output format, so we use it directly. *Alternative:*
  flatten `contact-details.addresses[]` — rejected as re-doing work upstream
  already did and inventing a format.

- **`asOfDate` = today.** The aggregation endpoint takes only an SSN; we request
  the current KYC form using today's date to get the latest snapshot.

- **Error taxonomy: `400` / `404` / `422` / `500`.** `400` malformed SSN (caught
  before any upstream call), `404` customer not found, `422` incomplete KYC
  (missing required field — a serious, logged condition), `500` unexpected. The
  documented output spec lists only `200/404/500`; `400` and `422` are deliberate,
  standard-code extensions that tell the truth about each failure. The taxonomy
  lives in one place (an exception-handling middleware) so it is cheap to change.

- **Required-field validation as a general rule.** Validate the whole required
  set, not a `tax_country` special-case, so the rule is easy to read, test, and
  resistant to future gaps. `income` and other optional fields may be null.

- **Authentication: API key.** A single API-key check via middleware demonstrates
  access control with minimal code. *Alternatives:* JWT bearer (more realistic,
  more setup) or none (rejected — leaves a PII oracle open).

- **Encryption at rest: app-level field encryption.** Sensitive PII columns are
  encrypted in the application (ASP.NET Core Data Protection) before persistence,
  keeping the store portable and the protection explicit. *Alternatives:* SQLCipher
  / encrypted store (more infra) or plaintext-with-a-note (weaker).

- **Rate limiting** via the built-in ASP.NET Core rate limiter to impede SSN
  enumeration.

- **PII-safe logging + access audit.** Structured logging with SSN/PII masked; an
  access-audit entry per lookup that records the event without the PII. Generic
  client error bodies, full detail only in server logs.

- **Project layout: one solution, Web API project + xUnit test project.** The
  map-and-validate logic is the unit under test. No extra layering unless a second
  consumer ever appears.

## Risks / Trade-offs

- **Relying on observed-but-undocumented upstream behavior** (root routes,
  pre-formatted address) → grounded by direct probing of all five test SSNs;
  mapping is isolated so a shape change is a localized fix.
- **Cached PII can go stale** (read-through never refreshes) → acceptable for this
  task; a TTL or invalidation can be added later without changing the contract.
- **Security vs. minimalism tension** → mitigated by using framework middleware
  for each control instead of custom code, keeping the footprint small.
- **SQLite as a shared store** is single-node only → explicitly a non-goal; fine
  for the service's scope.

## Open Questions

- **`422` vs `500` for incomplete KYC.** `422` is the current choice; it may be
  revisited. It is isolated in the error-handling middleware, so changing it does
  not affect the specs' behavior intent (fail + flag as serious), the approach, or
  the task breakdown — only the emitted status code.
