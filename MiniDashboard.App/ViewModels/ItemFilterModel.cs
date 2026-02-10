using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.App.ViewModels;

public partial class ItemFilterModel : ObservableObject
{
    public Action? FiltersChanged { get; set; }

    // String filters
    [ObservableProperty] private string _productCode = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _typeName = string.Empty;
    [ObservableProperty] private string _categoryName = string.Empty;

    // Numeric range filters (stored as strings for easy binding, parsed on match)
    [ObservableProperty] private string _priceMin = string.Empty;
    [ObservableProperty] private string _priceMax = string.Empty;
    [ObservableProperty] private string _quantityMin = string.Empty;
    [ObservableProperty] private string _quantityMax = string.Empty;
    [ObservableProperty] private string _discountMin = string.Empty;
    [ObservableProperty] private string _discountMax = string.Empty;

    // Date range filters
    [ObservableProperty] private DateTime? _createdFrom;
    [ObservableProperty] private DateTime? _createdTo;
    [ObservableProperty] private DateTime? _updatedFrom;
    [ObservableProperty] private DateTime? _updatedTo;
    [ObservableProperty] private DateTime? _discountDateFrom;
    [ObservableProperty] private DateTime? _discountDateTo;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName != nameof(FiltersChanged))
            FiltersChanged?.Invoke();
    }

    public bool MatchesItem(ItemDto item)
    {
        // String contains filters
        if (!MatchesString(item.ProductCode, ProductCode)) return false;
        if (!MatchesString(item.Name, Name)) return false;
        if (!MatchesString(item.Description, Description)) return false;
        if (!MatchesString(item.TypeName, TypeName)) return false;
        if (!MatchesString(item.CategoryName, CategoryName)) return false;

        // Numeric range filters
        if (!MatchesDecimalRange(item.Price, PriceMin, PriceMax)) return false;
        if (!MatchesIntRange(item.Quantity, QuantityMin, QuantityMax)) return false;
        if (!MatchesDecimalRange(item.Discount, DiscountMin, DiscountMax)) return false;

        // Date range filters
        if (!MatchesDateRange(item.CreatedAt, CreatedFrom, CreatedTo)) return false;
        if (!MatchesDateRange(item.UpdatedAt, UpdatedFrom, UpdatedTo)) return false;
        if (!MatchesDateRange(item.DiscountDate, DiscountDateFrom, DiscountDateTo)) return false;

        return true;
    }

    [RelayCommand]
    private void Clear()
    {
        ProductCode = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        TypeName = string.Empty;
        CategoryName = string.Empty;
        PriceMin = string.Empty;
        PriceMax = string.Empty;
        QuantityMin = string.Empty;
        QuantityMax = string.Empty;
        DiscountMin = string.Empty;
        DiscountMax = string.Empty;
        CreatedFrom = null;
        CreatedTo = null;
        UpdatedFrom = null;
        UpdatedTo = null;
        DiscountDateFrom = null;
        DiscountDateTo = null;
    }

    private static bool MatchesString(string value, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        return value.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesDecimalRange(decimal value, string minStr, string maxStr)
    {
        if (!string.IsNullOrEmpty(minStr) && decimal.TryParse(minStr, out var min) && value < min)
            return false;
        if (!string.IsNullOrEmpty(maxStr) && decimal.TryParse(maxStr, out var max) && value > max)
            return false;
        return true;
    }

    private static bool MatchesIntRange(int value, string minStr, string maxStr)
    {
        if (!string.IsNullOrEmpty(minStr) && int.TryParse(minStr, out var min) && value < min)
            return false;
        if (!string.IsNullOrEmpty(maxStr) && int.TryParse(maxStr, out var max) && value > max)
            return false;
        return true;
    }

    private static bool MatchesDateRange(DateTime? value, DateTime? from, DateTime? to)
    {
        if (from.HasValue && (!value.HasValue || value.Value.Date < from.Value.Date))
            return false;
        if (to.HasValue && (!value.HasValue || value.Value.Date > to.Value.Date))
            return false;
        return true;
    }

    private static bool MatchesDateRange(DateTime value, DateTime? from, DateTime? to)
    {
        if (from.HasValue && value.Date < from.Value.Date)
            return false;
        if (to.HasValue && value.Date > to.Value.Date)
            return false;
        return true;
    }
}
