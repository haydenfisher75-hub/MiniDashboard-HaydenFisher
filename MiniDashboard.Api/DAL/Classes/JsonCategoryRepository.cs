using System.Text.Json;
using MiniDashboard.DAL.Interfaces;

namespace MiniDashboard.DAL.Classes;

public class JsonCategoryRepository : ICategoryRepository
{
    private readonly string _filePath;
    private List<Category>? _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonCategoryRepository(string filePath)
    {
        _filePath = filePath;
    }

    private async Task<List<Category>> LoadAsync()
    {
        if (_cache is not null)
            return _cache;

        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        _cache = JsonSerializer.Deserialize<List<Category>>(json, JsonOptions) ?? [];
        return _cache;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
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

    public async Task<Category?> GetByIdAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var categories = await LoadAsync();
            return categories.FirstOrDefault(c => c.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<Category>> GetByTypeIdAsync(int typeId)
    {
        await _lock.WaitAsync();
        try
        {
            var categories = await LoadAsync();
            return categories.Where(c => c.TypeId == typeId);
        }
        finally
        {
            _lock.Release();
        }
    }
}
