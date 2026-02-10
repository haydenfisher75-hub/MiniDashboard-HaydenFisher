using MiniDashboard.BL.Interfaces;
using MiniDashboard.DAL.Interfaces;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.BL.Classes;

public class ItemService : IItemService
{
    private readonly IItemRepository _repository;
    private readonly ITypeRepository _typeRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IItemMapper _mapper;

    public ItemService(IItemRepository repository, ITypeRepository typeRepository, ICategoryRepository categoryRepository, IItemMapper mapper)
    {
        _repository = repository;
        _typeRepository = typeRepository;
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ItemDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        var (typeLookup, categoryLookup) = await GetLookupsAsync();
        return items.Select(i => MapToDto(i, typeLookup, categoryLookup));
    }

    public async Task<ItemDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return null;
        var (typeLookup, categoryLookup) = await GetLookupsAsync();
        return MapToDto(item, typeLookup, categoryLookup);
    }

    public async Task<IEnumerable<ItemDto>> SearchAsync(string query)
    {
        var items = string.IsNullOrWhiteSpace(query)
            ? await _repository.GetAllAsync()
            : await _repository.SearchAsync(query);

        var (typeLookup, categoryLookup) = await GetLookupsAsync();
        return items.Select(i => MapToDto(i, typeLookup, categoryLookup));
    }

    public async Task<ItemDto> AddAsync(CreateItemDto dto)
    {
        var items = await _repository.GetAllAsync();

        await ValidateUniquenessAsync(items, dto.Name, dto.Description);

        var entity = _mapper.ToEntity(dto);
        entity.ProductCode = await GenerateProductCodeAsync(dto.CategoryId, items);

        var created = await _repository.AddAsync(entity);
        var (typeLookup, categoryLookup) = await GetLookupsAsync();
        return MapToDto(created, typeLookup, categoryLookup);
    }

    public async Task<ItemDto?> UpdateAsync(int id, UpdateItemDto dto)
    {
        var items = await _repository.GetAllAsync();

        await ValidateUniquenessAsync(items, dto.Name, dto.Description, id);

        var entity = _mapper.ToEntity(id, dto);

        var existing = items.FirstOrDefault(i => i.Id == id);
        if (existing is not null)
        {
            if (dto.Discount > 0 && existing.Discount == 0)
                entity.DiscountDate = DateTime.UtcNow;
            else if (dto.Discount == 0)
                entity.DiscountDate = null;
            else
                entity.DiscountDate = existing.DiscountDate;
        }

        var updated = await _repository.UpdateAsync(entity);
        if (updated is null) return null;
        var (typeLookup, categoryLookup) = await GetLookupsAsync();
        return MapToDto(updated, typeLookup, categoryLookup);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    private async Task<(Dictionary<int, string> typeLookup, Dictionary<int, string> categoryLookup)> GetLookupsAsync()
    {
        var types = await _typeRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        var typeLookup = types.ToDictionary(t => t.Id, t => t.Name);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);
        return (typeLookup, categoryLookup);
    }

    private ItemDto MapToDto(DAL.Classes.Item item, Dictionary<int, string> typeLookup, Dictionary<int, string> categoryLookup)
    {
        var typeName = typeLookup.GetValueOrDefault(item.TypeId, "Unknown");
        var categoryName = categoryLookup.GetValueOrDefault(item.CategoryId, "Unknown");
        return _mapper.ToDto(item, typeName, categoryName);
    }

    private Task ValidateUniquenessAsync(IEnumerable<DAL.Classes.Item> items, string name, string description, int? excludeId = null)
    {
        var filtered = excludeId.HasValue
            ? items.Where(i => i.Id != excludeId.Value)
            : items;

        if (filtered.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"An item with the name '{name}' already exists.");

        if (filtered.Any(i => i.Description.Equals(description, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"An item with the same description already exists.");

        return Task.CompletedTask;
    }

    private async Task<string> GenerateProductCodeAsync(int categoryId, IEnumerable<DAL.Classes.Item> items)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId)
            ?? throw new InvalidOperationException($"Category with id {categoryId} not found.");

        var prefix = category.Prefix;

        var maxSuffix = items
            .Where(i => i.ProductCode.StartsWith(prefix + "-"))
            .Select(i =>
            {
                var parts = i.ProductCode.Split('-');
                return parts.Length == 2 && int.TryParse(parts[1], out var num) ? num : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}-{(maxSuffix + 1):D3}";
    }
}
