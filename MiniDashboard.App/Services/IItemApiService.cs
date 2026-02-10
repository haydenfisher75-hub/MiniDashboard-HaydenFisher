using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.App.Services;

public interface IItemApiService
{
    Task<List<ItemDto>> GetAllItemsAsync();
    Task<ItemDto> CreateItemAsync(CreateItemDto dto);
    Task<ItemDto?> UpdateItemAsync(int id, UpdateItemDto dto);
    Task<bool> DeleteItemAsync(int id);
    Task<List<TypeDto>> GetAllTypesAsync();
    Task<List<CategoryDto>> GetCategoriesAsync(int? typeId = null);
}
