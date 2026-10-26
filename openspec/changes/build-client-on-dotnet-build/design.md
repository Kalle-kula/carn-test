# Design

## Context

See proposal.md - Why. Current state:

- `KycAggregation.Api.csproj` has one `BuildReactClient` target that runs before
  `Publish`: `npm install` (if `node_modules` is missing), `npm run build`, then
  clears `wwwroot/` and copies `web/dist/` into it.
- `wwwroot/`, `web/dist/` and `web/node_modules/` are gitignored. `web/` has a
  `package-lock.json`, and npm writes `node_modules/.package-lock.json` after
  each install.
- `Program.cs` serves `wwwroot/` with `UseStaticFiles()` and registers
  `MapFallbackToFile("index.html")` after the KYC endpoints.
- The test project references the API project, so `dotnet test` builds the API
  and runs any build target added to it. Tests run in the `Development`
  environment with the API project as content root.
- No CI exists, and warnings are not treated as errors.

## Goals / Non-Goals

**Goals:**

- A fresh clone with Node.js gets a working client from `dotnet run` alone.
- A fresh clone without Node.js builds, tests and runs the API with a warning.
- No client rebuild when nothing in `web/` changed.
- `dotnet publish` from a clean clone includes the client.

**Non-Goals:**

- No change to the client code, the Vite config or `npm run dev`.
- No CI pipeline.
- No change to the `/kyc-data` endpoint or authentication.

## Decisions

### Decision: Incremental client build target that runs on every build

A target runs before the build with `Inputs` = `web/src/**`, `web/index.html`,
`web/vite.config.js`, `web/package.json`, `web/package-lock.json` and `Outputs`
= `wwwroot/index.html`. MSBuild skips it when the output is newer than every
input. It runs `npm run build`, clears `wwwroot/` (Vite gives built files hashed
names, so old ones would otherwise pile up), and copies `web/dist/` in.

It does not run during IDE design-time builds (`'$(DesignTimeBuild)' != 'true'`),
so the editor doesn't start npm in the background.

- **Alternative - build every time**: rejected, adds several seconds to every
  build and test run.
- **Alternative - build only when `wwwroot/index.html` is missing**: rejected,
  serves an old client after edits with no warning.

### Decision: `npm install` as its own incremental step

A separate target runs `npm install` with `Inputs` = `package.json`,
`package-lock.json` and `Outputs` = `node_modules/.package-lock.json`. It runs on
a fresh clone and again whenever dependencies change. The current check (install
only when `node_modules` is missing) misses dependency changes.

### Decision: Missing Node.js is a warning, never an error

Before any npm command, the target runs `node --version` and ignores its exit
code. A non-zero exit code (command not found) produces a build warning with its
own code, for example `KYC001`: Node.js was not found, the client was not built,
the API still works, and `/` will not serve the client. The target then stops
without producing its output, so the next build checks again and repeats the
warning until Node.js is installed.

The warning has its own code so that if warnings are ever treated as errors,
`KYC001` can be added to `WarningsNotAsErrors` and builds without Node.js keep
working.

### Decision: `SkipClientBuild` opt-out

Both client targets have the condition `'$(SkipClientBuild)' != 'true'`. Set it
with `-p:SkipClientBuild=true` or as an environment variable (MSBuild reads
environment variables as properties). When set, a normal-importance message
says the client build was skipped. It is a message, not a warning, because the
user asked for it.

### Decision: Register generated files before static web assets are collected

The SDK collects `wwwroot/` files when the project is loaded. On a clean clone
the client build creates them later, so they are missing from that list and from
the publish output. The existing publish-only target probably has this problem.

The client target therefore runs before the SDK resolves static web assets
(target `ResolveProjectStaticWebAssets`; confirm the name during
implementation). After copying, it adds the new files as `Content` items,
excluding any that already exist (`Include="wwwroot/**" Exclude="@(Content)"`).
The files are then handled exactly as on a second build, for both `dotnet run`
and `dotnet publish`, without adding separate publish items.

- **Alternative - add files to `ResolvedFileToPublish` after
  `ComputeFilesToPublish`** (the old SPA template approach): rejected. Here the
  client is inside `wwwroot/`, so when the files already existed at load time
  they would be added twice and fail with a duplicate-output error (`NETSDK1152`).

### Decision: `dotnet publish` always rebuilds the client

When publishing, the target deletes `wwwroot/index.html` before the incremental
check, so the check always runs. This covers the one case incremental builds
miss: a file deleted from `web/src`. Publishing is detected with the SDK's
`_IsPublishing` property, which `dotnet publish` sets. If that property turns out
not to be reliable, the fallback is a publish-only target that runs the npm
build directly. Without Node.js, or with `SkipClientBuild`, publish follows the
same warning or message rules and produces an API-only output.

### Decision: One startup warning when the client is missing

After `builder.Build()`, `Program.cs` checks
`app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists`. If it
doesn't exist (including when `wwwroot/` itself is missing), `app.Logger` logs
one warning: the client is not built, `/` returns 404, and how to build it.

`MapFallbackToFile("index.html")` stays registered unconditionally. With no
file it returns 404, which is what the spec requires. Building the client while
the API is running then works without a restart.

- **Alternative - register the fallback only when the file exists**: rejected.
  Same 404, but a client built after startup is not served until restart.

### Decision: README covers running the app and the design decisions only

Sections:

- **Prerequisites:** .NET SDK per `global.json`; Node.js optional, only needed
  for the web client.
- **Run:** `dotnet run --project src/KycAggregation.Api`.
- **Tests:** `dotnet test`.
- **Try it:** `curl` with the `X-Api-Key` header and the dev key, plus the five
  upstream test SSNs and their expected status (four return 200,
  `19800115-1234` returns 422). Without the key and the SSN list, a first run
  ends in a 401 or 404 and looks broken.
- **Web client:** open `http://localhost:5034/`; built automatically when
  Node.js is installed.
- **Decisions:** persistence (EF Core + SQLite read-through cache, chosen over
  Redis), error handling (one error shape, clear user messages, unexpected
  errors logged and returned as a generic 500), status codes beyond the spec
  (400, 401, 422, 429, with the reason for 422), security (API key added because
  the spec has none, encrypted personal fields, hashed SSNs, no SSNs in logs),
  upstream differences from its documentation, KYC form requested as of today,
  known limitation (stored records never refreshed; 422 results not stored),
  test scope, and the web client being optional. The bullets describe the code
  as it is, not the original design document where they differ.

Left out: project layout, publish, the `KYC001` warning, `SkipClientBuild`, the
Vite dev server, and forcing a client rebuild. Those are build details, not
needed to start and run the app.

## Risks / Trade-offs

- **[Deleted files in `web/src` are not detected by incremental builds]** -> The
  client can be out of date until another input changes. Publish always
  rebuilds, and deleting `wwwroot/` forces a rebuild locally.
- **[First build after clone is slower]** -> Runs npm install and build once.
  `SkipClientBuild` avoids it, for example for test-only runs.
- **[`_IsPublishing` is an internal SDK property]** -> Could change in a future
  SDK. Fallback is the publish-only target described above.
- **[Static web assets target name differs between SDK versions]** -> Confirmed
  during implementation against the pinned SDK (`global.json`), and covered by
  the clean-clone publish check.
- **[Tests log the startup warning when the client isn't built]** -> Harmless.
  The warning contains no PII, so the test that no SSN appears in logs is
  unaffected.
