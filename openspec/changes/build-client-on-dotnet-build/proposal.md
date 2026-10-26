# Proposal

## Why

The React client is built into the API's `wwwroot` only on `dotnet publish`, and
`wwwroot` is gitignored. On a fresh clone, `dotnet run` starts a working API but
`/` returns 404 because no client was ever built. There is also no README, so
nothing explains how to build or run the API, the tests, or the client. Building
the client as part of the normal .NET build, while never making Node a hard
requirement, makes a fresh clone work with one command.

## What Changes

- `dotnet build` (and therefore `dotnet run` and `dotnet test`) builds the React
  client into the API's `wwwroot`, incrementally: the client build is skipped
  when nothing under `web/` has changed since the last build.
- When Node.js is not installed, the client build is skipped with a build
  warning; the build never fails because of it. The API, including `/kyc-data`,
  runs normally without the client.
- An opt-out property, `SkipClientBuild=true` (for example
  `dotnet test -p:SkipClientBuild=true`), skips the client build even when
  Node.js is installed.
- When the API starts without a built client, it logs one warning saying the
  client is not available at `/` and how to build it.
- `dotnet publish` always does a full client build and the published output
  includes the client assets (from a clean clone, not only when `wwwroot`
  already exists).
- Add a `README.md` limited to starting and running the app: prerequisites, how
  to run it, how to run the tests, how to try the API (API key, `curl` example,
  the five upstream test SSNs and the status each returns), the optional web
  client, and a Decisions section explaining the main design choices.

## Capabilities

### New Capabilities

<!-- None: build integration and documentation are tooling, not system behavior. -->

### Modified Capabilities

- `kyc-client-ui`: "Client is served from the API origin" changes from always
  serving the client to serving it when it has been built; when the client was not
  built (for example, Node.js unavailable), the API keeps working, only the
  client is absent, and a startup warning says so.

## Impact

- **Build** (`src/KycAggregation.Api/KycAggregation.Api.csproj`): the
  publish-only client target is replaced by an incremental target that runs on
  build, detects Node.js, warns and skips when it is missing, honors
  `SkipClientBuild`, and adds the generated files to the publish output.
- **Startup** (`src/KycAggregation.Api/Program.cs`): a check for the built
  client's `index.html` that logs a warning when it is missing.
- **Tests**: `dotnet test` builds the API project and so may build the client
  when Node.js is present; it still passes without Node.js.
- **Docs**: new `README.md` at the repository root with run instructions and
  design decisions.
- **Developer prerequisites**: .NET SDK (per `global.json`) is required; Node.js
  becomes optional and is only needed to get the client.
- **No API contract change**: `/kyc-data/{ssn}` behavior and authentication are
  unchanged.
