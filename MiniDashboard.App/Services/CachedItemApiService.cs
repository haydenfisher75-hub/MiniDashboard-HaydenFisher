using System.IO;
using System.Net.Http;
using System.Text.Json;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.App.Services;

public class CachedItemApiService : IItemApiService
{
    private readonly IItemApiService _inner;
    private readonly string _cacheDir;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public bool IsOffline { get; private set; }

    public CachedItemApiService(IItemApiService inner, string cacheDir)
    {
        _inner = inner;
        _cacheDir = cacheDir;
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<List<ItemDto>> GetAllItemsAsync()
    {
        return await GetWithCacheAsync(
            () => _inner.GetAllItemsAsync(),
            "items.json");
    }

    public async Task<List<TypeDto>> GetAllTypesAsync()
    {
        return await GetWithCacheAsync(
            () => _inner.GetAllTypesAsync(),
            "types.json");
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(int? typeId = null)
    {
        var cacheFile = typeId.HasValue ? $"categories-{typeId}.json" : "categories.json";
        return await GetWithCacheAsync(
            () => _inner.GetCategoriesAsync(typeId),
            cacheFile);
    }

    public Task<ItemDto> CreateItemAsync(CreateItemDto dto) => _inner.CreateItemAsync(dto);
    public Task<ItemDto?> UpdateItemAsync(int id, UpdateItemDto dto) => _inner.UpdateItemAsync(id, dto);
    public Task<bool> DeleteItemAsync(int id) => _inner.DeleteItemAsync(id);

    private async Task<List<T>> GetWithCacheAsync<T>(Func<Task<List<T>>> apiCall, string fileName)
    {
        try
        {
            var result = await apiCall();
            IsOffline = false;
            _ = WriteCacheAsync(fileName, result);
            return result;
        }
        catch (HttpRequestException)
        {
            IsOffline = true;
            return await ReadCacheAsync<T>(fileName);
        }
    }

    private async Task WriteCacheAsync<T>(string fileName, List<T> data)
    {
        await _lock.WaitAsync();
        try
        {
            var path = Path.Combine(_cacheDir, fileName);
            var json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(path, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<T>> ReadCacheAsync<T>(string fileName)
    {
        var path = Path.Combine(_cacheDir, fileName);
        if (!File.Exists(path))
            return [];

        await _lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
        }
        finally
        {
            _lock.Release();
        }
    }
}
