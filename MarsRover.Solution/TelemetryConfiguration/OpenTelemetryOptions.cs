using System.ComponentModel.DataAnnotations;

namespace MarsRover.Solution.TelemetryConfiguration;

public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    [Required] public string OtlpEndpoint { get; set; } = string.Empty;
    [Required] public string ServiceName { get; set; } = string.Empty;
    [Required] public string ServiceVersion { get; set; } = string.Empty;
}