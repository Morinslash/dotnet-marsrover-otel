using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MarsRover.Solution.TelemetryConfiguration;

public static class OpenTelemetryConfigurationExtension
{
    public static IServiceCollection AddOpenTelemetryServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var otelOptions = GetValidatedOptions(configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(otelOptions.ServiceName, otelOptions.ServiceVersion))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(otelOptions.ServiceName)
                .AddOtlpExporter(option => { option.Endpoint = new Uri(otelOptions.OtlpEndpoint); }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                .AddHttpClientInstrumentation(options => options.RecordException = true)
                .AddOtlpExporter(options => { options.Endpoint = new Uri(otelOptions.OtlpEndpoint); }));

        return services;
    }

    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder logging,
        IConfiguration configuration)
    {
        var otelOptions = GetValidatedOptions(configuration);

        logging.AddOpenTelemetry(options =>
        {
            options.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(otelOptions.OtlpEndpoint);
            });
        });
        return logging;
    }

    private static OpenTelemetryOptions GetValidatedOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(OpenTelemetryOptions.SectionName);

        if (!section.Exists())
        {
            throw new InvalidOperationException(
                $"Configuration section '{OpenTelemetryOptions.SectionName}' is missing from appsettings.json");
        }

        var options = section.Get<OpenTelemetryOptions>();
        if (options == null)
        {
            throw new InvalidOperationException(
                $"Failed to bind configuration section '{OpenTelemetryOptions.SectionName}'");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            throw new InvalidOperationException("OpenTelemetry:OtlpEndpoint is required and cannot be empty");

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            throw new InvalidOperationException("OpenTelemetry:ServiceName is required and cannot be empty");

        if (string.IsNullOrWhiteSpace(options.ServiceVersion))
            throw new InvalidOperationException("OpenTelemetry:ServiceVersion is required and cannot be empty");

        return options;
    }
}