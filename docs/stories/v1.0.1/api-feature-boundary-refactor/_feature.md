# Feature Specification

## Metadata

- Request Date: `2026-05-27`
- Requested By: `3M0R4C`
- Owner: `3M0R4C`
- Status: `Done`

## Goal

Clarify the architectural boundary between API-pipeline features and true
infrastructure adapters by relocating rate-limiting, JWT, API Key, and Swagger
out of `Adapters/` and into the `Api/` layer, and consolidating their tests and
contracts to match.

## Business Value

- Eliminates incorrect classification of ASP.NET Core pipeline features as adapters
- Reduces project count and simplifies the solution graph
- Enables a future distributed-cache adapter to automatically enable multi-instance rate limiting
- Improves discoverability for new contributors

## Stories

| Story ID | Title | Depends On | Status |
| --- | --- | --- | --- |
| `US-01` | Relocate Rate Limiting | `-` | `Done` |
| `US-02` | Relocate JWT Security | `-` | `Done` |
| `US-03` | Relocate API Key Security | `-` | `Done` |
| `US-04` | Relocate Swagger Documentation | `-` | `Done` |
| `US-05` | Consolidate Tests and Docs | `US-01, US-02, US-03, US-04` | `Done` |
| `US-06` | Flatten Rate Limiting Settings | `US-01` | `Done` |
| `US-07` | Relocate Rate Limit Contracts | `US-01` | `Done` |

## In Scope

Scope at the capability level. Individual functional requirements live in stories.

- Relocation of rate-limiting, JWT, API Key, and Swagger features into the API layer
- Removal of the corresponding adapter and test projects
- Consolidation of tests into `emc.camus.api.test` and `emc.camus.api.integration.test`
- Flattening of `RateLimitingSettings` to a closed set of policy names
- Documentation updates (README, architecture)

## Out of Scope

- Behavioral changes to rate limiting, authentication, or documentation
- New rate-limit policies beyond default/strict/relaxed

## Feature-Level Constraints

- No behavioral or HTTP-contract changes; only internal restructuring
- All existing unit and integration tests must continue to pass

## Open Questions

- None

## Product Owner Handoff Gate

- Metadata complete: `Yes`
- Goal stated as outcome (not implementation): `Yes`
- All stories created under this feature folder: `Yes`
- Story `Depends On` column lists only direct prerequisites: `Yes`
- Ready for development: `Yes`
- Product Owner sign-off: `3M0R4C, 2026-05-27`
