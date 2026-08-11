using System.ComponentModel.DataAnnotations;

namespace SmartFactory.Web.Options;

public sealed class RiskThresholdOptions
{
    public const string SectionName = "RiskThresholds";

    [Range(0, 1)]
    public decimal LowMaximum { get; init; } = 0.30m;

    [Range(0, 1)]
    public decimal MediumMaximum { get; init; } = 0.60m;

    [Range(0, 1)]
    public decimal HighMaximum { get; init; } = 0.80m;
}
