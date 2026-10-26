# Tasks

## 1. Client build integration

- [x] 1.1 Replace the publish-only `BuildReactClient` target with an incremental `npm install` target (inputs `web/package.json`, `web/package-lock.json`; output `web/node_modules/.package-lock.json`); verify that after deleting `web/node_modules`, `dotnet build` runs `npm install` once and a second `dotnet build` skips it.
- [x] 1.2 Add the incremental client build target (inputs `web/src/**`, `web/index.html`, `web/vite.config.js`, `web/package.json`, `web/package-lock.json`; output `wwwroot/index.html`) that runs `npm run build`, clears `wwwroot/` and copies `web/dist/` in, and does not run during design-time builds; verify that deleting `wwwroot/` and running `dotnet build` recreates it, an unchanged second build skips the target, and touching `web/src/App.jsx` rebuilds.
- [x] 1.3 Add the Node.js check (`node --version`, exit code ignored) that emits warning `KYC001` and skips the client build when Node.js is missing; verify by running `dotnet build` and `dotnet test` with a `PATH` that has `dotnet` but not `node`: both succeed, `KYC001` appears, and the next build shows it again.
- [x] 1.4 Add the `SkipClientBuild` condition to both client targets with a skip message; verify `dotnet build -p:SkipClientBuild=true` and `SkipClientBuild=true dotnet build` both print the message and run no npm command.
- [x] 1.5 Run the client build before static web assets are resolved and add the generated files as `Content` items (excluding existing ones); verify in a fresh `git clone` in a temp directory that `dotnet publish src/KycAggregation.Api -o <out>` produces `<out>/wwwroot/index.html`, and that publishing again without cleaning gives no `NETSDK1152` error.
- [x] 1.6 Force the client build during `dotnet publish` (delete `wwwroot/index.html` when `_IsPublishing` is true, or a publish-only target if that property is unreliable); verify that with an up-to-date `wwwroot/`, `dotnet publish` still runs `npm run build`.

## 2. Startup warning when the client is missing

- [x] 2.1 In `Program.cs`, check `WebRootFileProvider.GetFileInfo("index.html").Exists` after `builder.Build()` and log one warning via `app.Logger` when it is missing; add a test that points the web root at an empty temp directory and asserts the warning is logged once, `GET /` returns 404, and `GET /kyc-data/{ssn}` with a valid key behaves as before; verify `dotnet test` passes.
- [x] 2.2 Add a test that points the web root at a temp directory containing an `index.html` and asserts no client warning is logged and `GET /` returns 200; verify `dotnet test` passes.

## 3. Keep SSNs out of upstream request logs

- [x] 3.1 Set the `System.Net.Http.HttpClient` log level to Warning in `appsettings.json`, and add a test that runs the real upstream client against a stub handler and asserts no log line contains the SSN; verify the test fails without the setting and passes with it, and that `dotnet run` no longer logs upstream URLs.

## 4. README

- [x] 4.1 Write `README.md` at the repository root with prerequisites, run, tests (`dotnet test`), trying the API (`X-Api-Key` header and dev key, a `curl` example, the five test SSNs with their expected status), the optional web client, and a Decisions section as listed in design.md; verify every command in it runs as written in a fresh clone and each Decisions bullet matches the current code.

## 5. End-to-end verification

- [x] 5.1 In a fresh clone with Node.js, run `dotnet run --project src/KycAggregation.Api` and verify `/` serves the client and a lookup works; then with Node.js removed from `PATH`, verify the build shows `KYC001`, `dotnet test` passes, the API logs the startup warning, `/` returns 404, and `/kyc-data/{ssn}` without a key returns 401.
