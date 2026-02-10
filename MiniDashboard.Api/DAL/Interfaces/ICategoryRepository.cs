using MiniDashboard.DAL.Classes;

namespace MiniDashboard.DAL.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<IEnumerable<Category>> GetByTypeIdAsync(int typeId);
}
