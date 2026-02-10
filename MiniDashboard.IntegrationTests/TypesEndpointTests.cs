using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MiniDashboard.DTOs.Classes;
using Xunit;

namespace MiniDashboard.IntegrationTests;

public class TypesEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TypesEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsAllSeededTypes()
    {
        var response = await _client.GetAsync("/api/types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var types = await response.Content.ReadFromJsonAsync<List<TypeDto>>(JsonOptions);
        types.Should().NotBeNull();
        types!.Should().HaveCount(2);
        types.Should().Contain(t => t.Name == "Electronics");
        types.Should().Contain(t => t.Name == "Furniture");
    }
}
