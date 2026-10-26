# Tasks

## 1. Frontend scaffolding

- [x] 1.1 Create a Vite + React app in a new `web/` directory and verify `npm install` succeeds and `npm run build` produces a static bundle in `web/dist`.
- [x] 1.2 Configure the Vite dev server to proxy `/kyc-data` to the API (`http://localhost:5034`) and verify a dev request reaches the API (401 without a key confirms the proxy path).

## 2. Client UI and lookup logic

- [x] 2.1 Add an API-key input field bound to app state, with no key hardcoded in the bundle; verify the built JS contains no literal dev key (grep the bundle) and that a lookup with an empty key is blocked with a prompt to enter one.
- [x] 2.2 Add SSN list management (add and remove entries); verify adding an SSN shows it in the list and removing it takes it out and excludes it from lookups.
- [x] 2.3 Implement per-SSN lookup that calls `GET /kyc-data/{ssn}` with the `X-Api-Key` header, one request per SSN; verify a successful lookup renders the record fields (name, address, tax country, income) in that row.
- [x] 2.4 Render per-SSN error outcomes from the `{ "error": "..." }` envelope by status (`400/404/422/429/401/500`); verify a mix of valid and invalid SSNs shows each row's result independently so one failure does not hide another.

## 3. API static-file serving

- [x] 3.1 Add `UseStaticFiles` and `MapFallbackToFile("index.html")` to `src/KycAggregation.Api/Program.cs` with `UseStaticFiles` placed before `UseApiKeyAuthentication`; verify loading the API root in a browser serves the client with no API key and that `GET /kyc-data/{ssn}` without a key still returns 401.
- [x] 3.2 Publish the SPA build output into the API's `wwwroot` (wire into the build/publish step or document the build command); verify `dotnet run --project src/KycAggregation.Api` serves the current client at the root URL.

## 4. End-to-end verification

- [x] 4.1 With the API running, load the client, enter the dev API key, add a list of SSNs including at least one that resolves and one that errors, run the lookups, and verify each row shows the expected record or error state.
