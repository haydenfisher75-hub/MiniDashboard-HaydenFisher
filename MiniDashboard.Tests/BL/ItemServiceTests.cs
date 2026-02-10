using FluentAssertions;
using MiniDashboard.BL.Classes;
using MiniDashboard.BL.Interfaces;
using MiniDashboard.DAL.Classes;
using MiniDashboard.DAL.Interfaces;
using Xunit;
using MiniDashboard.DTOs.Classes;
using Moq;

namespace MiniDashboard.Tests.BL;

public class ItemServiceTests
{
    private readonly Mock<IItemRepository> _mockItemRepo = new();
    private readonly Mock<ITypeRepository> _mockTypeRepo = new();
    private readonly Mock<ICategoryRepository> _mockCategoryRepo = new();
    private readonly Mock<IItemMapper> _mockMapper = new();
    private readonly ItemService _service;

    private readonly List<ItemType> _types =
    [
        new() { Id = 1, Name = "Electronics" },
        new() { Id = 2, Name = "Furniture" }
    ];

    private readonly List<Category> _categories =
    [
        new() { Id = 1, Name = "Phones", Prefix = "PHN", TypeId = 1 },
        new() { Id = 2, Name = "Laptops", Prefix = "LPT", TypeId = 1 },
        new() { Id = 3, Name = "Chairs", Prefix = "CHR", TypeId = 2 }
    ];

    public ItemServiceTests()
    {
        _mockTypeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(_types.AsEnumerable());
        _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(_categories.AsEnumerable());

        _service = new ItemService(_mockItemRepo.Object, _mockTypeRepo.Object, _mockCategoryRepo.Object, _mockMapper.Object);
    }

    #region GetAll / GetById / Search

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Phone", TypeId = 1, CategoryId = 1, ProductCode = "PHN-001" }
        };
        var expectedDto = new ItemDto { Id = 1, Name = "Phone", TypeName = "Electronics", CategoryName = "Phones" };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items.AsEnumerable());
        _mockMapper.Setup(m => m.ToDto(items[0], "Electronics", "Phones")).Returns(expectedDto);

        var result = await _service.GetAllAsync();

        result.Should().ContainSingle().Which.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsMappedDto()
    {
        var item = new Item { Id = 1, Name = "Phone", TypeId = 1, CategoryId = 1 };
        var expectedDto = new ItemDto { Id = 1, Name = "Phone", TypeName = "Electronics", CategoryName = "Phones" };

        _mockItemRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _mockMapper.Setup(m => m.ToDto(item, "Electronics", "Phones")).Returns(expectedDto);

        var result = await _service.GetByIdAsync(1);

        result.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _mockItemRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Item?)null);

        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_WithQuery_ReturnsMappedResults()
    {
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Phone", TypeId = 1, CategoryId = 1 }
        };
        var expectedDto = new ItemDto { Id = 1, Name = "Phone" };

        _mockItemRepo.Setup(r => r.SearchAsync("Phone")).ReturnsAsync(items.AsEnumerable());
        _mockMapper.Setup(m => m.ToDto(items[0], "Electronics", "Phones")).Returns(expectedDto);

        var result = await _service.SearchAsync("Phone");

        result.Should().ContainSingle().Which.Should().Be(expectedDto);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsAllItems()
    {
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Phone", TypeId = 1, CategoryId = 1 }
        };
        var expectedDto = new ItemDto { Id = 1, Name = "Phone" };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items.AsEnumerable());
        _mockMapper.Setup(m => m.ToDto(items[0], "Electronics", "Phones")).Returns(expectedDto);

        var result = await _service.SearchAsync("");

        result.Should().ContainSingle();
        _mockItemRepo.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_WhenValid_CreatesItemWithProductCode()
    {
        var dto = new CreateItemDto
        {
            Name = "New Phone",
            Description = "A new phone",
            TypeId = 1,
            CategoryId = 1,
            Price = 999m,
            Quantity = 5,
            Discount = 0
        };
        var entity = new Item { Name = "New Phone", CategoryId = 1 };
        var created = new Item { Id = 1, Name = "New Phone", ProductCode = "PHN-001", TypeId = 1, CategoryId = 1 };
        var expectedDto = new ItemDto { Id = 1, Name = "New Phone", ProductCode = "PHN-001" };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Item>());
        _mockMapper.Setup(m => m.ToEntity(dto)).Returns(entity);
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_categories[0]);
        _mockItemRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).ReturnsAsync(created);
        _mockMapper.Setup(m => m.ToDto(created, "Electronics", "Phones")).Returns(expectedDto);

        var result = await _service.AddAsync(dto);

        result.Should().Be(expectedDto);
        entity.ProductCode.Should().Be("PHN-001");
    }

    [Fact]
    public async Task AddAsync_WhenDuplicateName_ThrowsInvalidOperationException()
    {
        var existingItems = new List<Item>
        {
            new() { Id = 1, Name = "Existing Phone", Description = "Desc1", ProductCode = "PHN-001" }
        };
        var dto = new CreateItemDto
        {
            Name = "existing phone",
            Description = "Different description",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(existingItems.AsEnumerable());

        var act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*name*already exists*");
    }

    [Fact]
    public async Task AddAsync_WhenDuplicateDescription_ThrowsInvalidOperationException()
    {
        var existingItems = new List<Item>
        {
            new() { Id = 1, Name = "Phone", Description = "Same Description", ProductCode = "PHN-001" }
        };
        var dto = new CreateItemDto
        {
            Name = "Different Name",
            Description = "same description",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(existingItems.AsEnumerable());

        var act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*description*already exists*");
    }

    [Fact]
    public async Task AddAsync_ProductCode_IncrementsFromExisting()
    {
        var existingItems = new List<Item>
        {
            new() { Id = 1, Name = "Phone 1", Description = "D1", ProductCode = "PHN-001" },
            new() { Id = 2, Name = "Phone 2", Description = "D2", ProductCode = "PHN-002" }
        };
        var dto = new CreateItemDto
        {
            Name = "Phone 3",
            Description = "D3",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };
        var entity = new Item { Name = "Phone 3", CategoryId = 1 };
        var created = new Item { Id = 3, Name = "Phone 3", ProductCode = "PHN-003", TypeId = 1, CategoryId = 1 };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(existingItems.AsEnumerable());
        _mockMapper.Setup(m => m.ToEntity(dto)).Returns(entity);
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_categories[0]);
        _mockItemRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).ReturnsAsync(created);
        _mockMapper.Setup(m => m.ToDto(It.IsAny<Item>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ItemDto());

        await _service.AddAsync(dto);

        entity.ProductCode.Should().Be("PHN-003");
    }

    [Fact]
    public async Task AddAsync_InvalidCategoryId_ThrowsInvalidOperationException()
    {
        var dto = new CreateItemDto
        {
            Name = "New Item",
            Description = "Desc",
            TypeId = 1,
            CategoryId = 999,
            Price = 10m,
            Quantity = 1
        };
        var entity = new Item { Name = "New Item", CategoryId = 999 };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Item>());
        _mockMapper.Setup(m => m.ToEntity(dto)).Returns(entity);
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Category?)null);

        var act = () => _service.AddAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Category*not found*");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenValid_ReturnsUpdatedDto()
    {
        var existing = new Item { Id = 1, Name = "Old Name", Description = "Old Desc", TypeId = 1, CategoryId = 1, Discount = 0 };
        var dto = new UpdateItemDto
        {
            Name = "New Name",
            Description = "New Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 50m,
            Quantity = 3,
            Discount = 0
        };
        var entity = new Item { Id = 1, Name = "New Name" };
        var updated = new Item { Id = 1, Name = "New Name", TypeId = 1, CategoryId = 1 };
        var expectedDto = new ItemDto { Id = 1, Name = "New Name" };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item> { existing }.AsEnumerable());
        _mockMapper.Setup(m => m.ToEntity(1, dto)).Returns(entity);
        _mockItemRepo.Setup(r => r.UpdateAsync(entity)).ReturnsAsync(updated);
        _mockMapper.Setup(m => m.ToDto(updated, "Electronics", "Phones")).Returns(expectedDto);

        var result = await _service.UpdateAsync(1, dto);

        result.Should().Be(expectedDto);
    }

    [Fact]
    public async Task UpdateAsync_WhenItemNotFound_ReturnsNull()
    {
        var dto = new UpdateItemDto
        {
            Name = "Name",
            Description = "Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };
        var entity = new Item { Id = 999, Name = "Name" };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Item>());
        _mockMapper.Setup(m => m.ToEntity(999, dto)).Returns(entity);
        _mockItemRepo.Setup(r => r.UpdateAsync(entity)).ReturnsAsync((Item?)null);

        var result = await _service.UpdateAsync(999, dto);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ExcludesSelf()
    {
        var existing = new Item { Id = 1, Name = "Widget", Description = "Desc", TypeId = 1, CategoryId = 1, Discount = 0 };
        var dto = new UpdateItemDto
        {
            Name = "Widget",
            Description = "Updated Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };
        var entity = new Item { Id = 1, Name = "Widget" };
        var updated = new Item { Id = 1, Name = "Widget", TypeId = 1, CategoryId = 1 };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item> { existing }.AsEnumerable());
        _mockMapper.Setup(m => m.ToEntity(1, dto)).Returns(entity);
        _mockItemRepo.Setup(r => r.UpdateAsync(entity)).ReturnsAsync(updated);
        _mockMapper.Setup(m => m.ToDto(updated, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ItemDto { Id = 1, Name = "Widget" });

        var result = await _service.UpdateAsync(1, dto);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_OtherItem_Throws()
    {
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Item A", Description = "Desc A", ProductCode = "PHN-001" },
            new() { Id = 2, Name = "Item B", Description = "Desc B", ProductCode = "PHN-002" }
        };
        var dto = new UpdateItemDto
        {
            Name = "Item A",
            Description = "Desc B Updated",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items.AsEnumerable());

        var act = () => _service.UpdateAsync(2, dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*name*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_DiscountApplied_SetsDiscountDate()
    {
        var existing = new Item { Id = 1, Name = "Item", Description = "Desc", TypeId = 1, CategoryId = 1, Discount = 0, DiscountDate = null };
        var dto = new UpdateItemDto
        {
            Name = "Item",
            Description = "Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1,
            Discount = 20
        };
        var entity = new Item { Id = 1, Name = "Item" };
        var updated = new Item { Id = 1, Name = "Item", TypeId = 1, CategoryId = 1 };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item> { existing }.AsEnumerable());
        _mockMapper.Setup(m => m.ToEntity(1, dto)).Returns(entity);
        _mockItemRepo.Setup(r => r.UpdateAsync(entity)).ReturnsAsync(updated);
        _mockMapper.Setup(m => m.ToDto(updated, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ItemDto());

        await _service.UpdateAsync(1, dto);

        entity.DiscountDate.Should().NotBeNull();
        entity.DiscountDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_DiscountRemoved_ClearsDiscountDate()
    {
        var existing = new Item
        {
            Id = 1, Name = "Item", Description = "Desc", TypeId = 1, CategoryId = 1,
            Discount = 20, DiscountDate = DateTime.UtcNow.AddDays(-5)
        };
        var dto = new UpdateItemDto
        {
            Name = "Item",
            Description = "Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1,
            Discount = 0
        };
        var entity = new Item { Id = 1, Name = "Item" };
        var updated = new Item { Id = 1, Name = "Item", TypeId = 1, CategoryId = 1 };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item> { existing }.AsEnumerable());
        _mockMapper.Setup(m => m.ToEntity(1, dto)).Returns(entity);
        _mockItemRepo.Setup(r => r.UpdateAsync(entity)).ReturnsAsync(updated);
        _mockMapper.Setup(m => m.ToDto(updated, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ItemDto());

        await _service.UpdateAsync(1, dto);

        entity.DiscountDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_DiscountUnchangedPositive_PreservesDiscountDate()
    {
        var originalDate = DateTime.UtcNow.AddDays(-10);
        var existing = new Item
        {
            Id = 1, Name = "Item", Description = "Desc", TypeId = 1, CategoryId = 1,
            Discount = 15, DiscountDate = originalDate
        };
        var dto = new UpdateItemDto
        {
            Name = "Item",
            Description = "Desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1,
            Discount = 25
        };
        var entity = new Item { Id = 1, Name = "Item" };
        var updated = new Item { Id = 1, Name = "Item", TypeId = 1, CategoryId = 1 };

        _mockItemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item> { existing }.AsEnumerable());
        _mockMapper.Setup(m => m.ToEntity(1, dto)).Returns(entity);
        _mockItemRepo.Setup(r => r.UpdateAsync(entity)).ReturnsAsync(updated);
        _mockMapper.Setup(m => m.ToDto(updated, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ItemDto());

        await _service.UpdateAsync(1, dto);

        entity.DiscountDate.Should().Be(originalDate);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        _mockItemRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _service.DeleteAsync(1);

        result.Should().BeTrue();
        _mockItemRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    #endregion
}
