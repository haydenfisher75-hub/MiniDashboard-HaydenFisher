using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MiniDashboard.Api.Controllers;
using MiniDashboard.DAL.Classes;
using MiniDashboard.DAL.Interfaces;
using Xunit;
using Moq;

namespace MiniDashboard.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryRepository> _mockRepo = new();
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _controller = new CategoriesController(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAll_WithoutTypeId_ReturnsAllCategories()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Phones", TypeId = 1 },
            new() { Id = 2, Name = "Laptops", TypeId = 1 }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories.AsEnumerable());

        var result = await _controller.GetAll(null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(categories);
    }

    [Fact]
    public async Task GetAll_WithTypeId_ReturnsFilteredCategories()
    {
        var filtered = new List<Category>
        {
            new() { Id = 1, Name = "Phones", TypeId = 1 }
        };
        _mockRepo.Setup(r => r.GetByTypeIdAsync(1)).ReturnsAsync(filtered.AsEnumerable());

        var result = await _controller.GetAll(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(filtered);
    }
}
