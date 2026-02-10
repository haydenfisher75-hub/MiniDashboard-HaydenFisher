using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniDashboard.DTOs.Classes;
using MiniDashboard.App.Services;

namespace MiniDashboard.App.ViewModels;

public partial class ItemDialogViewModel : ObservableObject
{
    private readonly IItemApiService _apiService;
    private List<CategoryDto> _allCategories = [];
    private int? _initialTypeId;
    private int? _initialCategoryId;

    public bool IsEditing { get; }
    public int? EditingItemId { get; }
    public string WindowTitle => IsEditing ? "Edit Item" : "Add Item";

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _discount;

    [ObservableProperty]
    private ObservableCollection<TypeDto> _types = [];

    [ObservableProperty]
    private TypeDto? _selectedType;

    [ObservableProperty]
    private ObservableCollection<CategoryDto> _filteredCategories = [];

    [ObservableProperty]
    private CategoryDto? _selectedCategory;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public ItemDialogViewModel(IItemApiService apiService)
    {
        _apiService = apiService;
        IsEditing = false;
    }

    public ItemDialogViewModel(IItemApiService apiService, ItemDto item) : this(apiService)
    {
        IsEditing = true;
        EditingItemId = item.Id;
        Name = item.Name;
        Description = item.Description;
        Price = item.Price;
        Quantity = item.Quantity;
        Discount = item.Discount;
        _initialTypeId = item.TypeId;
        _initialCategoryId = item.CategoryId;
    }

    partial void OnSelectedTypeChanged(TypeDto? value)
    {
        if (value is null)
        {
            FilteredCategories = [];
            SelectedCategory = null;
            return;
        }

        FilteredCategories = new ObservableCollection<CategoryDto>(
            _allCategories.Where(c => c.TypeId == value.Id));

        if (_initialCategoryId.HasValue)
        {
            SelectedCategory = FilteredCategories.FirstOrDefault(c => c.Id == _initialCategoryId.Value);
            _initialCategoryId = null;
        }
        else
        {
            SelectedCategory = null;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            var types = await _apiService.GetAllTypesAsync();
            _allCategories = await _apiService.GetCategoriesAsync();
            Types = new ObservableCollection<TypeDto>(types);

            if (_initialTypeId.HasValue)
            {
                SelectedType = Types.FirstOrDefault(t => t.Id == _initialTypeId.Value);
                _initialTypeId = null;
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please check your connection.";
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong while loading form data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        { ErrorMessage = "Name is required."; return; }
        if (string.IsNullOrWhiteSpace(Description))
        { ErrorMessage = "Description is required."; return; }
        if (SelectedType is null)
        { ErrorMessage = "Type is required."; return; }
        if (SelectedCategory is null)
        { ErrorMessage = "Category is required."; return; }
        if (Price <= 0)
        { ErrorMessage = "Price must be greater than zero."; return; }

        try
        {
            ErrorMessage = null;
            IsLoading = true;

            if (IsEditing)
            {
                var dto = new UpdateItemDto
                {
                    Name = Name,
                    Description = Description,
                    TypeId = SelectedType.Id,
                    CategoryId = SelectedCategory.Id,
                    Price = Price,
                    Quantity = Quantity,
                    Discount = Discount
                };
                await _apiService.UpdateItemAsync(EditingItemId!.Value, dto);
            }
            else
            {
                var dto = new CreateItemDto
                {
                    Name = Name,
                    Description = Description,
                    TypeId = SelectedType.Id,
                    CategoryId = SelectedCategory.Id,
                    Price = Price,
                    Quantity = Quantity,
                    Discount = Discount
                };
                await _apiService.CreateItemAsync(dto);
            }

            IsSaved = true;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please check your connection and try again.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong while saving. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
