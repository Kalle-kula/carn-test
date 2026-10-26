# Spec Delta

## MODIFIED Requirements

### Requirement: Client is served from the API origin

When the client has been built, it SHALL be delivered as static assets from the
same origin as the KYC aggregation API so that no cross-origin configuration is
required, and its assets SHALL be reachable without presenting an API key. When
the client has not been built, the API SHALL keep serving its endpoints and SHALL
log a warning at startup that the client is unavailable.

#### Scenario: Loading the client

- **WHEN** the client has been built and a user navigates to the API's root URL
  in a browser without an API key
- **THEN** the client's HTML, JavaScript, and CSS are served with a success status
  and the client application renders

#### Scenario: Deep link falls back to the client

- **WHEN** the client has been built and a user requests any non-API client route
  that is not a static file
- **THEN** the client's entry page is returned so client-side routing can resolve
  the view

#### Scenario: Client not built

- **WHEN** the API starts without a built client
- **THEN** it logs one warning at startup that the client is not available, the
  KYC endpoint responds exactly as it does when the client is present, and a
  request to the root URL returns not found
