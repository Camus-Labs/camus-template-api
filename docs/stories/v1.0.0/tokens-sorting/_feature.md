# Feature Specification

## Metadata

- Request Date: `2026-04-28`
- Requested By: `3M0R4C`
- Owner: `3M0R4C`
- Status: `Done`

## Goal

Allow authenticated API consumers to sort the list of generated tokens by a
specified field and direction so they can organize token results when reviewing
them.

## Business Value

- Improves usability for consumers managing large token collections
- Aligns the token listing endpoint with standard list-resource conventions

## Stories

| Story ID | Title | Status |
| --- | --- | --- |
| `US-01` | Sort Generated Tokens | `Done` |

## In Scope

Scope at the capability level. Individual functional requirements live in stories.

- Server-side sorting on `GET /api/v2/auth/tokens` via `sortBy` and `sortDirection` query parameters
- Validation that both parameters must be present or both absent

## Out of Scope

- Multi-field sorting
- Sorting on endpoints other than token listing

## Cross-Story Dependencies

- None

## Feature-Level Constraints

- Sorting must be applied at the database level, before pagination

## Open Questions

- None

## Product Owner Handoff Gate

- Metadata complete: `Yes`
- Goal stated as outcome (not implementation): `Yes`
- All stories created under this feature folder: `Yes`
- Cross-story dependencies identified: `Yes`
- Ready for development: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-04-28`
