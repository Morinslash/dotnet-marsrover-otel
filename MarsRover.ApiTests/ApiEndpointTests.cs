using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MarsRover.ApiTests;

public record EndpointInfo(string Path, string HttpMethod);

public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private static readonly Lazy<List<EndpointInfo>> AllDiscoveredEndpoints = new(DiscoverAllEndpoints);

    public ApiEndpointTests(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;

        // Fail fast if no endpoints discovered - no point running tests
        if (AllDiscoveredEndpoints.Value.Count == 0)
        {
            throw new InvalidOperationException(
                "No endpoints were discovered in the application. Cannot proceed with API tests.");
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

    public static IEnumerable<object[]> DiscoveredGetEndpoints()
        => AllDiscoveredEndpoints.Value
            .Where(endpoint => endpoint.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            .Select(endpoint => new object[] { endpoint.Path, endpoint.HttpMethod });

    [Theory]
    [MemberData(nameof(DiscoveredGetEndpoints))]
    public async Task EndpointShouldReturnSuccess(string path, string httpMethod)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [MemberData(nameof(DiscoveredGetEndpoints))]
    public async Task GetEndpoint_ShouldRespondWithinReasonableTime(string path, string httpMethod)
    {
        var client = _factory.CreateClient();
        var stopwatch = Stopwatch.StartNew();

        var response = await client.GetAsync(path);

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 second timeout
    }
    
    [Theory]
    [MemberData(nameof(DiscoveredGetEndpoints))]
    public async Task GetEndpoint_ShouldHaveProperHeaders(string path, string httpMethod)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
    
        response.EnsureSuccessStatusCode();
    
        var allHeaders = response.Headers
            .Concat(response.Content.Headers)
            .ToList();
    
    
        allHeaders.Should().Contain(h => h.Key.Contains("Content-Type", StringComparison.OrdinalIgnoreCase));
    }
    // This is not working on the TestSerever as it has limited scope of headers, not simulating production
    // [Theory]
    // [MemberData(nameof(DiscoveredGetEndpoints))]
    // public async Task GetEndpoint_ShouldNotHaveHeaders(string path, string httpMethod)
    // {
    //     var client = _factory.CreateClient();
    //     var response = await client.GetAsync(path);
    //
    //     response.EnsureSuccessStatusCode();
    //     // Check both response headers and content headers for security-risky headers
    //     var allHeaders = response.Headers
    //         .Concat(response.Content.Headers)
    //         .ToList();
    //
    //     var headerNames = allHeaders.Select(h => h.Key).ToList();
    //     // This will help us see what headers exist
    //     _testOutputHelper.WriteLine($"All headers for {path}: {string.Join(", ", headerNames)}");
    //
    //
    //     allHeaders.Should().NotContain(
    //         h => h.Key.Equals("X-Powered-By", StringComparison.OrdinalIgnoreCase),
    //         "X-Powered-By header exposes server technology and should be removed for security");
    //
    //     allHeaders.Should().NotContain(
    //         h => h.Key.Equals("Server", StringComparison.OrdinalIgnoreCase),
    //         "Server header exposes server information and should be removed for security");
    // }

    [Theory]
    [MemberData(nameof(DiscoveredGetEndpoints))]
    public async Task GetEndpoint_ShouldReturnValidContentType(string path, string httpMethod)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentLength.Should().BeGreaterThan(0);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.Should().BeOneOf("application/json", "text/plain", "text/html");
    }
}