namespace Companies.Application.Relevance;

/// <summary>Configuration section <c>Relevance:ContentInspection</c>.</summary>
public sealed class ContentInspectionOptions
{
    /// <summary>When the domain doesn't match, fetch the home page and look for the name in it.</summary>
    public bool Enabled { get; set; } = true;
}
