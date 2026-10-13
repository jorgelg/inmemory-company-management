using Companies.Application.Relevance;
using Companies.Application.Text;
using Companies.Domain.Companies;

namespace Companies.UnitTests.Application.Relevance;

internal static class RelevanceTestData
{
    public const double Threshold = 0.6;

    public static RelevanceCandidate Candidate(string name, string website)
    {
        var companyName = CompanyName.Create(name).Value;
        var websiteUrl = WebsiteUrl.Create(website).Value;

        return new RelevanceCandidate(companyName, websiteUrl, CompanyNameNormalizer.Normalize(companyName));
    }
}
