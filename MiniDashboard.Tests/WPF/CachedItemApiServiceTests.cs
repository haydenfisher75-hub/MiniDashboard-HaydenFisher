using System.IO;
using System.Net.Http;
using FluentAssertions;
using MiniDashboard.DTOs.Classes;
using MiniDashboard.App.Services;
using Moq;
using Xunit;

namespace MiniDashboard.Tests.WPF;

public class CachedItemApiServiceTests : IDisposable
{
    private readonly Mock<IItemApiService> _mockInner = new();
    private readonly string _cacheDir;
    private readonly CachedItemApiService _service;

    public CachedItemApiServiceTests()
    {
        _cacheDir = Path.Combine(Path.GetTempPath(), "CacheTests_" + Guid.NewGuid().ToString("N"));
        _service = new CachedItemApiService(_mockInner.Object, _cacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDir))
        {
            try { Directory.Delete(_cacheDir, true); }
            catch { }
        }
    }

    #region GetAllItemsAsync

    [Fact]
    public async Task GetAllItemsAsync_WhenOnline_ReturnsFromApi()
    {
        var items = new List<ItemDto> { new() { Id = 1, Name = "Phone" } };
        _mockInner.Setup(s => s.GetAllItemsAsync()).ReturnsAsync(items);

        var result = await _service.GetAllItemsAsync();

        result.Should().BeEquivalentTo(items);
        _service.IsOffline.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllItemsAsync_WhenOnline_CachesResult()
    {
        var items = new List<ItemDto> { new() { Id = 1, Name = "Phone" } };
        _mockInner.Setup(s => s.GetAllItemsAsync()).ReturnsAsync(items);

        await _service.GetAllItemsAsync();

        // Give fire-and-forget cache write time to complete
        await Task.Delay(200);

        var cachePath = Path.Combine(_cacheDir, "items.json");
        File.Exists(cachePath).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllItemsAsync_WhenOffline_ReturnsCachedData()
    {
        var items = new List<ItemDto> { new() { Id = 1, Name = "Cached Phone" } };
        _mockInner.Setup(s => s.GetAllItemsAsync()).ReturnsAsync(items);

        // First call: online, caches data
        await _service.GetAllItemsAsync();
        await Task.Delay(200);

        // Second call: offline
        _mockInner.Setup(s => s.GetAllItemsAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.GetAllItemsAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Cached Phone");
        _service.IsOffline.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllItemsAsync_WhenOffline_NoCacheFile_ReturnsEmptyList()
    {
        _mockInner.Setup(s => s.GetAllItemsAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.GetAllItemsAsync();

        result.Should().BeEmpty();
        _service.IsOffline.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllItemsAsync_AfterOfflineThenOnline_SetsIsOfflineFalse()
    {
        _mockInner.Setup(s => s.GetAllItemsAsync()).ThrowsAsync(new HttpRequestException());
        await _service.GetAllItemsAsync();
        _service.IsOffline.Should().BeTrue();

        _mockInner.Setup(s => s.GetAllItemsAsync()).ReturnsAsync(new List<ItemDto>());
        await _service.GetAllItemsAsync();
        _service.IsOffline.Should().BeFalse();
    }

    #endregion

    #region GetAllTypesAsync

    [Fact]
    public async Task GetAllTypesAsync_WhenOnline_ReturnsFromApi()
    {
        var types = new List<TypeDto> { new() { Id = 1, Name = "Electronics" } };
        _mockInner.Setup(s => s.GetAllTypesAsync()).ReturnsAsync(types);

        var result = await _service.GetAllTypesAsync();

        result.Should().BeEquivalentTo(types);
    }

    [Fact]
    public async Task GetAllTypesAsync_WhenOffline_ReturnsCached()
    {
        var types = new List<TypeDto> { new() { Id = 1, Name = "Electronics" } };
        _mockInner.Setup(s => s.GetAllTypesAsync()).ReturnsAsync(types);
        await _service.GetAllTypesAsync();
        await Task.Delay(200);

        _mockInner.Setup(s => s.GetAllTypesAsync()).ThrowsAsync(new HttpRequestException());
        var result = await _service.GetAllTypesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Electronics");
    }

    #endregion

    #region GetCategoriesAsync

    [Fact]
    public async Task GetCategoriesAsync_WithoutTypeId_UsesCategoriesJson()
    {
        var categories = new List<CategoryDto> { new() { Id = 1, Name = "Phones" } };
        _mockInner.Setup(s => s.GetCategoriesAsync(null)).ReturnsAsync(categories);

        var result = await _service.GetCategoriesAsync();

        result.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public async Task GetCategoriesAsync_WithTypeId_UsesSeparateCacheFile()
    {
        var categories = new List<CategoryDto> { new() { Id = 1, Name = "Phones", TypeId = 1 } };
        _mockInner.Setup(s => s.GetCategoriesAsync(1)).ReturnsAsync(categories);

        await _service.GetCategoriesAsync(1);
        await Task.Delay(200);

        var cachePath = Path.Combine(_cacheDir, "categories-1.json");
        File.Exists(cachePath).Should().BeTrue();
    }

    #endregion

    #region Write operations (pass-through)

    [Fact]
    public async Task CreateItemAsync_DelegatesToInner()
    {
        var dto = new CreateItemDto { Name = "New" };
        var created = new ItemDto { Id = 1, Name = "New" };
        _mockInner.Setup(s => s.CreateItemAsync(dto)).ReturnsAsync(created);

        var result = await _service.CreateItemAsync(dto);

        result.Should().Be(created);
        _mockInner.Verify(s => s.CreateItemAsync(dto), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_DelegatesToInner()
    {
        var dto = new UpdateItemDto { Name = "Updated" };
        var updated = new ItemDto { Id = 1, Name = "Updated" };
        _mockInner.Setup(s => s.UpdateItemAsync(1, dto)).ReturnsAsync(updated);

        var result = await _service.UpdateItemAsync(1, dto);

        result.Should().Be(updated);
        _mockInner.Verify(s => s.UpdateItemAsync(1, dto), Times.Once);
    }

    [Fact]
    public async Task DeleteItemAsync_DelegatesToInner()
    {
        _mockInner.Setup(s => s.DeleteItemAsync(1)).ReturnsAsync(true);

        var result = await _service.DeleteItemAsync(1);

        result.Should().BeTrue();
        _mockInner.Verify(s => s.DeleteItemAsync(1), Times.Once);
    }

    #endregion
}
