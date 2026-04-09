# emc.camus.domain

Domain layer for the Camus application containing core business entities, invariants, and exception types.

> **📖 Parent Documentation:** [Main README](../../../README.md) |
[Architecture Guide](../../../docs/architecture.md)

---

## 📋 Overview

The Domain layer is the innermost layer in the hexagonal architecture. It defines the business entities and rules
that the rest of the system depends on. It has **zero external dependencies** — no NuGet packages and no project
references — ensuring that business logic remains portable and free from infrastructure concerns.

---

## ✨ Features

- 🔒 **Immutable State** — All entity properties use private setters; mutation occurs only through
  validated constructors or explicit business methods
- 🛡️ **Constructor Validation** — Public constructors enforce business invariants before setting state
- 🏭 **Reconstitute Pattern** — Most entities provide a static `Reconstitute` factory that lets persistence
  adapters rebuild instances without re-running business validation (`AuthToken` is excluded — it is a simple
  value object with no reconstitution path)
- 📦 **Zero Dependencies** — No `PackageReference` or `ProjectReference` elements; pure C# only

---

## 📁 Namespace Structure

### `Auth/`

Business entities for authentication and authorization:

- **`User`** — Represents a system user with a username and assigned roles; auto-generates `Id` when not
  supplied; exposes `GetPermissions()` to aggregate distinct permissions across all roles
- **`Role`** — Represents an authorization role with a name, optional description, and a list of permissions;
  auto-generates `Id` when not supplied
- **`AuthToken`** — Represents an authentication token paired with a UTC expiration; validates that the token
  is non-empty and the expiration is in the future
- **`GeneratedToken`** — Tracks a user-created token with custom permissions and expiration; enforces that
  permissions are a subset of the creator's permissions, that the suffix contains only alphanumeric characters,
  dots, hyphens, and underscores, and that expiration falls within one hour to one year; supports `Revoke()` —
  only the original creator may revoke, and double-revocation is rejected
- **`ApiInfo`** — Describes API metadata (name, version, status, feature list) for documentation and
  status endpoints

### `Exceptions/`

Domain-specific exception types:

- **`DomainException`** — Thrown when a business invariant is violated; all domain rule failures use
  this type

---

## 🔄 Entity Lifecycle

Most entities follow a consistent lifecycle pattern. `AuthToken` is a lightweight value object that uses only
the public constructor and does not support reconstitution or mutation.

| Phase | Mechanism | Validation |
| ----- | --------- | ---------- |
| **Creation** | Public constructor | Full business-rule validation |
| **Reconstitution** | `Reconstitute` static factory | Skipped — data is already validated |
| **Mutation** | Business methods (e.g., `Revoke()`) | Guard clauses before any field change |

On `GeneratedToken`, lifecycle and audit fields (`CreatedAt`, `RevokedAt`) are read-only and populated only
through `Reconstitute` or the `Revoke()` business method.

---

## ⚙️ Configuration

This layer requires no configuration. It contains only pure business logic with no infrastructure bindings.

---

## 🔗 Integration

The Domain layer is referenced by the Application layer (`emc.camus.application`), which defines service
interfaces and orchestrates domain operations. Persistence adapters interact with most domain entities through
the `Reconstitute` factory when loading data and through public constructors when creating new instances.

---

## 🔧 Troubleshooting

| Symptom | Likely Cause |
| ------- | ------------ |
| `DomainException` at runtime | A business invariant was violated — check the exception message for the specific rule |
| `ArgumentException` during entity creation | A required field was null, empty, or whitespace |
| `ArgumentOutOfRangeException` during entity creation | A value fell outside its allowed range (e.g., empty GUID, expired date) |
