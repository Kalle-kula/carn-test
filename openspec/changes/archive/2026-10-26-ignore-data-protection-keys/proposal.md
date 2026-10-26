# Proposal

## Why

The ASP.NET Core Data Protection key that encrypts the personal data cached in
`kyc.db` is committed to git
(`src/KycAggregation.Api/keys/key-7014ceca-....xml`, added in `caba702` and
merged to `origin/main`). Key material does not belong in source control: the
app generates a key on first run, and every clone should have its own. The
`.gitignore` already excludes the database (`*.db`) but not the key folder.

## What Changes

- Add the Data Protection key folder (`keys/`) to `.gitignore`, next to the
  existing secrets and local config entries.
- Stop tracking the committed key file (`git rm --cached`). The file stays on
  disk, so the local API keeps decrypting the existing `kyc.db`.
- Fresh clones no longer receive a key; the app creates one on first run, as it
  already does when the folder is missing.

Out of scope:

- **Git history.** The key remains in earlier commits on `origin/main`. Removing
  it would mean rewriting history and force-pushing a shared branch. Accepted
  because the key only protects a local development cache filled from the
  upstream test API, and `kyc.db` itself has never been committed.
- **Key rotation.** The local key and `kyc.db` are kept as they are.

## Capabilities

### New Capabilities

<!-- None: repository hygiene only. -->

### Modified Capabilities

<!-- None: no system behavior changes. The change sets skip_specs: true. -->

## Impact

- **`.gitignore`**: one new entry.
- **Git index**: the key file is removed from tracking; the next commit records
  the removal. Committing and pushing are left to the user.
- **Runtime**: unchanged. The existing local key keeps working; fresh clones
  generate their own key.
