using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MiniDashboard.DTOs.Classes;
using Xunit;

namespace MiniDashboard.IntegrationTests;

public class ItemsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ItemsEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private CreateItemDto MakeCreateDto(string name = "Test Item", string description = "Test Description",
        int typeId = 1, int categoryId = 1, decimal price = 29.99m, int quantity = 10, decimal discount = 0)
    {
        return new CreateItemDto
        {
            Name = name,
            Description = description,
            TypeId = typeId,
            CategoryId = categoryId,
            Price = price,
            Quantity = quantity,
            Discount = discount
        };
    }

    private async Task<ItemDto> CreateTestItemAsync(string name, string description, decimal discount = 0)
    {
        var dto = MakeCreateDto(name: name, description: description, discount: discount);
        var response = await _client.PostAsJsonAsync("/api/items", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ItemDto>(JsonOptions))!;
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/items");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ItemDto>>(JsonOptions);
        items.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ValidItem_ReturnsCreatedWithProductCode()
    {
        var dto = MakeCreateDto(name: "Create Test Phone", description: "A test phone for creation");
        var response = await _client.PostAsJsonAsync("/api/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<ItemDto>(JsonOptions);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Create Test Phone");
        created.ProductCode.Should().StartWith("PHN-");
        created.TypeName.Should().Be("Electronics");
        created.CategoryName.Should().Be("Phones");
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        await CreateTestItemAsync("Duplicate Name Test", "Unique desc 1");

        var dto = MakeCreateDto(name: "duplicate name test", description: "Unique desc 2");
        var response = await _client.PostAsJsonAsync("/api/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_DuplicateDescription_ReturnsConflict()
    {
        await CreateTestItemAsync("Unique name 1", "Duplicate Desc Test");

        var dto = MakeCreateDto(name: "Unique name 2", description: "duplicate desc test");
        var response = await _client.PostAsJsonAsync("/api/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var dto = MakeCreateDto(name: "", description: "Valid desc");
        var response = await _client.PostAsJsonAsync("/api/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_PriceZero_ReturnsBadRequest()
    {
        var dto = MakeCreateDto(name: "Price test", description: "Price desc", price: 0);
        var response = await _client.PostAsJsonAsync("/api/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DiscountOver100_ReturnsBadRequest()
    {
        var dto = MakeCreateDto(name: "Disc test", description: "Disc desc", discount: 150);
        var response = await _client.PostAsJsonAsync("/api/items", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithDiscount_SetsDiscountDate()
    {
        var created = await CreateTestItemAsync("Discounted Item", "Discounted desc", discount: 25);

        created.DiscountDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithoutDiscount_DiscountDateIsNull()
    {
        var created = await CreateTestItemAsync("No Discount Item", "No discount desc", discount: 0);

        created.DiscountDate.Should().BeNull();
    }

    [Fact]
    public async Task GetById_ExistingItem_ReturnsOkWithEnrichedDto()
    {
        var created = await CreateTestItemAsync("GetById Test", "GetById desc");

        var response = await _client.GetAsync($"/api/items/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await response.Content.ReadFromJsonAsync<ItemDto>(JsonOptions);
        item.Should().NotBeNull();
        item!.Id.Should().Be(created.Id);
        item.TypeName.Should().NotBeNullOrEmpty();
        item.CategoryName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/items/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_MatchingQuery_ReturnsResults()
    {
        await CreateTestItemAsync("Searchable Widget", "Widget for search test");

        var response = await _client.GetAsync("/api/items/search?query=Searchable");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ItemDto>>(JsonOptions);
        items.Should().NotBeNull();
        items!.Should().Contain(i => i.Name.Contains("Searchable"));
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/items/search?query=zzzzzznonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ItemDto>>(JsonOptions);
        items.Should().NotBeNull();
        items!.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ExistingItem_ReturnsOkWithUpdated()
    {
        var created = await CreateTestItemAsync("Update Original", "Update original desc");

        var updateDto = new UpdateItemDto
        {
            Name = "Update Modified",
            Description = "Update modified desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 59.99m,
            Quantity = 20,
            Discount = 0
        };
        var response = await _client.PutAsJsonAsync($"/api/items/{created.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ItemDto>(JsonOptions);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Update Modified");
        updated.Price.Should().Be(59.99m);
    }

    [Fact]
    public async Task Update_NonExistentItem_ReturnsNotFound()
    {
        var updateDto = new UpdateItemDto
        {
            Name = "Ghost",
            Description = "Ghost desc",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };
        var response = await _client.PutAsJsonAsync("/api/items/99999", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsConflict()
    {
        var item1 = await CreateTestItemAsync("Update Dup A", "Update dup desc A");
        var item2 = await CreateTestItemAsync("Update Dup B", "Update dup desc B");

        var updateDto = new UpdateItemDto
        {
            Name = "Update Dup A",
            Description = "Update dup desc B changed",
            TypeId = 1,
            CategoryId = 1,
            Price = 10m,
            Quantity = 1
        };
        var response = await _client.PutAsJsonAsync($"/api/items/{item2.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ApplyDiscount_SetsDiscountDate()
    {
        var created = await CreateTestItemAsync("Discount Apply Test", "Discount apply desc", discount: 0);
        created.DiscountDate.Should().BeNull();

        var updateDto = new UpdateItemDto
        {
            Name = created.Name,
            Description = created.Description,
            TypeId = created.TypeId,
            CategoryId = created.CategoryId,
            Price = created.Price,
            Quantity = created.Quantity,
            Discount = 10
        };
        var response = await _client.PutAsJsonAsync($"/api/items/{created.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ItemDto>(JsonOptions);
        updated!.DiscountDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_RemoveDiscount_ClearsDiscountDate()
    {
        var created = await CreateTestItemAsync("Discount Remove Test", "Discount remove desc", discount: 20);
        created.DiscountDate.Should().NotBeNull();

        var updateDto = new UpdateItemDto
        {
            Name = created.Name,
            Description = created.Description,
            TypeId = created.TypeId,
            CategoryId = created.CategoryId,
            Price = created.Price,
            Quantity = created.Quantity,
            Discount = 0
        };
        var response = await _client.PutAsJsonAsync($"/api/items/{created.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ItemDto>(JsonOptions);
        updated!.DiscountDate.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        var created = await CreateTestItemAsync("Delete Test", "Delete test desc");

        var response = await _client.DeleteAsync($"/api/items/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentItem_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/items/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var created = await CreateTestItemAsync("Delete Then Get", "Delete then get desc");

        await _client.DeleteAsync($"/api/items/{created.Id}");

        var response = await _client.GetAsync($"/api/items/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FullCrudFlow_CreateReadUpdateDelete()
    {
        // Create
        var created = await CreateTestItemAsync("CRUD Flow Item", "CRUD flow desc");
        created.Id.Should().BeGreaterThan(0);
        created.ProductCode.Should().NotBeNullOrEmpty();

        // Read
        var getResponse = await _client.GetAsync($"/api/items/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ItemDto>(JsonOptions);
        fetched!.Name.Should().Be("CRUD Flow Item");

        // Update
        var updateDto = new UpdateItemDto
        {
            Name = "CRUD Flow Updated",
            Description = "CRUD flow desc updated",
            TypeId = 1,
            CategoryId = 1,
            Price = 99.99m,
            Quantity = 50,
            Discount = 5
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/items/{created.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ItemDto>(JsonOptions);
        updated!.Name.Should().Be("CRUD Flow Updated");
        updated.Discount.Should().Be(5);
        updated.DiscountDate.Should().NotBeNull();

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/items/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify gone
        var verifyResponse = await _client.GetAsync($"/api/items/{created.Id}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
