# Tasks

## 1. Move the decisions and add the reflection

- [x] 1.1 Create `REFLECTION.md` at the repository root with a `Decisions` section containing the README's current Decisions bullets unchanged except for the reworded KYC form date line (wording in proposal.md) and the added known-limitation line on why stored records are never refreshed (assignment requires serving from the store; upstream sends no `ETag`/`Last-Modified`), and an `Improve with more time` section with the items listed in proposal.md; verify with `git diff` that those two lines are the only differences from the section removed from the README.
- [x] 1.2 Remove the `## Decisions` section from `README.md`; verify the README's remaining headings are Prerequisites, Run, Tests, Try it and Web client, and that nothing else in it changed.
- [x] 1.3 Check each "Improve with more time" item against the current code so none describes something that already exists: rate limiting uses one global partition (`Program.cs`), the KYC form is requested as of today (`KycAggregationService`), no OpenAPI document is exposed, no `.github/workflows` exists, and the Data Protection key ring has no XML encryptor; verify by reading those files.

## Workflow follow-up

- Commit and push the documentation change (left to the user).
