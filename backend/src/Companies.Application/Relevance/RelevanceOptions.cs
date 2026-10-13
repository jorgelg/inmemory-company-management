using System.ComponentModel.DataAnnotations;

namespace Companies.Application.Relevance;

/// <summary>Configuration section <c>Relevance</c>.</summary>
public sealed class RelevanceOptions
{
    public const string SectionName = "Relevance";

    /// <summary>Minimum score in [0, 1] for a name to count as relevant to its website.</summary>
    [Range(0.0, 1.0)]
    public double Threshold { get; set; } = 0.6;

    public ContentInspectionOptions ContentInspection { get; set; } = new();
}

