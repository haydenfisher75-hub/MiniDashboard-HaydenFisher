using MiniDashboard.DAL.Classes;

namespace MiniDashboard.DAL.Interfaces;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync();
    Task<Item?> GetByIdAsync(int id);
    Task<IEnumerable<Item>> SearchAsync(string query);
    Task<Item> AddAsync(Item item);
    Task<Item?> UpdateAsync(Item item);
    Task<bool> DeleteAsync(int id);
}
