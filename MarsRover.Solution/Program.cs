using MarsRover.Solution.TelemetryConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => 
    { c.SwaggerDoc
        ("v1", new()
        {
            Title = "Mars Rover API",
            Version = "v1",
            Description = "API for Mars Rover"
        });
        
    });

builder.Services.AddOpenTelemetryServices(builder.Configuration);
builder.Logging.AddOpenTelemetryLogging(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mars Rover API v1");
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get weather forecast";
        operation.Description = "Retrivs a 5-day weather forecast for Mars operation";
        operation.Tags = [new Microsoft.OpenApi.Models.OpenApiTag {Name = "Weather"}];
        return operation;
    });

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}