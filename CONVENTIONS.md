# Coding conventions

How code in this repository is written. The build enforces what it can (analyzers, `.editorconfig`, banned APIs, warnings as errors); the rest is listed here. When a convention changes, update this file in the same change.

## 1. Architecture rules

| Layer | May reference | Contains |
|---|---|---|
| `Companies.Domain` | nothing | Entities, value objects, `Result`/`Error`, domain errors |
| `Companies.Application` | Domain | Use cases (services), DTOs, ports (interfaces), relevance + search logic |
| `Companies.Infrastructure` | Application | Port implementations (in-memory repository, HTTP fetcher), DI registration |
| `Companies.Api` | Application, Infrastructure (DI only) | Controllers, HTTP mapping, middleware, composition root |

- **Controllers hold no business logic.** They bind input, call one application service method, and map the `Result` to an HTTP response.
- **Only DTOs cross the API boundary.** Never return or accept a domain entity or storage type over HTTP.
- **Storage types are `internal`** to Infrastructure and are mapped to and from domain entities inside the repository.
- **Ports live in Application**, and their implementations live in Infrastructure. A new external dependency always gets an interface first.

## 2. C# style

- **File layout**: one public type per file, named after the type. File-scoped namespaces that match the folder path (`Companies.Domain.Companies`). `using` directives sit outside the namespace, `System` first.
- **Naming**: `PascalCase` for types, members and constants; `_camelCase` for private fields; `camelCase` for locals and parameters; `I` prefix for interfaces; `Async` suffix on async methods.
- **Types**: `sealed` by default. Use records for value objects and DTOs, and classes for entities and services. Use primary constructors for services that only capture dependencies.
- **Immutability**: get-only or `init` properties; no public setters on domain types. Expose collections as `IReadOnlyList<T>`/`IReadOnlyCollection<T>`.
- **Modern C#**: `var` when the type is apparent, collection expressions (`[]`), pattern matching, `required` members on DTOs, `[GeneratedRegex]` for regular expressions, `FrozenSet`/`FrozenDictionary` for static lookup tables.
- **Nullability** is enabled everywhere. Don't use `!` except right after a check the compiler can't see (e.g. after `IsSuccess`).
- **Time and ids**: inject `TimeProvider` and never call `DateTime.Now` or `DateTimeOffset.UtcNow` directly. Ids are `Guid.CreateVersion7(timestamp)`.
- **Comments**: XML `<summary>` on public types and on members whose purpose isn't obvious from the name. Inline comments explain *why*, never *what*.
- **Analyzer suppressions** go on the narrowest scope possible, using `[SuppressMessage]` with a `Justification`. Never suppress a rule project-wide without recording why in `.editorconfig`.

## 3. Validation and errors

- **Expected failures are values, not exceptions.** Factories and services return `Result<T>`; an `Error` carries a stable `Code` (`area.reason`, snake_case, e.g. `website.invalid_host`), a concise user-facing `Message`, an `ErrorType` (Validation / NotFound / Conflict), and an optional `Target` (the JSON field name).
- Define errors as `static readonly` fields in an `XxxErrors` class next to the type that raises them, never inline string literals at the call site.
- **Value objects validate themselves** in a `static Create(string?) → Result<T>` factory with a private constructor, so an invalid instance can't exist. Normalize (trim, case, canonical form) before validating.
- **Exceptions are for bugs and broken invariants** (`ArgumentNullException`, `ArgumentException`, `InvalidOperationException`). They are never caught to drive control flow. Unexpected exceptions reach the global handler and become a 500 that leaks no details.
- **HTTP mapping** (Api only): Validation → 400 `ValidationProblemDetails`, NotFound → 404, Conflict → 409, all RFC 9457 ProblemDetails. Success → 200, or 201 with a `Location` header for creation.
- **Messages** are one sentence, end with a period, and say what's wrong and, where useful, what a valid value looks like ("…e.g. https://www.example.com.").

## 4. Async, I/O and logging

- Every method that does or may do I/O is async, returns `Task`/`Task<T>`/`ValueTask<T>`, and takes a `CancellationToken cancellationToken` as its last parameter. Pass the token on to every awaited call. No `.Result`, no `.Wait()`, no `async void`.
- Outbound HTTP uses a typed `HttpClient` registered through `IHttpClientFactory`, with an explicit timeout.
- Logging uses `[LoggerMessage]` source-generated methods (in a `static partial class XxxLog` next to the class that logs). Use structured placeholders (`{CompanyId}`), never string interpolation. Levels:
  - `Information`: business events (company created).
  - `Warning`: rejected input, a degraded dependency (validation failure, fetch failure).
  - `Error`: unexpected exceptions.
  - Never log secrets or full response bodies.

## 5. API design

- Resources are plural nouns: `/api/companies`, `/api/companies/{id:guid}`. No verbs in routes.
- Filtering, search, paging and sorting are query parameters on the collection: `?name=&domain=&q=&page=&pageSize=&sort=`.
- An empty collection is a `200` with `items: []`, not a 404. A missing item is a 404.
- Request and response DTOs are `sealed record`s suffixed `Request` / `Response` and use camelCase JSON.
- Every endpoint is covered by integration tests for each status code it can return.

## 6. Testing

- **Stack**: NUnit 4 (runner, `[TestFixture]`, `[Test]`, `[TestCase]`), **Shouldly** for every assertion, NSubstitute for test doubles of ports, `FakeTimeProvider` for time, and `WebApplicationFactory<Program>` for API tests.
- **NUnit `Assert`, `Assume` and the classic asserts are banned** (`tests/BannedSymbols.txt`; the build fails with RS0030). Use Shouldly:

  | Instead of | Write |
  |---|---|
  | `Assert.That(x, Is.EqualTo(y))` | `x.ShouldBe(y)` |
  | `Assert.That(b, Is.True)` | `b.ShouldBeTrue()` |
  | `Assert.That(x, Is.Null)` | `x.ShouldBeNull()` / `x.ShouldNotBeNull()` (returns the non-null value) |
  | `Assert.That(() => f(), Throws.TypeOf<T>())` | `Should.Throw<T>(() => f())` / `await Should.ThrowAsync<T>(...)` |
  | `Assert.Multiple(...)` | `subject.ShouldSatisfyAllConditions(s => ..., s => ...)` |

- **Result helpers** (`Companies.UnitTests.Support`): `result.ShouldBeSuccess()` returns the value and reports the error on failure; `result.ShouldBeFailure()` returns the `Error`; `result.ShouldBeValidationError(code, target)`.
- **Naming**: the class is `<TypeUnderTest>Tests`; methods follow `Method_Scenario_Expectation` (`Create_WithTooShortName_Fails`).
- **Shape**: Arrange / Act / Assert separated by blank lines, with no `// Arrange` comments. One behaviour per test; use `[TestCase]` for input tables rather than loops.
- **Isolation**: no real network, clock or file system. Substitute `IWebsiteContentFetcher`, use `FakeTimeProvider`, and give each test a fresh repository.
- **Mocks**: NSubstitute for ports only, never for domain types or value objects. Verify interactions (`Received()`, `DidNotReceive()`) only when the interaction *is* the behaviour, e.g. "nothing is saved after a rejection".
- **TDD** for Domain and Application logic: write the failing test, watch it fail, then implement.
- **Location**: unit tests mirror the source layer (`Companies.UnitTests/Domain`, `/Application`, `/Infrastructure`); API tests live in `Companies.Api.IntegrationTests`.

## 7. Frontend (Angular)

- Standalone components, signals for state, `OnPush`-equivalent behaviour (zoneless). Use the new control flow (`@if`, `@for`) and `inject()` for DI.
- Feature folders: `features/<feature>/{data-access,ui}` plus a page component; cross-cutting code goes in `core/`.
- HTTP calls only in `data-access` services, which return typed models that mirror the API DTOs. Components never call `HttpClient` directly.
- Server ProblemDetails are parsed once (`core/http`) and shown as field errors or a banner. Client validators mirror the server rules but the server stays the source of truth.
- Minimal styling: plain CSS, no UI library.

## 8. Git

- Short, imperative commit subjects ("Add domain model and value objects"), with a brief body only when it adds something.
- No `Co-Authored-By:` or other AI attribution trailers.
- Each commit builds with zero warnings and passes all tests.
- Line endings are LF (`.gitattributes`); indentation follows `.editorconfig`.
