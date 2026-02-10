using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.App.Services;

public class ItemApiService : IItemApiService
{
    private readonly HttpClient _httpClient;

    public ItemApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ItemDto>> GetAllItemsAsync()
    {
        var items = await _httpClient.GetFromJsonAsync<List<ItemDto>>("api/items");
        return items ?? [];
    }

    public async Task<ItemDto> CreateItemAsync(CreateItemDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/items", dto);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ItemDto>())!;
    }

    public async Task<ItemDto?> UpdateItemAsync(int id, UpdateItemDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/items/{id}", dto);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ItemDto>();
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/items/{id}");
        await EnsureSuccessAsync(response);
        return true;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var message = await TryGetErrorMessageAsync(response);
        throw new ApiException(message);
    }

    private static async Task<string> TryGetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
                return errorProp.GetString() ?? "An unexpected error occurred.";
        }
        catch { }

        return "An unexpected error occurred. Please try again.";
    }

    public async Task<List<TypeDto>> GetAllTypesAsync()
    {
        var types = await _httpClient.GetFromJsonAsync<List<TypeDto>>("api/types");
        return types ?? [];
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(int? typeId = null)
    {
        var url = typeId.HasValue ? $"api/categories?typeId={typeId}" : "api/categories";
        var categories = await _httpClient.GetFromJsonAsync<List<CategoryDto>>(url);
        return categories ?? [];
    }
}
