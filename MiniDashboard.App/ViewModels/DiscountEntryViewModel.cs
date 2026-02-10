using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.App.ViewModels;

public partial class DiscountEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<DiscountEntryItem> Items { get; }

    public DiscountEntryViewModel(List<ItemDto> items)
    {
        Items = new ObservableCollection<DiscountEntryItem>(
            items.Select(i => new DiscountEntryItem
            {
                ItemId = i.Id,
                ProductCode = i.ProductCode,
                Name = i.Name,
                Discount = i.Discount > 0 ? i.Discount : 0
            }));
    }

    [RelayCommand]
    private void Save()
    {
        foreach (var item in Items)
        {
            if (item.Discount <= 0 || item.Discount > 100)
            {
                ErrorMessage = $"Discount for {item.ProductCode} must be between 1 and 100.";
                return;
            }
        }

        ErrorMessage = null;
        IsSaved = true;
    }
}

public partial class DiscountEntryItem : ObservableObject
{
    public int ItemId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private decimal _discount;
}
