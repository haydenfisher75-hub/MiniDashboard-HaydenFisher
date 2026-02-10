using System.Net.Http;
using FluentAssertions;
using MiniDashboard.DTOs.Classes;
using MiniDashboard.App.Services;
using MiniDashboard.App.ViewModels;
using Moq;
using Xunit;

namespace MiniDashboard.Tests.WPF;

public class ItemDialogViewModelTests
{
    private readonly Mock<IItemApiService> _mockApi = new();

    private ItemDialogViewModel CreateAddVm() => new(_mockApi.Object);

    private ItemDialogViewModel CreateEditVm(ItemDto item) => new(_mockApi.Object, item);

    #region Constructor

    [Fact]
    public void Constructor_AddMode_SetsIsEditingFalse()
    {
        var vm = CreateAddVm();

        vm.IsEditing.Should().BeFalse();
        vm.EditingItemId.Should().BeNull();
        vm.WindowTitle.Should().Be("Add Item");
    }

    [Fact]
    public void Constructor_EditMode_SetsIsEditingTrue()
    {
        var item = new ItemDto
        {
            Id = 5, Name = "Widget", Description = "Desc",
            Price = 29.99m, Quantity = 3, Discount = 10,
            TypeId = 1, CategoryId = 2
        };

        var vm = CreateEditVm(item);

        vm.IsEditing.Should().BeTrue();
        vm.EditingItemId.Should().Be(5);
        vm.WindowTitle.Should().Be("Edit Item");
        vm.Name.Should().Be("Widget");
        vm.Description.Should().Be("Desc");
        vm.Price.Should().Be(29.99m);
        vm.Quantity.Should().Be(3);
        vm.Discount.Should().Be(10);
    }

    #endregion

    #region LoadDataAsync

    [Fact]
    public async Task LoadDataAsync_LoadsTypesAndCategories()
    {
        var types = new List<TypeDto> { new() { Id = 1, Name = "Electronics" } };
        var categories = new List<CategoryDto> { new() { Id = 1, Name = "Phones", TypeId = 1 } };
        _mockApi.Setup(s => s.GetAllTypesAsync()).ReturnsAsync(types);
        _mockApi.Setup(s => s.GetCategoriesAsync(null)).ReturnsAsync(categories);

        var vm = CreateAddVm();
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.Types.Should().HaveCount(1);
        vm.ErrorMessage.Should().BeNull();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDataAsync_EditMode_SelectsInitialType()
    {
        var types = new List<TypeDto> { new() { Id = 1, Name = "Electronics" }, new() { Id = 2, Name = "Furniture" } };
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Phones", TypeId = 1 },
            new() { Id = 2, Name = "Chairs", TypeId = 2 }
        };
        _mockApi.Setup(s => s.GetAllTypesAsync()).ReturnsAsync(types);
        _mockApi.Setup(s => s.GetCategoriesAsync(null)).ReturnsAsync(categories);

        var item = new ItemDto { Id = 1, TypeId = 2, CategoryId = 2, Name = "Chair", Description = "D", Price = 10 };
        var vm = CreateEditVm(item);
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.SelectedType.Should().NotBeNull();
        vm.SelectedType!.Id.Should().Be(2);
    }

    [Fact]
    public async Task LoadDataAsync_HttpRequestException_SetsErrorMessage()
    {
        _mockApi.Setup(s => s.GetAllTypesAsync()).ThrowsAsync(new HttpRequestException());

        var vm = CreateAddVm();
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("Unable to connect");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDataAsync_GenericException_SetsErrorMessage()
    {
        _mockApi.Setup(s => s.GetAllTypesAsync()).ThrowsAsync(new Exception("boom"));

        var vm = CreateAddVm();
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("Something went wrong");
        vm.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public async Task SaveAsync_EmptyName_SetsErrorMessage()
    {
        var vm = CreateAddVm();
        vm.Name = "";
        vm.Description = "Desc";

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Name is required.");
        vm.IsSaved.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_EmptyDescription_SetsErrorMessage()
    {
        var vm = CreateAddVm();
        vm.Name = "Test";
        vm.Description = "";

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Description is required.");
    }

    [Fact]
    public async Task SaveAsync_NoType_SetsErrorMessage()
    {
        var vm = CreateAddVm();
        vm.Name = "Test";
        vm.Description = "Desc";
        // SelectedType is null by default

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Type is required.");
    }

    [Fact]
    public async Task SaveAsync_NoCategory_SetsErrorMessage()
    {
        var vm = CreateAddVm();
        vm.Name = "Test";
        vm.Description = "Desc";
        vm.SelectedType = new TypeDto { Id = 1, Name = "Electronics" };
        // SelectedCategory is null

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Category is required.");
    }

    [Fact]
    public async Task SaveAsync_PriceZero_SetsErrorMessage()
    {
        var vm = CreateAddVm();
        vm.Name = "Test";
        vm.Description = "Desc";
        vm.SelectedType = new TypeDto { Id = 1, Name = "Electronics" };
        vm.SelectedCategory = new CategoryDto { Id = 1, Name = "Phones" };
        vm.Price = 0;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Be("Price must be greater than zero.");
    }

    #endregion

    #region SaveAsync - Create

    [Fact]
    public async Task SaveAsync_AddMode_CallsCreateItemAsync()
    {
        var created = new ItemDto { Id = 1, Name = "New" };
        _mockApi.Setup(s => s.CreateItemAsync(It.IsAny<CreateItemDto>())).ReturnsAsync(created);

        var vm = CreateAddVm();
        vm.Name = "New";
        vm.Description = "New Desc";
        vm.SelectedType = new TypeDto { Id = 1, Name = "Electronics" };
        vm.SelectedCategory = new CategoryDto { Id = 1, Name = "Phones" };
        vm.Price = 49.99m;
        vm.Quantity = 5;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.IsSaved.Should().BeTrue();
        vm.IsLoading.Should().BeFalse();
        _mockApi.Verify(s => s.CreateItemAsync(It.Is<CreateItemDto>(d =>
            d.Name == "New" && d.Price == 49.99m)), Times.Once);
    }

    #endregion

    #region SaveAsync - Update

    [Fact]
    public async Task SaveAsync_EditMode_CallsUpdateItemAsync()
    {
        var updated = new ItemDto { Id = 5, Name = "Updated" };
        _mockApi.Setup(s => s.UpdateItemAsync(5, It.IsAny<UpdateItemDto>())).ReturnsAsync(updated);

        var item = new ItemDto { Id = 5, Name = "Old", Description = "Old Desc", Price = 10, TypeId = 1, CategoryId = 1 };
        var vm = CreateEditVm(item);
        vm.Name = "Updated";
        vm.Description = "Updated Desc";
        vm.SelectedType = new TypeDto { Id = 1, Name = "Electronics" };
        vm.SelectedCategory = new CategoryDto { Id = 1, Name = "Phones" };
        vm.Price = 59.99m;
        vm.Quantity = 10;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.IsSaved.Should().BeTrue();
        _mockApi.Verify(s => s.UpdateItemAsync(5, It.Is<UpdateItemDto>(d =>
            d.Name == "Updated" && d.Price == 59.99m)), Times.Once);
    }

    #endregion

    #region SaveAsync - Error handling

    [Fact]
    public async Task SaveAsync_HttpRequestException_SetsErrorMessage()
    {
        _mockApi.Setup(s => s.CreateItemAsync(It.IsAny<CreateItemDto>()))
            .ThrowsAsync(new HttpRequestException());

        var vm = CreateAddVm();
        vm.Name = "Test";
        vm.Description = "Desc";
        vm.SelectedType = new TypeDto { Id = 1, Name = "E" };
        vm.SelectedCategory = new CategoryDto { Id = 1, Name = "P" };
        vm.Price = 10m;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.IsSaved.Should().BeFalse();
        vm.ErrorMessage.Should().Contain("Unable to connect");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_ApiException_ShowsApiMessage()
    {
        _mockApi.Setup(s => s.CreateItemAsync(It.IsAny<CreateItemDto>()))
            .ThrowsAsync(new ApiException("An item with the name 'Test' already exists."));

        var vm = CreateAddVm();
        vm.Name = "Test";
        vm.Description = "Desc";
        vm.SelectedType = new TypeDto { Id = 1, Name = "E" };
        vm.SelectedCategory = new CategoryDto { Id = 1, Name = "P" };
        vm.Price = 10m;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.IsSaved.Should().BeFalse();
        vm.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task SaveAsync_GenericException_SetsErrorMessage()
    {
        _mockApi.Setup(s => s.CreateItemAsync(It.IsAny<CreateItemDto>()))
            .ThrowsAsync(new Exception("unexpected"));

        var vm = CreateAddVm();
        vm.Name = "Test";
        vm.Description = "Desc";
        vm.SelectedType = new TypeDto { Id = 1, Name = "E" };
        vm.SelectedCategory = new CategoryDto { Id = 1, Name = "P" };
        vm.Price = 10m;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.IsSaved.Should().BeFalse();
        vm.ErrorMessage.Should().Contain("Something went wrong");
    }

    #endregion

    #region OnSelectedTypeChanged

    [Fact]
    public async Task SelectedTypeChanged_FiltersCategories()
    {
        var types = new List<TypeDto>
        {
            new() { Id = 1, Name = "Electronics" },
            new() { Id = 2, Name = "Furniture" }
        };
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Phones", TypeId = 1 },
            new() { Id = 2, Name = "Laptops", TypeId = 1 },
            new() { Id = 3, Name = "Chairs", TypeId = 2 }
        };
        _mockApi.Setup(s => s.GetAllTypesAsync()).ReturnsAsync(types);
        _mockApi.Setup(s => s.GetCategoriesAsync(null)).ReturnsAsync(categories);

        var vm = CreateAddVm();
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.SelectedType = types[0]; // Electronics

        vm.FilteredCategories.Should().HaveCount(2);
        vm.FilteredCategories.Should().OnlyContain(c => c.TypeId == 1);
    }

    [Fact]
    public async Task SelectedTypeChanged_ToNull_ClearsCategories()
    {
        var types = new List<TypeDto> { new() { Id = 1, Name = "Electronics" } };
        var categories = new List<CategoryDto> { new() { Id = 1, Name = "Phones", TypeId = 1 } };
        _mockApi.Setup(s => s.GetAllTypesAsync()).ReturnsAsync(types);
        _mockApi.Setup(s => s.GetCategoriesAsync(null)).ReturnsAsync(categories);

        var vm = CreateAddVm();
        await vm.LoadDataCommand.ExecuteAsync(null);

        vm.SelectedType = types[0];
        vm.FilteredCategories.Should().HaveCount(1);

        vm.SelectedType = null;
        vm.FilteredCategories.Should().BeEmpty();
        vm.SelectedCategory.Should().BeNull();
    }

    #endregion
}
