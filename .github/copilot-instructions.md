# Note Taking App

Using DDD, ports and adapters.

## Domain model

The application has three aggregate roots:

| Aggregate | Responsibility |
|---|---|
| `User` | Represents a note owner and team member. |
| `Team` | Owns team membership. |
| `Note` | Represents note content, its immutable owner, and lifecycle. |
| `NoteAccessPolicy` | Owns the grants for one note, keyed by its `NoteId`. |

Identifiers and other domain concepts will be represented with small value
objects rather than primitives. Aggregate invariants will be enforced in
constructors and behavior methods, not in API endpoints.

### Note access

Notes do not contain sharing fields. A separate `NoteAccessPolicy` stores
generic grants made up of a principal kind (`User` or `Team`), principal
identifier, and permission (`Read`, `Write`, or `Delete`). Policies reject
invalid identifiers and duplicate grants. The note owner always has every
permission without requiring an explicit grant.

Authorization first checks the owner, then an exact direct user grant, then
exact grants to teams of which the user is a member. Permissions do not imply
one another.

## API boundaries

Two APIs keep administrative work separate from user-facing note operations:

| API | Responsibility |
|---|---|
| `NTA.Api.Management` | Issues management tokens; team management remains intentionally out of scope. |
| `NTA.Api.Users` | Creates, reads, edits, and deletes notes, and lets owners add access grants. |

SQLite is the initial persistence implementation. It is an implementation
detail behind repository contracts, so the domain and API contracts remain
independent of the database.

## Pull-request validation

The GitHub Actions workflow at
`.github/workflows/pull-request-validation.yml` restores, builds, and tests
the complete solution for every pull request. Configure this check as a
required status check in the repository branch protection rule for the default
branch to block merges when validation fails.
