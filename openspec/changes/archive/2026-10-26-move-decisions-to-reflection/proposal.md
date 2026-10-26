# Proposal

## Why

The assignment invites an optional `REFLECTION.md` for trade-offs, design
decisions, and what would be improved with more time. The design decisions
currently live in the README's Decisions section, while the README was meant to
cover only how to start and run the app. Moving the decisions into
`REFLECTION.md` gives each file one job and adds the missing "improve with more
time" part.

## What Changes

- Add `REFLECTION.md` at the repository root with two sections:
  - **Decisions**: the README's current Decisions section, moved unchanged
    except for two edits:
    - the KYC form date line is reworded, because "returns the latest version"
      is not accurate (the test data is identical for every date, and with
      caching the date only matters on the first request). New wording: "**KYC
      form date:** the Customer Data API requires a date, so the form is
      requested as of today. The endpoint in the task takes no date, and the
      test data is the same for any date."
    - one line is added under the known limitations explaining why never
      refreshing stored records is acceptable: the assignment requires serving
      from the local store, and the Customer Data API sends no `ETag` or
      `Last-Modified` header, so a change cannot be detected without fetching
      everything again.
  - **Improve with more time**: a short list grounded in this codebase:
    - refresh stored records (expiry or invalidation) instead of keeping them
      forever;
    - support historical lookups (pass an as-of date instead of always using
      today);
    - rate limit per API key instead of one global limit shared by all clients;
    - stronger authentication than a single API key, and a small backend for the
      web client so the key is not handled in the browser;
    - encrypt the Data Protection key ring at rest (ASP.NET warns it is stored
      unencrypted) and remove the old key from git history;
    - publish an OpenAPI document;
    - a CI pipeline that builds and tests with and without Node.js.
- Remove the Decisions section from `README.md` entirely, so the README covers
  only prerequisites, running, tests, trying the API, and the web client.
- Record in the `kyc-aggregation` spec that stored records are never updated:
  a new scenario under "Read-through persistence" states that upstream changes
  after the first fetch are not reflected. This documents existing behavior; the
  code does not change.

Out of scope: an "areas to discuss in the interview" section (not wanted), and
any code changes.

## Capabilities

### New Capabilities

<!-- None. -->

### Modified Capabilities

- `kyc-aggregation`: "Read-through persistence" gains a scenario stating that
  upstream changes after a record is stored are not reflected. Behavior is
  unchanged; the requirement now says so explicitly.

## Impact

- **Docs**: new `REFLECTION.md`; `README.md` loses its Decisions section.
- **Specs**: one added scenario in `kyc-aggregation`.
- **Code**: none.
