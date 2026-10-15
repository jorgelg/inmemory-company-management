# ADR 002: Relevance strategy (company name ↔ website URL)

- **Status:** Accepted
- **Date:** 2026-10-14

## Context

"The Company Name must be relevant to the Website URL", using "modern, reasonable, and explainable techniques (e.g. domain matching, content inspection, or equivalent logic)". A rejection must come back as a 400 with a concise message.

The hard part is that real names and domains differ in many ways:

| Name | Website | Why naive equality fails |
|---|---|---|
| Verus LLC | www.verus.com | legal suffix, `www` |
| The Blue Bottle Coffee Company | blue-bottle-coffee.com | stop words, legal suffix, hyphens |
| Smith & Jones Law | smithandjoneslaw.com | `&` vs `and` |
| International Business Machines | ibm.com | acronym |
| Acme | acmecorp.com | extra word in the domain |
| Müller GmbH | müller.de / xn--mller-kva.de | diacritics, IDN/punycode |
| Acme | acme.co.uk | multi-label public suffix |
| Hacker News | news.ycombinator.com | brand ≠ domain |

## Decision

A **pipeline of strategies** behind `IRelevanceStrategy`, run in order by `RelevanceValidator`. Evaluation stops at the first strategy whose score reaches the **threshold (0.6, configurable)**. Each strategy returns a score in [0, 1] plus a one-line reason, so every decision is explainable.

### 1. Normalization (shared)

`WebsiteUrl` canonicalizes the URL: lowercase scheme and host, **Unicode host** (so the IDN and punycode spellings compare equal), no default port, root slash or fragment. It also extracts the **registrable domain** and its **label**: `www.shop.acme.co.uk` → `acme.co.uk` → `acme`; `www.google.com.bo` → `google.com.bo` → `google`.

That split uses the **Mozilla Public Suffix List**, embedded as a snapshot in the Domain project (`Companies/public_suffix_list.dat`, version in its `// VERSION:` header):
- It includes the ICANN and private sections, so `acme.github.io` is its own registrable domain.
- It supports wildcard (`*.ck`) and exception (`!www.ck`) rules.
- An unknown TLD falls back to the default rule.
- A host that is itself a public suffix (`com.bo`, `github.io`) is rejected.

The registrable domain feeds the relevance label, uniqueness (409) and the `domain` filter, so it must be right.
- *History:* the first version used a curated list of ~45 two-label suffixes. Testing `https://google.com.bo` exposed it: `com.bo` wasn't listed, so the domain became `com.bo`, and every other `*.com.bo` company would have been rejected as a duplicate. The full list replaced the curated one, with regression tests for `com.bo` / `gob.bo` / `com.ec` / `co.ke` and an integration test proving `google.com.bo` and `tigo.com.bo` can both be registered.
- *Refresh:* `curl -o backend/src/Companies.Domain/Companies/public_suffix_list.dat https://publicsuffix.org/list/public_suffix_list.dat`, then run the tests.

`CompanyNameNormalizer` folds the name:
- lowercase, strip diacritics and apostrophes;
- join dotted initialisms ("S.A." → `sa`);
- turn other punctuation into spaces;
- strip **trailing** legal suffixes (LLC, Inc, Corp, Ltd, GmbH, AG, S.A., Co, Company, …);
- drop stop words (the, and, of, group, holdings, …).

It also derives the acronyms. If only generic words remain ("The Company LLC"), it falls back to all words.

### 2. Domain match (offline, always runs first)

The strongest signal wins:

| Score | Signal | Example |
|---|---|---|
| 1.00 | label equals the name | `verus` ↔ Verus LLC; `att` ↔ AT&T Inc. |
| 0.90 | label contains every word of the name | `smithandjoneslaw` ↔ Smith & Jones Law |
| 0.85 | label and name contain each other (≥ 3 chars) | `acmecorp` ↔ Acme; `tesla` ↔ Tesla Motors |
| 0.80 | label is the name's acronym | `ibm` ↔ International Business Machines |
| 0.30–0.80 | label contains some of the words, proportionally | `bluebottleroasters` ↔ Blue Bottle Coffee (2 of 3 → 0.63) |
| 0.00–0.80 | Jaro-Winkler similarity ≥ 0.7, scaled | `google` ↔ Gogle (0.68); `virus` ↔ Verus (0.48, rejected) |

### 3. Content inspection (fallback, only when the domain score is below the threshold)

`IWebsiteContentFetcher` reads the home page. The name is then looked for in the page's `<title>`, `og:site_name` and first `<h1>`:
- the whole name appears: **0.75**;
- otherwise **0.6 × the fraction of the name's words found**.

It can be switched off with `Relevance:ContentInspection:Enabled`. Real example: "Hacker News" ↔ `news.ycombinator.com` fails the domain match and passes on the page title.

The fetcher is treated as hostile-input I/O:
- **SSRF guard on every connection.** `SocketsHttpHandler.ConnectCallback` resolves DNS and refuses if *any* address is non-public: loopback, RFC 1918, link-local including `169.254.169.254` metadata, CGNAT, multicast/reserved, IPv6 ULA/link-local, IPv4-mapped. It then dials exactly the checked addresses, which defeats DNS rebinding.
- **Redirects followed manually**, at most 3. Each target must itself be a valid public `WebsiteUrl`, so `localhost`, IP literals and `file:` are refused.
- **Limits:** a 3 s budget across all hops, 256 KB read, HTML only, regexes with timeouts. Proxy and cookies are off.
- **Never throws.** Failures return "unavailable" and log a warning; only caller cancellation propagates.

### 4. Decision and message

The verdict carries the best evidence and every strategy that ran. A rejection is a 400 with code `company.not_relevant`:

> Company name 'Acme' does not appear to be related to website 'example.com': domain label 'example' does not resemble the name; the website's title and headings do not mention the name (best score 0.00, threshold 0.60).

## Consequences

- **Explainable and tunable.** Each rule is a line in a table, and the threshold and fallback are configuration. Unit tests pin every signal with real-world pairs, positive and negative.
- **Mostly offline and fast.** The large majority of real companies pass on the domain match without any network call. An integration test asserts that no fetch happens when the domain matches.
- **Extensible.** A new signal is a new `IRelevanceStrategy` registration. Ordering is cost order: cheap first.
- **Heuristic, not proof.** "Apple" ↔ `pineapplefarm.com` would pass the containment rule. The goal is catching obvious mismatches and typos, not verifying ownership.
- **Flagged improvement: rate limiting.** Content inspection makes the API fetch a user-supplied (public) site. The SSRF guard covers *where* we may connect, not *how often*. A per-client rate limit on `POST /api/companies` (429 problem response) and a per-domain cache of fetch results are the next steps.
- **Known limitation: bot protection.** Some sites refuse server-side clients outright. `abc.xyz` sits behind a Cloudflare managed challenge (`Cf-Mitigated: challenge`) and answers `403` to .NET's HTTP client, whatever the VPN or headers. Content inspection then reports "content could not be retrieved", and **Alphabet Inc. ↔ abc.xyz is rejected** with that reason in the message. We deliberately don't spoof browser fingerprints or solve challenges: that would mean evading the site's bot protection.

## Production alternatives

For production, the fallback could be replaced or extended with:
- **Trusted data sources**: company registries (Companies House, OpenCorporates), Clearbit/BuiltWith-style company↔domain datasets, or WHOIS/RDAP registrant organization where it isn't redacted.
- **Proof of control**: a DNS TXT record or a verification email to an address at the domain, which is the only real proof the submitter owns the website.
- **Human review queue** for low-confidence results, instead of a hard 400.
- **Embeddings / LLM judgment** as an additional strategy for brand ↔ domain pairs. It would be explainable only if it returned its reasoning alongside the score, and it costs latency and money, so it should come after the cheap checks.

## Alternatives considered

- **Exact domain equality only.** Too strict: it fails legal suffixes, acronyms and multi-word names.
- **Plain Levenshtein on the full strings.** Not explainable, and penalizes length differences ("Acme" vs "acmecorp").
- **Always fetch the page.** Slow, fragile (bot protection, timeouts), a larger SSRF surface, and unnecessary for most inputs.
