using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.BL.Interfaces;

public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllAsync();
    Task<ItemDto?> GetByIdAsync(int id);
    Task<IEnumerable<ItemDto>> SearchAsync(string query);
    Task<ItemDto> AddAsync(CreateItemDto dto);
    Task<ItemDto?> UpdateAsync(int id, UpdateItemDto dto);
    Task<bool> DeleteAsync(int id);
}
