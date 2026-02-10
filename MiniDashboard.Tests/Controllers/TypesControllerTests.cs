using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MiniDashboard.Api.Controllers;
using MiniDashboard.DAL.Classes;
using MiniDashboard.DAL.Interfaces;
using Xunit;
using Moq;

namespace MiniDashboard.Tests.Controllers;

public class TypesControllerTests
{
    private readonly Mock<ITypeRepository> _mockRepo = new();
    private readonly TypesController _controller;

    public TypesControllerTests()
    {
        _controller = new TypesController(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTypes()
    {
        var types = new List<ItemType>
        {
            new() { Id = 1, Name = "Electronics" },
            new() { Id = 2, Name = "Furniture" }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(types.AsEnumerable());

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(types);
    }
}
