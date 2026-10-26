# Tasks

## 1. Ignore and untrack the Data Protection keys

- [x] 1.1 Add `keys/` to `.gitignore` under the "Secrets / local config" section; verify `git check-ignore -v src/KycAggregation.Api/keys/key-7014ceca-e8b5-4884-9e8c-ba102e9d9897.xml` reports the new rule once the file is untracked.
- [x] 1.2 Stop tracking the committed key with `git rm --cached src/KycAggregation.Api/keys/key-7014ceca-e8b5-4884-9e8c-ba102e9d9897.xml`; verify `git ls-files | grep keys/` prints nothing, the file still exists on disk, and `git status` shows only a staged deletion for it (no untracked `keys/` entry).

## 2. Verification

- [x] 2.1 Verify the local API still reads the existing cache: run `dotnet run --project src/KycAggregation.Api` and request `19900220-5678` with the dev key; it returns 200 from `kyc.db` without upstream calls.
- [x] 2.2 Verify a fresh copy of the tracked files (`git ls-files -co --exclude-standard`) contains no `keys/` folder, and that `dotnet run` there generates a new key and serves a lookup with 200.

## Workflow follow-up

- Commit and push the `.gitignore` change and the key removal (left to the user).
