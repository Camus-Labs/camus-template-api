# Feature Specification

## Metadata

- Request Date: `2026-05-02`
- Requested By: `3M0R4C`
- Owner: `3M0R4C`
- Status: `Done`

## Goal

Provide idempotency guarantees for POST endpoints so that duplicate requests with
the same `Idempotency-Key` header are detected and either replayed from cache or
rejected on body conflict, preventing double-processing of write operations.

## Business Value

- Eliminates double-charging and duplicate writes on client retries
- Standardizes safe-retry semantics across all POST endpoints
- Aligns the API with industry idempotency conventions (Stripe-style)

## Stories

| Story ID | Title | Depends On | Status |
| --- | --- | --- | --- |
| `US-01` | Idempotency Key Enforcement | `-` | `Done` |
| `US-02` | Idempotent Response Caching | `US-01` | `Done` |
| `US-03` | Apply Idempotency to POST Endpoints | `US-01, US-02` | `Done` |

## In Scope

Scope at the capability level. Individual functional requirements live in stories.

- `[RequireIdempotencyKey]` attribute and validation filter
- Response caching keyed by (principal, idempotency-key) with TTL
- Request-body conflict detection (HTTP 409)
- `Idempotency-Key-Status: hit/miss` response header
- Configurable TTL policies (default, long-term) in appsettings
- Applying enforcement to all existing POST endpoints

## Out of Scope

- Idempotency for non-POST methods
- Distributed cache backend (in-memory only in this release)

## Feature-Level Constraints

- Cache must fail open: if the cache is unavailable, requests proceed normally
- Only 2xx responses are cached; 4xx/5xx are not

## Open Questions

- None

## Product Owner Handoff Gate

- Metadata complete: `Yes`
- Goal stated as outcome (not implementation): `Yes`
- All stories created under this feature folder: `Yes`
- Story `Depends On` column lists only direct prerequisites: `Yes`
- Ready for development: `Yes`
- Product Owner sign-off: `Internal Service Team, 2026-05-02`
