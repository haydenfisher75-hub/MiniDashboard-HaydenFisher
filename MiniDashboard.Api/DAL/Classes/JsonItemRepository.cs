using System.Text.Json;
using MiniDashboard.DAL.Interfaces;

namespace MiniDashboard.DAL.Classes;

public class JsonItemRepository : IItemRepository
{
    private readonly string _filePath;
    private readonly string _deletedFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonItemRepository(string filePath, string deletedFilePath)
    {
        _filePath = filePath;
        _deletedFilePath = deletedFilePath;
    }

    private async Task<List<Item>> ReadFileAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<Item>>(json, JsonOptions) ?? [];
    }

    private async Task WriteFileAsync(List<Item> items)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private async Task<List<DeletedItem>> ReadDeletedFileAsync()
    {
        if (!File.Exists(_deletedFilePath))
            return [];

        var json = await File.ReadAllTextAsync(_deletedFilePath);
        return JsonSerializer.Deserialize<List<DeletedItem>>(json, JsonOptions) ?? [];
    }

    private async Task WriteDeletedFileAsync(List<DeletedItem> items)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions);
        await File.WriteAllTextAsync(_deletedFilePath, json);
    }

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await ReadFileAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var items = await ReadFileAsync();
            return items.FirstOrDefault(i => i.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<Item>> SearchAsync(string query)
    {
        await _lock.WaitAsync();
        try
        {
            var items = await ReadFileAsync();
            var lower = query.ToLowerInvariant();
            return items.Where(i =>
                i.Name.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                i.ProductCode.Contains(lower, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Item> AddAsync(Item item)
    {
        await _lock.WaitAsync();
        try
        {
            var items = await ReadFileAsync();
            item.Id = items.Count > 0 ? items.Max(i => i.Id) + 1 : 1;
            item.CreatedAt = DateTime.UtcNow;
            items.Add(item);
            await WriteFileAsync(items);
            return item;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Item?> UpdateAsync(Item item)
    {
        await _lock.WaitAsync();
        try
        {
            var items = await ReadFileAsync();
            var index = items.FindIndex(i => i.Id == item.Id);
            if (index == -1)
                return null;

            item.CreatedAt = items[index].CreatedAt;
            item.ProductCode = items[index].ProductCode;
            item.UpdatedAt = DateTime.UtcNow;
            items[index] = item;
            await WriteFileAsync(items);
            return item;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var items = await ReadFileAsync();
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item is null)
                return false;

            items.Remove(item);
            await WriteFileAsync(items);

            var deletedItem = new DeletedItem
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                TypeId = item.TypeId,
                CategoryId = item.CategoryId,
                ProductCode = item.ProductCode,
                Price = item.Price,
                Quantity = item.Quantity,
                Discount = item.Discount,
                DiscountDate = item.DiscountDate,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                DeletedAt = DateTime.UtcNow
            };

            var deletedItems = await ReadDeletedFileAsync();
            deletedItems.Add(deletedItem);
            await WriteDeletedFileAsync(deletedItems);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
