using MiniDashboard.DAL.Classes;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.BL.Interfaces;

public interface IItemMapper
{
    ItemDto ToDto(Item item, string typeName, string categoryName);
    Item ToEntity(CreateItemDto dto);
    Item ToEntity(int id, UpdateItemDto dto);
}
