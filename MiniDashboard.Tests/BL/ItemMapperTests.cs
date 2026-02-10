using FluentAssertions;
using MiniDashboard.BL.Classes;
using Xunit;
using MiniDashboard.DAL.Classes;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.Tests.BL;

public class ItemMapperTests
{
    private readonly ItemMapper _mapper = new();

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var item = new Item
        {
            Id = 1,
            Name = "Test Item",
            Description = "Test Description",
            TypeId = 2,
            CategoryId = 3,
            ProductCode = "PHN-001",
            Price = 99.99m,
            Quantity = 10,
            Discount = 15,
            DiscountDate = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _mapper.ToDto(item, "Electronics", "Phones");

        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Item");
        result.Description.Should().Be("Test Description");
        result.TypeId.Should().Be(2);
        result.TypeName.Should().Be("Electronics");
        result.CategoryId.Should().Be(3);
        result.CategoryName.Should().Be("Phones");
        result.ProductCode.Should().Be("PHN-001");
        result.Price.Should().Be(99.99m);
        result.Quantity.Should().Be(10);
        result.Discount.Should().Be(15);
        result.DiscountDate.Should().Be(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        result.CreatedAt.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.UpdatedAt.Should().Be(new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToEntity_FromCreateItemDto_MapsCorrectly()
    {
        var dto = new CreateItemDto
        {
            Name = "New Item",
            Description = "New Description",
            TypeId = 1,
            CategoryId = 2,
            Price = 49.99m,
            Quantity = 5,
            Discount = 0
        };

        var result = _mapper.ToEntity(dto);

        result.Name.Should().Be("New Item");
        result.Description.Should().Be("New Description");
        result.TypeId.Should().Be(1);
        result.CategoryId.Should().Be(2);
        result.Price.Should().Be(49.99m);
        result.Quantity.Should().Be(5);
        result.Discount.Should().Be(0);
        result.ProductCode.Should().BeEmpty();
    }

    [Fact]
    public void ToEntity_FromCreateItemDto_WithDiscount_SetsDiscountDate()
    {
        var dto = new CreateItemDto
        {
            Name = "Discounted",
            Description = "Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1,
            Discount = 25
        };

        var result = _mapper.ToEntity(dto);

        result.DiscountDate.Should().NotBeNull();
        result.DiscountDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ToEntity_FromCreateItemDto_WithoutDiscount_DiscountDateIsNull()
    {
        var dto = new CreateItemDto
        {
            Name = "No Discount",
            Description = "Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1,
            Discount = 0
        };

        var result = _mapper.ToEntity(dto);

        result.DiscountDate.Should().BeNull();
    }

    [Fact]
    public void ToEntity_FromUpdateItemDto_MapsIdAndProperties()
    {
        var dto = new UpdateItemDto
        {
            Name = "Updated",
            Description = "Updated Desc",
            TypeId = 3,
            CategoryId = 4,
            Price = 75m,
            Quantity = 20,
            Discount = 10
        };

        var result = _mapper.ToEntity(42, dto);

        result.Id.Should().Be(42);
        result.Name.Should().Be("Updated");
        result.Description.Should().Be("Updated Desc");
        result.TypeId.Should().Be(3);
        result.CategoryId.Should().Be(4);
        result.Price.Should().Be(75m);
        result.Quantity.Should().Be(20);
        result.Discount.Should().Be(10);
    }
}
