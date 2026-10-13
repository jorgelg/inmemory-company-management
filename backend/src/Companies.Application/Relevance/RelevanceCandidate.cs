using Companies.Application.Text;
using Companies.Domain.Companies;

namespace Companies.Application.Relevance;

/// <summary>What a relevance strategy evaluates: the submitted name and website, plus the name pre-normalized once.</summary>
public sealed record RelevanceCandidate(CompanyName Name, WebsiteUrl Website, NormalizedName NormalizedName);
