using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MiniDashboard.DTOs.Classes;
using Xunit;

namespace MiniDashboard.IntegrationTests;

public class CategoriesEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CategoriesEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsAllSeededCategories()
    {
        var response = await _client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
        categories.Should().NotBeNull();
        categories!.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAll_WithTypeId_ReturnsFilteredCategories()
    {
        var response = await _client.GetAsync("/api/categories?typeId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
        categories.Should().NotBeNull();
        categories!.Should().HaveCount(2);
        categories.Should().OnlyContain(c => c.TypeId == 1);
    }

    [Fact]
    public async Task GetAll_WithNonExistentTypeId_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/categories?typeId=999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
        categories.Should().NotBeNull();
        categories!.Should().BeEmpty();
    }
}
