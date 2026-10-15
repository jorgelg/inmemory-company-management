# In-memory company management

A Web API (ASP.NET Core, .NET 10) and an Angular 22 frontend for registering companies. Each submission is validated, including a check that the **company name is relevant to its website URL**. Valid companies are stored in memory and can be listed, filtered, searched by relevance and fetched by id.

- **Backend**: Clean Architecture (Domain / Application / Infrastructure / Api), RFC 9457 ProblemDetails, structured logging, 316 NUnit tests.
- **Frontend**: standalone, zoneless Angular with signals, Signal Forms and `httpResource`, 35 Vitest tests.
- **Design docs**: [architecture](docs/architecture.md) · [ADR 001 API design](docs/decisions/001-api-design.md) · [ADR 002 relevance strategy](docs/decisions/002-relevance-strategy.md) · [ADR 003 persistence](docs/decisions/003-persistence.md) · [coding conventions](CONVENTIONS.md)

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0.401 or later 10.0.x (pinned with roll-forward in `backend/global.json`) |
| Node.js | 24.15+ (or 22.22.3+ / 26+), required by Angular CLI 22 |
| npm | 11+ |

The backend serves HTTPS on port 7000 with the ASP.NET Core development certificate. If your browser or curl complains about it, trust it once with `dotnet dev-certs https --trust`, or use the HTTP port.

## Run it

**Backend** (http://localhost:5000 and https://localhost:7000):

```bash
cd backend/src/Companies.Api
dotnet run
```

**Frontend** (http://localhost:4200, with `/api` proxied to the backend on port 5000):

```bash
cd frontend
npm install
npm start
```

Open http://localhost:4200, add a company (try `Verus LLC` + `https://www.verus.com`), then search for it. Start the backend first: the frontend reaches it through the dev-server proxy (`frontend/proxy.conf.json`).

In Development, `dotnet run` opens the interactive **Scalar** API reference at http://localhost:5000/scalar/v1 (try any endpoint from the browser). The raw OpenAPI document is at http://localhost:5000/openapi/v1.json. Both are disabled outside Development. [`backend/src/Companies.Api/Companies.Api.http`](backend/src/Companies.Api/Companies.Api.http) has a ready-made request for every scenario (VS Code REST Client / Rider / Visual Studio).

## Test it

```bash
cd backend
dotnet test                 # 265 unit + 51 integration tests

cd ../frontend
npm run test:ci             # 35 Vitest tests (npm test = watch mode)
```

The backend tests never touch the network: the website content fetcher is substituted. The integration tests host the real API in memory (`WebApplicationFactory`) with DI validation on, so each run also checks the composition root.

## API

Base path: `/api/companies`. All errors are `application/problem+json` with `title`, `detail`, `status`, `traceId` and a machine-readable `code`. Field-level errors are also listed under `errors`.

| Method & route | Success | Errors |
|---|---|---|
| `POST /api/companies` | **201** + `Location` + the company | **400** validation or relevance failure · **409** a company with that registrable domain exists · **415** not JSON |
| `GET /api/companies` | **200** a page (an empty `items` is still 200) | **400** invalid query |
| `GET /api/companies/{id}` | **200** the company | **404** unknown id (or not a GUID) |

Query parameters for `GET /api/companies`:

| Parameter | Meaning |
|---|---|
| `name` | Partial or full company name, case-insensitive |
| `domain` | `acme.com`, `www.acme.com` or a URL matches the registrable domain; a fragment like `acme` matches any host containing it |
| `q` | Relevance search. Results are ordered by score, and each item carries `relevanceScore` (0–1) |
| `sort` | `name`, `-name`, `createdAt`, `-createdAt`, `relevance` (needs `q`). Default: relevance with `q`, otherwise name |
| `page`, `pageSize` | 1-based page (default 1), size 1–100 (default 20) |

```bash
# create: 201 Created + Location
curl -i -X POST http://localhost:5000/api/companies \
  -H "Content-Type: application/json" \
  -d '{"name":"Verus LLC","website":"https://www.verus.com"}'

# name not relevant to the website: 400, code company.not_relevant
curl -s -X POST http://localhost:5000/api/companies \
  -H "Content-Type: application/json" \
  -d '{"name":"Acme","website":"https://www.example.com"}'

# relevance search
curl -s "http://localhost:5000/api/companies?q=verus"

# filter, sort, page
curl -s "http://localhost:5000/api/companies?domain=verus.com&sort=-createdAt&page=1&pageSize=10"
```

Example responses:

```json
// 201 Created
{ "id": "01a13ac0-863a-71d0-9592-9751dbf2fbd0", "name": "Verus LLC", "website": "https://www.verus.com",
  "domain": "verus.com", "createdAt": "2026-10-14T14:10:58.2343615+00:00" }

// 400 Bad Request
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1", "title": "One or more validation errors occurred.",
  "status": 400, "code": "company.not_relevant", "traceId": "00-…",
  "detail": "Company name 'Acme' does not appear to be related to website 'example.com': domain label 'example' does not resemble the name; the website's title and headings do not mention the name (best score 0.00, threshold 0.60)." }
```

## How the relevance check works

In short (details in [ADR 002](docs/decisions/002-relevance-strategy.md)):

1. **Normalize the name.** Lowercase, strip accents and punctuation, drop trailing legal suffixes (LLC, Inc, GmbH, S.A., …) and generic words (the, and, group, …). "The Blue Bottle Coffee Company" becomes `blue bottle coffee`.
2. **Domain match (offline).** Compare the name with the registrable domain's label (`www.shop.acme.co.uk` → `acme`). The signals are an exact match, every word contained, the name and label containing each other, an acronym (`ibm`), a partial word match, and Jaro-Winkler similarity for typos. The strongest signal wins, giving a score from 0 to 1.
3. **Content inspection (fallback).** Only if the domain score is below the threshold (0.6, configurable): fetch the home page and look for the name in its `<title>`, `og:site_name` and first `<h1>`. This is how "Hacker News" is accepted for `news.ycombinator.com`. The fetch is SSRF-guarded (public IPs only, checked on every connection and redirect) and capped at 3 s and 256 KB.
4. **Explain the result.** A rejection lists every check that ran and the best score against the threshold.

## Project structure

```
backend/
  src/Companies.Domain           Company entity, CompanyName / WebsiteUrl value objects, Result/Error   (no dependencies)
  src/Companies.Application      use cases (CompanyService), DTOs, ports, relevance strategies, search ranker
  src/Companies.Infrastructure   in-memory repository, SSRF-guarded website content fetcher
  src/Companies.Api              thin controllers, ProblemDetails mapping, exception handler, CORS, OpenAPI
  tests/Companies.UnitTests              domain, application, infrastructure, architecture rules
  tests/Companies.Api.IntegrationTests   every endpoint and status code, over HTTP
frontend/src/app
  core/http                      API base URL, ProblemDetails parsing
  features/companies/data-access typed API client, signal store (httpResource)
  features/companies/ui          company-form, company-search, company-list
docs/                            architecture and decision records
```

## Configuration

`backend/src/Companies.Api/appsettings.json`:

| Key | Default | Purpose |
|---|---|---|
| `Relevance:Threshold` | `0.6` | Minimum relevance score to accept a company |
| `Relevance:ContentInspection:Enabled` | `true` | Allow the home-page fallback (set `false` for fully offline checks) |
| `WebsiteFetcher:Timeout` / `MaxResponseBytes` / `MaxRedirects` | `00:00:03` / `262144` / `3` | Limits for the home-page fetch |
| `Cors:AllowedOrigins` | `["http://localhost:4200"]` | Browser origins allowed to call the API directly |

## Known limitations

- **No rate limiting yet (flagged improvement).** A `POST` whose domain doesn't match can trigger an outbound fetch of the submitted website. The SSRF guard keeps internal targets unreachable, but nothing throttles how often a client can make the API fetch third-party sites. Next step: ASP.NET Core's built-in rate limiter (`AddRateLimiter`, a fixed window per client on `POST /api/companies`) returning a 429 problem response, plus caching content-inspection results per registrable domain.
- **Storage is in memory.** Data is lost on restart, by design (the spec says no database). [ADR 003](docs/decisions/003-persistence.md) covers the path to a real database.
- **Sites behind bot protection can't be inspected.** Some sites (e.g. `abc.xyz`, behind a Cloudflare managed challenge) answer `403` to server-side HTTP clients. Content inspection then reports "content could not be retrieved" and only the domain match counts, so `Alphabet Inc.` + `https://abc.xyz` is rejected, with that reason in the message. We deliberately don't try to get around bot detection. [ADR 002](docs/decisions/002-relevance-strategy.md) discusses production alternatives.
- **Uniqueness is one company per registrable domain.** `shop.acme.com` and `www.acme.com` conflict.
- **Public suffixes** come from an embedded snapshot of the [Public Suffix List](https://publicsuffix.org) (MPL 2.0, version `2026-10-07`). New suffixes appear upstream over time. To refresh: `curl -o backend/src/Companies.Domain/Companies/public_suffix_list.dat https://publicsuffix.org/list/public_suffix_list.dat`.
