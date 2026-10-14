using Companies.Application.Companies;
using Companies.Application.Companies.Contracts;
using Companies.Application.Relevance;
using Companies.Application.Search;
using Companies.Domain.Common;
using Companies.Domain.Companies;
using Companies.UnitTests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Companies.UnitTests.Application.Companies;

[TestFixture]
public sealed class CompanyServiceCreateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private ICompanyRepository _repository = null!;
    private IRelevanceValidator _relevance = null!;
    private FakeLogger<CompanyService> _logger = null!;
    private CompanyService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ICompanyRepository>();
        _repository.TryAddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>()).Returns(true);
        _relevance = Substitute.For<IRelevanceValidator>();
        GivenRelevance(isRelevant: true);
        _logger = new FakeLogger<CompanyService>();
        _service = new CompanyService(_repository, _relevance, new CompanySearchRanker(), new FakeTimeProvider(Now), _logger);
    }

    [Test]
    public async Task Create_ValidRequest_StoresCompanyAndReturnsResponse()
    {
        var result = await _service.CreateAsync(Request(" Verus  LLC ", "https://WWW.Verus.com/"), CancellationToken.None);

        var response = result.ShouldBeSuccess();
        response.ShouldSatisfyAllConditions(
            r => r.Id.ShouldNotBe(Guid.Empty),
            r => r.Name.ShouldBe("Verus LLC"),
            r => r.Website.ShouldBe("https://www.verus.com"),
            r => r.Domain.ShouldBe("verus.com"),
            r => r.CreatedAt.ShouldBe(Now),
            r => r.RelevanceScore.ShouldBeNull());
        await _repository.Received(1).TryAddAsync(
            Arg.Is<Company>(c => c.Id == response.Id && c.Name.Value == "Verus LLC"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Create_ValidRequest_LogsCreation()
    {
        await _service.CreateAsync(Request("Verus LLC", "https://www.verus.com"), CancellationToken.None);

        var record = _logger.Collector.LatestRecord;
        record.ShouldSatisfyAllConditions(
            r => r.Level.ShouldBe(LogLevel.Information),
            r => r.Message.ShouldContain("verus.com"));
    }

    [TestCase(null, "company_name.required")]
    [TestCase("A", "company_name.too_short")]
    public async Task Create_InvalidName_FailsWithoutSideEffects(string? name, string code)
    {
        var result = await _service.CreateAsync(Request(name, "https://www.verus.com"), CancellationToken.None);

        result.ShouldBeValidationError(code, "name");
        await AssertNothingEvaluatedOrStored();
    }

    [TestCase("not a url", "website.malformed")]
    [TestCase("https://localhost", "website.invalid_host")]
    public async Task Create_InvalidWebsite_FailsWithoutSideEffects(string website, string code)
    {
        var result = await _service.CreateAsync(Request("Verus LLC", website), CancellationToken.None);

        result.ShouldBeValidationError(code, "website");
        await AssertNothingEvaluatedOrStored();
    }

    [Test]
    public async Task Create_InvalidRequest_LogsWarning()
    {
        await _service.CreateAsync(Request("A", "https://www.verus.com"), CancellationToken.None);

        _logger.Collector.LatestRecord.Level.ShouldBe(LogLevel.Warning);
    }

    [Test]
    public async Task Create_DuplicateDomain_FailsWithConflictBeforeRelevanceCheck()
    {
        _repository.ExistsByDomainAsync("verus.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.CreateAsync(Request("Verus LLC", "https://shop.verus.com"), CancellationToken.None);

        var error = result.ShouldBeFailure();
        error.ShouldSatisfyAllConditions(
            e => e.Type.ShouldBe(ErrorType.Conflict),
            e => e.Code.ShouldBe("company.duplicate_website"),
            e => e.Target.ShouldBe("website"),
            e => e.Message.ShouldContain("verus.com"));
        await AssertNothingEvaluatedOrStored();
    }

    [Test]
    public async Task Create_NameNotRelevantToWebsite_FailsWithExplanation()
    {
        GivenRelevance(isRelevant: false, score: 0.12, reason: "domain label 'example' does not resemble the name");

        var result = await _service.CreateAsync(Request("Acme", "https://www.example.com"), CancellationToken.None);

        var error = result.ShouldBeFailure();
        error.ShouldSatisfyAllConditions(
            e => e.Type.ShouldBe(ErrorType.Validation),
            e => e.Code.ShouldBe("company.not_relevant"),
            e => e.Message.ShouldContain("'Acme'"),
            e => e.Message.ShouldContain("example.com"),
            e => e.Message.ShouldContain("does not resemble the name"),
            e => e.Message.ShouldContain("0.12"),
            e => e.Message.ShouldContain("0.60"));
        await _repository.DidNotReceiveWithAnyArgs().TryAddAsync(default!, default);
        _logger.Collector.LatestRecord.Level.ShouldBe(LogLevel.Warning);
    }

    [Test]
    public async Task Create_NotRelevant_ExplainsEveryStrategyThatRan()
    {
        RelevanceEvidence domain = new("domain-match", 0.0, "domain label 'abc' does not resemble the name");
        RelevanceEvidence content = new("content-inspection", 0.0, "website content could not be retrieved");
        _relevance.EvaluateAsync(Arg.Any<CompanyName>(), Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>())
            .Returns(new RelevanceVerdict(false, 0.6, domain, [domain, content]));

        var result = await _service.CreateAsync(Request("Alphabet Inc.", "https://abc.xyz"), CancellationToken.None);

        result.ShouldBeFailure().Message.ShouldBe(
            "Company name 'Alphabet Inc.' does not appear to be related to website 'abc.xyz': " +
            "domain label 'abc' does not resemble the name; website content could not be retrieved " +
            "(best score 0.00, threshold 0.60).");
    }

    [Test]
    public async Task Create_WhenDomainTakenConcurrently_FailsWithConflict()
    {
        _repository.TryAddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.CreateAsync(Request("Verus LLC", "https://www.verus.com"), CancellationToken.None);

        result.ShouldBeFailure().Code.ShouldBe("company.duplicate_website");
    }

    [Test]
    public async Task Create_NullRequest_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _service.CreateAsync(null!, CancellationToken.None));
    }

    private static CreateCompanyRequest Request(string? name, string? website) => new() { Name = name, Website = website };

    private void GivenRelevance(bool isRelevant, double score = 1.0, string reason = "domain label matches the name exactly")
    {
        var evidence = new RelevanceEvidence("domain-match", score, reason);
        _relevance.EvaluateAsync(Arg.Any<CompanyName>(), Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>())
            .Returns(new RelevanceVerdict(isRelevant, 0.6, evidence, [evidence]));
    }

    private async Task AssertNothingEvaluatedOrStored()
    {
        await _relevance.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default!, default);
        await _repository.DidNotReceiveWithAnyArgs().TryAddAsync(default!, default);
    }
}
