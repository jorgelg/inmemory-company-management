# ADR 001: API design

- **Status:** Accepted
- **Date:** 2026-10-14

## Context

The API must create companies, list them, get one by id, filter by name and domain, and search by relevance. It has to use correct HTTP semantics, return concise validation errors, expose only DTOs, keep business logic out of controllers, and leave room for updates, deletion, paging and sorting. Route and query design are explicitly part of the evaluation.

## Decision

**One resource, plural noun, query parameters for everything that narrows a collection.**

| Operation | Request | Response |
|---|---|---|
| Create | `POST /api/companies` | `201 Created`, a `Location: /api/companies/{id}` header and the created representation |
| List, filter, search, sort, page | `GET /api/companies?name=&domain=&q=&sort=&page=&pageSize=` | `200 OK` with `{ items, page, pageSize, totalCount, totalPages }` |
| Get one | `GET /api/companies/{id:guid}` | `200 OK`, or `404` |

- **Filtering and search are not separate endpoints.** `/api/companies/search` or `/api/companies/by-domain/{d}` would be RPC-style verbs on a resource; query parameters compose (`?name=verus&domain=verus.com&sort=-createdAt`) and keep one cacheable collection URL.
- **`q` means relevance search.** It is the conventional name for free-text search. Results default to relevance order and carry a `relevanceScore`; the field is omitted when there is no `q`, so ordinary listings stay clean. `sort` can still override the order (`?q=verus&sort=name`) without losing the scores.
- **`domain` accepts what users paste.** `acme.com`, `www.acme.com` and `https://shop.acme.com/x` all match on the registrable domain `acme.com`; a bare fragment like `acme` falls back to a host substring match.
- **`sort` uses the `-field` convention** for descending order (`name`, `-name`, `createdAt`, `-createdAt`, `relevance`). It's one parameter and reads naturally in URLs.
- **The paging envelope** (`items` + totals) rather than headers, so a browser client reads it without custom header exposure. Defaults: page 1, pageSize 20, max 100.
- **Empty results are `200` with `items: []`.** A `404` means "this resource doesn't exist"; an empty collection does exist.
- **Ids are GUID v7.** Time-ordered (good for index locality if a database is added), unguessable, and the `{id:guid}` route constraint turns `GET /api/companies/abc` into a `404`, not a `400`.

**Status codes**

| Code | When |
|---|---|
| 201 | Company created |
| 200 | Read succeeded, even when empty |
| 400 | Malformed body, failed field validation, name not relevant to the website, invalid query |
| 404 | Unknown id, or an unmatched route |
| 409 | A company with the same registrable domain already exists (also when a concurrent insert wins the race) |
| 415 | Body is not JSON |
| 500 | Unhandled exception; generic message only |

**409 for duplicates** is the one code beyond the spec's list. The spec asks for "consistent and correct use of HTTP status codes", and a uniqueness conflict is a state conflict, not malformed input.

**Errors are RFC 9457 ProblemDetails** (`application/problem+json`): `title`, `status`, a concise human `detail`, `traceId`, plus:
- `code`: a stable machine-readable key (`company.not_relevant`, `website.invalid_host`, `query.invalid_page_size`, …), so clients can branch without parsing prose.
- `errors`: field → messages, keyed by JSON name (`name`, `website`, `pageSize`), so a form can put each message next to its field.

**Thin controllers.** Each action binds input, makes one `ICompanyService` call, and maps `Result<T>` through `ErrorResults.ToProblem`, the single place where error types become status codes. Business rules return `Result<T>`/`Error` values instead of throwing, so expected failures are explicit in signatures and cheap.

## Consequences

- Clients get one predictable error shape for every failure, including model-binding errors (rewritten by `InvalidRequestResponseFactory` so serializer internals never leak).
- New operations slot in without redesign: `PUT`/`PATCH`/`DELETE /api/companies/{id}`, more filters on the same collection, cursor paging if offsets become costly.
- `relevanceScore` appears conditionally, so clients must treat it as optional (the TypeScript model does).
- Implementation notes that bit us:
  - Action methods have no `Async` suffix, because MVC trims it and `CreatedAtAction(nameof(...))` would then not resolve.
  - There is no controller-level `[Produces]`, because it overwrites `application/problem+json` on errors.
  - A query property bound as `q` but named `Search` needs `[JsonPropertyName("q")]`, or validation leaves it unvalidated and every `?q=` request fails with an empty 400.

## Alternatives considered

- **Minimal APIs.** Equally capable. Controllers were chosen because the spec talks about "controller layers", and `[ApiController]` gives automatic model validation and OpenAPI metadata with little code.
- **Separate `/search` endpoint.** Rejected as RPC-style and non-composable with the other filters.
- **400 for duplicates.** Rejected: the request is well-formed; it conflicts with current state.
