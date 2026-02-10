using MiniDashboard.BL.Interfaces;
using MiniDashboard.DAL.Classes;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.BL.Classes;

public class ItemMapper : IItemMapper
{
    public ItemDto ToDto(Item item, string typeName, string categoryName)
    {
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            TypeId = item.TypeId,
            TypeName = typeName,
            CategoryId = item.CategoryId,
            CategoryName = categoryName,
            ProductCode = item.ProductCode,
            Price = item.Price,
            Quantity = item.Quantity,
            Discount = item.Discount,
            DiscountDate = item.DiscountDate,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    public Item ToEntity(CreateItemDto dto)
    {
        return new Item
        {
            Name = dto.Name,
            Description = dto.Description,
            TypeId = dto.TypeId,
            CategoryId = dto.CategoryId,
            Price = dto.Price,
            Quantity = dto.Quantity,
            Discount = dto.Discount,
            DiscountDate = dto.Discount > 0 ? DateTime.UtcNow : null
        };
    }

    public Item ToEntity(int id, UpdateItemDto dto)
    {
        return new Item
        {
            Id = id,
            Name = dto.Name,
            Description = dto.Description,
            TypeId = dto.TypeId,
            CategoryId = dto.CategoryId,
            Price = dto.Price,
            Quantity = dto.Quantity,
            Discount = dto.Discount
        };
    }
}
