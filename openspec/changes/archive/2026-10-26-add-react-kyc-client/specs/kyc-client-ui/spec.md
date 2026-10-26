# Spec Delta

## Purpose

Provides a browser client for the KYC aggregation service so an operator can enter
an API key and a list of SSNs, look each one up, and review the aggregated record
or the per-SSN error without crafting HTTP requests by hand.

## ADDED Requirements

### Requirement: Client is served from the API origin

The client SHALL be delivered as static assets from the same origin as the KYC
aggregation API so that no cross-origin configuration is required, and its assets
SHALL be reachable without presenting an API key.

#### Scenario: Loading the client

- **WHEN** a user navigates to the API's root URL in a browser without an API key
- **THEN** the client's HTML, JavaScript, and CSS are served with a success status
  and the client application renders

#### Scenario: Deep link falls back to the client

- **WHEN** a user requests any non-API client route that is not a static file
- **THEN** the client's entry page is returned so client-side routing can resolve
  the view

### Requirement: User supplies the API key in the client

The client SHALL provide a field for the user to enter the API key at runtime and
SHALL send that key on every KYC lookup request. The client SHALL NOT embed a
hardcoded API key in its bundle.

#### Scenario: Key provided

- **WHEN** the user has entered an API key and triggers a lookup
- **THEN** the request to the KYC endpoint includes that key in the API-key header

#### Scenario: Key missing

- **WHEN** the user triggers a lookup without having entered an API key
- **THEN** the client does not send the request and prompts the user to provide a
  key

### Requirement: User manages a list of SSNs

The client SHALL let the user add SSNs to a list and remove entries from it before
looking them up.

#### Scenario: Add an SSN

- **WHEN** the user enters an SSN and confirms adding it
- **THEN** the SSN appears in the list of SSNs to look up

#### Scenario: Remove an SSN

- **WHEN** the user removes an entry from the list
- **THEN** that SSN no longer appears in the list and is not looked up

### Requirement: Client looks up each SSN and shows the outcome

The client SHALL request the KYC record for each SSN in the list and SHALL present,
per SSN, either the aggregated record fields or the error outcome returned by the
service.

#### Scenario: Successful lookup

- **WHEN** the service returns an aggregated record for an SSN
- **THEN** the client displays that SSN's record fields (such as name, address, tax
  country, and income) in the results

#### Scenario: Error outcome

- **WHEN** the service returns an error status for an SSN (for example invalid SSN,
  not found, incomplete data, unauthorized, or rate limited)
- **THEN** the client displays that SSN's row with an indication of the failure
  rather than a record

#### Scenario: Mixed results across the list

- **WHEN** the list contains SSNs that resolve to different outcomes
- **THEN** each SSN's result is shown independently so one SSN's failure does not
  hide another SSN's result
