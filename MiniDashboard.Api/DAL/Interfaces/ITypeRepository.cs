using MiniDashboard.DAL.Classes;

namespace MiniDashboard.DAL.Interfaces;

public interface ITypeRepository
{
    Task<IEnumerable<ItemType>> GetAllAsync();
    Task<ItemType?> GetByIdAsync(int id);
}
