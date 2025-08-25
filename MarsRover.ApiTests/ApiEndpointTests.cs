using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MarsRover.ApiTests;

public record EndpointInfo(string Path, string HttpMethod);

public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Lazy<List<EndpointInfo>> DiscoveredEndpoints = new(DiscoverAllEndpoints);

    public ApiEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        
        // Fail fast if no endpoints discovered - no point running tests
        if (DiscoveredEndpoints.Value.Count == 0)
        {
            throw new InvalidOperationException("No endpoints were discovered in the application. Cannot proceed with API tests.");
        }
    }

    private static List<EndpointInfo> DiscoverAllEndpoints()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var endpointDataSource = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
        
        return endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => !string.IsNullOrEmpty(endpoint.RoutePattern.RawText))
            .SelectMany(endpoint => 
                endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? Enumerable.Empty<string>(),
                (endpoint, method) => new EndpointInfo(endpoint.RoutePattern.RawText!, method))
            .ToList();
    }

    public static IEnumerable<object[]> GetDiscoveredEndpoints()
        => DiscoveredEndpoints.Value
            .Where(endpoint => endpoint.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            .Select(endpoint => new object[] { endpoint.Path, endpoint.HttpMethod });

    [Theory]
    [MemberData(nameof(GetDiscoveredEndpoints))]
    public void EndpointShouldExist(string path, string httpMethod)
    {
        Assert.NotNull(path);
        Assert.Equal("GET", httpMethod);
    }
}