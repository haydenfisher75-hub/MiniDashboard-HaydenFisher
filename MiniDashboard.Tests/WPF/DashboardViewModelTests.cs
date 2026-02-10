using System.Net.Http;
using FluentAssertions;
using MiniDashboard.DTOs.Classes;
using MiniDashboard.App.Services;
using MiniDashboard.App.ViewModels;
using Moq;
using Xunit;

namespace MiniDashboard.Tests.WPF;

public class DashboardViewModelTests
{
    private readonly Mock<IItemApiService> _mockApi = new();

    private DashboardViewModel CreateVm() => new(_mockApi.Object);

    #region Constructor

    [Fact]
    public void Constructor_InitializesFilters()
    {
        var vm = CreateVm();

        vm.AllItemsFilter.Should().NotBeNull();
        vm.DiscountedFilter.Should().NotBeNull();
        vm.SelectedAllItems.Should().BeEmpty();
        vm.SelectedDiscountedItems.Should().BeEmpty();
    }

    #endregion

    #region LoadDataAsync - Error Handling

    [Fact]
    public async Task LoadDataAsync_HttpRequestException_SetsErrorMessage()
    {
        _mockApi.Setup(s => s.GetAllItemsAsync()).ThrowsAsync(new HttpRequestException());

        var vm = CreateVm();
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("Unable to connect to the server");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDataAsync_GenericException_SetsErrorMessage()
    {
        _mockApi.Setup(s => s.GetAllItemsAsync()).ThrowsAsync(new Exception("boom"));

        var vm = CreateVm();
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("Something went wrong");
        vm.IsLoading.Should().BeFalse();
    }

    #endregion

    #region DropOnAllItemsAsync

    [Fact]
    public async Task DropOnAllItemsAsync_WithNullParameter_DoesNothing()
    {
        var vm = CreateVm();

        await vm.DropOnAllItemsCommand.ExecuteAsync(null);

        _mockApi.Verify(s => s.UpdateItemAsync(It.IsAny<int>(), It.IsAny<UpdateItemDto>()), Times.Never);
    }

    [Fact]
    public async Task DropOnAllItemsAsync_WithEmptyList_DoesNothing()
    {
        var vm = CreateVm();

        await vm.DropOnAllItemsCommand.ExecuteAsync(new List<ItemDto>());

        _mockApi.Verify(s => s.UpdateItemAsync(It.IsAny<int>(), It.IsAny<UpdateItemDto>()), Times.Never);
    }

    [Fact]
    public async Task DropOnAllItemsAsync_SetsDiscountToZero()
    {
        // LoadDataAsync will be called after update, so mock it too
        _mockApi.Setup(s => s.GetAllItemsAsync()).ThrowsAsync(new HttpRequestException());
        _mockApi.Setup(s => s.UpdateItemAsync(It.IsAny<int>(), It.IsAny<UpdateItemDto>()))
            .ReturnsAsync(new ItemDto());

        var items = new List<ItemDto>
        {
            new() { Id = 1, Name = "Item1", Description = "D1", TypeId = 1, CategoryId = 1, Price = 10, Quantity = 1, Discount = 20 }
        };

        var vm = CreateVm();
        await vm.DropOnAllItemsCommand.ExecuteAsync(items);

        _mockApi.Verify(s => s.UpdateItemAsync(1, It.Is<UpdateItemDto>(d => d.Discount == 0)), Times.Once);
    }

    [Fact]
    public async Task DropOnAllItemsAsync_HttpException_SetsErrorMessage()
    {
        _mockApi.Setup(s => s.UpdateItemAsync(It.IsAny<int>(), It.IsAny<UpdateItemDto>()))
            .ThrowsAsync(new HttpRequestException());

        var items = new List<ItemDto>
        {
            new() { Id = 1, Name = "Item1", Description = "D1", TypeId = 1, CategoryId = 1, Price = 10, Quantity = 1, Discount = 20 }
        };

        var vm = CreateVm();
        await vm.DropOnAllItemsCommand.ExecuteAsync(items);

        vm.ErrorMessage.Should().Contain("Unable to connect");
    }

    [Fact]
    public async Task DropOnAllItemsAsync_ApiException_ShowsApiMessage()
    {
        _mockApi.Setup(s => s.UpdateItemAsync(It.IsAny<int>(), It.IsAny<UpdateItemDto>()))
            .ThrowsAsync(new ApiException("Business rule violation"));

        var items = new List<ItemDto>
        {
            new() { Id = 1, Name = "Item1", Description = "D1", TypeId = 1, CategoryId = 1, Price = 10, Quantity = 1, Discount = 20 }
        };

        var vm = CreateVm();
        await vm.DropOnAllItemsCommand.ExecuteAsync(items);

        vm.ErrorMessage.Should().Be("Business rule violation");
    }

    #endregion
}
