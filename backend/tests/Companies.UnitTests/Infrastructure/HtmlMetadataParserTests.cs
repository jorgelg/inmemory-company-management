using Companies.Infrastructure.WebContent;

namespace Companies.UnitTests.Infrastructure;

[TestFixture]
public sealed class HtmlMetadataParserTests
{
    [Test]
    public void Parse_ExtractsTitleSiteNameAndFirstHeading()
    {
        const string html = """
            <html><head>
              <title>  Alphabet
                 Inc. </title>
              <meta property="og:site_name" content="Alphabet">
            </head><body>
              <h1 class="hero">Welcome to <span>Alphabet</span></h1>
              <h1>Second</h1>
            </body></html>
            """;

        var content = HtmlMetadataParser.Parse(html);

        content.ShouldSatisfyAllConditions(
            c => c.Title.ShouldBe("Alphabet Inc."),
            c => c.SiteName.ShouldBe("Alphabet"),
            c => c.Heading.ShouldBe("Welcome to Alphabet"));
    }

    [Test]
    public void Parse_SiteNameWithContentBeforeProperty()
    {
        var content = HtmlMetadataParser.Parse("""<meta content='Blue Bottle' property='og:site_name' />""");

        content.SiteName.ShouldBe("Blue Bottle");
    }

    [Test]
    public void Parse_DecodesHtmlEntities()
    {
        var content = HtmlMetadataParser.Parse("<title>AT&amp;T &#8211; Home</title>");

        content.Title.ShouldBe("AT&T – Home");
    }

    [Test]
    public void Parse_MissingElements_ReturnsNulls()
    {
        var content = HtmlMetadataParser.Parse("<html><body><p>Nothing here</p></body></html>");

        content.ShouldSatisfyAllConditions(
            c => c.Title.ShouldBeNull(),
            c => c.SiteName.ShouldBeNull(),
            c => c.Heading.ShouldBeNull());
    }

    [Test]
    public void Parse_TruncatesVeryLongValues()
    {
        var content = HtmlMetadataParser.Parse($"<title>{new string('a', 1000)}</title>");

        content.Title.ShouldNotBeNull().Length.ShouldBe(HtmlMetadataParser.MaxValueLength);
    }
}
