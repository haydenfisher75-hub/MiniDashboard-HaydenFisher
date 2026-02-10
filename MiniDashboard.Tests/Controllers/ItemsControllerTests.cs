using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MiniDashboard.Api.Controllers;
using MiniDashboard.BL.Interfaces;
using MiniDashboard.DTOs.Classes;
using Xunit;
using Moq;

namespace MiniDashboard.Tests.Controllers;

public class ItemsControllerTests
{
    private readonly Mock<IItemService> _mockService = new();
    private readonly ItemsController _controller;

    public ItemsControllerTests()
    {
        _controller = new ItemsController(_mockService.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var items = new List<ItemDto> { new() { Id = 1, Name = "Test" } };
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(items);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(items);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var item = new ItemDto { Id = 1, Name = "Test" };
        _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(item);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((ItemDto?)null);

        var result = await _controller.GetById(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Search_ReturnsOkWithResults()
    {
        var items = new List<ItemDto> { new() { Id = 1, Name = "Phone" } };
        _mockService.Setup(s => s.SearchAsync("Phone")).ReturnsAsync(items);

        var result = await _controller.Search("Phone");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(items);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreatedAtAction()
    {
        var dto = new CreateItemDto { Name = "New", Description = "Desc", TypeId = 1, CategoryId = 1, Price = 10m, Quantity = 1 };
        var created = new ItemDto { Id = 1, Name = "New" };
        _mockService.Setup(s => s.AddAsync(dto)).ReturnsAsync(created);

        var result = await _controller.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ItemsController.GetById));
        createdResult.Value.Should().Be(created);
    }

    [Fact]
    public async Task Create_WhenDuplicate_ReturnsConflict()
    {
        var dto = new CreateItemDto { Name = "Existing", Description = "Desc", TypeId = 1, CategoryId = 1, Price = 10m, Quantity = 1 };
        _mockService.Setup(s => s.AddAsync(dto))
            .ThrowsAsync(new InvalidOperationException("An item with the name 'Existing' already exists."));

        var result = await _controller.Create(dto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOk()
    {
        var dto = new UpdateItemDto { Name = "Updated", Description = "Desc", TypeId = 1, CategoryId = 1, Price = 10m, Quantity = 1 };
        var updated = new ItemDto { Id = 1, Name = "Updated" };
        _mockService.Setup(s => s.UpdateAsync(1, dto)).ReturnsAsync(updated);

        var result = await _controller.Update(1, dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updated);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var dto = new UpdateItemDto { Name = "Updated", Description = "Desc", TypeId = 1, CategoryId = 1, Price = 10m, Quantity = 1 };
        _mockService.Setup(s => s.UpdateAsync(999, dto)).ReturnsAsync((ItemDto?)null);

        var result = await _controller.Update(999, dto);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_WhenDuplicate_ReturnsConflict()
    {
        var dto = new UpdateItemDto { Name = "Duplicate", Description = "Desc", TypeId = 1, CategoryId = 1, Price = 10m, Quantity = 1 };
        _mockService.Setup(s => s.UpdateAsync(1, dto))
            .ThrowsAsync(new InvalidOperationException("An item with the name 'Duplicate' already exists."));

        var result = await _controller.Update(1, dto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _controller.Delete(999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
