# Note Taking App

This repository is a DDD-focused exercise for a note-taking application. The
goal is to make the domain model and API contracts explicit before adding
infrastructure or UI complexity.

## Domain model

The application has three aggregate roots:

| Aggregate | Responsibility |
|---|---|
| `User` | Represents a note owner and team member. |
| `Team` | Owns team membership. |
| `Note` | Represents note content, its owner, and its sharing scope. |

Identifiers and other domain concepts will be represented with small value
objects rather than primitives. Aggregate invariants will be enforced in
constructors and behavior methods, not in API endpoints.

### Note sharing

Each note belongs to one user and has exactly one sharing scope:

| Scope | Rule |
|---|---|
| Private | Visible only to its owner. |
| Team | Shared with members of one team. |
| Public | Visible to all users. |

A team-scoped note must reference a team. Private and public notes must not
reference a team.

## API boundaries

Two APIs keep administrative work separate from user-facing note operations:

| API | Responsibility |
|---|---|
| `NTA.Api.Management` | Creates teams and users, and manages team membership. |
| `NTA.Api.Users` | Creates, reads, edits, and deletes notes. It does not manage teams or users. |

SQLite is the initial persistence implementation. It is an implementation
detail behind repository contracts, so the domain and API contracts remain
independent of the database.

## Delivery phases

1. **Phase 0 — Architecture and delivery guardrails (this PR):** document the
   domain model, API boundaries, note-sharing rules, delivery sequence, and
   pull-request validation.
2. **Phase 1 — Note domain and contracts:** add note value objects, the
   aggregate, sharing invariants, repository contract, and focused domain
   tests.
3. **Phase 2 — SQLite persistence:** implement the note repository and verify
   persistence behavior against SQLite.
4. **Phase 3 — Users API note CRUD:** add the HTTP contract and endpoints for
   creating, reading, updating, and deleting notes, backed by the repository.
5. **Phase 4 — Management API:** add users, teams, and membership management;
   connect team-note access to membership rules.

## Planned Users API contract

Phase 3 will expose the following routes:

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/notes` | Create a note for a user. |
| `GET` | `/notes/{noteId}` | Retrieve a note. |
| `GET` | `/notes` | List notes available to the requesting user. |
| `PUT` | `/notes/{noteId}` | Replace editable note fields and sharing scope. |
| `DELETE` | `/notes/{noteId}` | Delete an owned note. |

Authentication and authorization are intentionally deferred. Until then, the
contracts will explicitly carry the user context needed to exercise ownership
and sharing rules; no endpoint will silently infer it.

## Pull-request validation

The GitHub Actions workflow at
`.github/workflows/pull-request-validation.yml` restores, builds, and tests
the complete solution for every pull request. Configure this check as a
required status check in the repository branch protection rule for the default
branch to block merges when validation fails.