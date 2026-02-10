using System.Text.Json;
using MiniDashboard.DAL.Interfaces;

namespace MiniDashboard.DAL.Classes;

public class JsonTypeRepository : ITypeRepository
{
    private readonly string _filePath;
    private List<ItemType>? _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonTypeRepository(string filePath)
    {
        _filePath = filePath;
    }

    private async Task<List<ItemType>> LoadAsync()
    {
        if (_cache is not null)
            return _cache;

        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        _cache = JsonSerializer.Deserialize<List<ItemType>>(json, JsonOptions) ?? [];
        return _cache;
    }

    public async Task<IEnumerable<ItemType>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await LoadAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ItemType?> GetByIdAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var types = await LoadAsync();
            return types.FirstOrDefault(t => t.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }
}
