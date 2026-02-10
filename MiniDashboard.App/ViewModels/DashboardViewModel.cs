using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniDashboard.DTOs.Classes;
using MiniDashboard.App.Services;
using MiniDashboard.App.Views;

namespace MiniDashboard.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IItemApiService _itemApiService;

    [ObservableProperty]
    private ObservableCollection<ItemDto> _allItems = [];

    [ObservableProperty]
    private ObservableCollection<ItemDto> _discountedItems = [];

    [ObservableProperty]
    private ICollectionView? _allItemsView;

    [ObservableProperty]
    private ICollectionView? _discountedItemsView;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _allItemsCountText = string.Empty;

    [ObservableProperty]
    private string _discountedItemsCountText = string.Empty;

    public ItemFilterModel AllItemsFilter { get; }
    public ItemFilterModel DiscountedFilter { get; }

    public ObservableCollection<ItemDto> SelectedAllItems { get; } = [];

    public ObservableCollection<ItemDto> SelectedDiscountedItems { get; } = [];

    public DashboardViewModel(IItemApiService itemApiService)
    {
        _itemApiService = itemApiService;
        AllItemsFilter = new ItemFilterModel();
        AllItemsFilter.FiltersChanged = () => { AllItemsView?.Refresh(); UpdateCounts(); };
        DiscountedFilter = new ItemFilterModel();
        DiscountedFilter.FiltersChanged = () => { DiscountedItemsView?.Refresh(); UpdateCounts(); };
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var items = await _itemApiService.GetAllItemsAsync();

            foreach (var item in items)
            {
                item.CreatedAt = item.CreatedAt.ToLocalTime();
                item.DiscountDate = item.DiscountDate?.ToLocalTime();
                item.UpdatedAt = item.UpdatedAt?.ToLocalTime();
            }

            AllItems = new ObservableCollection<ItemDto>(items);
            DiscountedItems = new ObservableCollection<ItemDto>(
                items.Where(i => i.Discount > 0));

            AllItemsView = CollectionViewSource.GetDefaultView(AllItems);
            AllItemsView.Filter = obj => obj is ItemDto item && AllItemsFilter.MatchesItem(item);

            DiscountedItemsView = CollectionViewSource.GetDefaultView(DiscountedItems);
            DiscountedItemsView.Filter = obj => obj is ItemDto item && DiscountedFilter.MatchesItem(item);

            UpdateCounts();

            if (_itemApiService is CachedItemApiService cached && cached.IsOffline)
            {
                ErrorMessage = "Offline mode — displaying cached data";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please check your connection and try again.";
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong while loading data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var vm = new ItemDialogViewModel(_itemApiService);
        var dialog = new ItemDialogWindow(vm);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task EditItemAsync(ItemDto item)
    {
        var vm = new ItemDialogViewModel(_itemApiService, item);
        var dialog = new ItemDialogWindow(vm);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task EditSelectedAsync()
    {
        var selected = SelectedAllItems.FirstOrDefault() ?? SelectedDiscountedItems.FirstOrDefault();
        if (selected == null) return;
        await EditItemAsync(selected);
    }

    [RelayCommand]
    private async Task DropOnDiscountedAsync(object parameter)
    {
        if (parameter is not List<ItemDto> items || items.Count == 0) return;

        var discountVm = new DiscountEntryViewModel(items);
        var dialog = new DiscountEntryWindow(discountVm);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            try
            {
                foreach (var entry in discountVm.Items)
                {
                    var item = AllItems.FirstOrDefault(i => i.Id == entry.ItemId);
                    if (item is null) continue;

                    var dto = new UpdateItemDto
                    {
                        Name = item.Name,
                        Description = item.Description,
                        TypeId = item.TypeId,
                        CategoryId = item.CategoryId,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        Discount = entry.Discount
                    };
                    await _itemApiService.UpdateItemAsync(entry.ItemId, dto);
                }

                await LoadDataAsync();
            }
            catch (HttpRequestException)
            {
                ErrorMessage = "Unable to connect to the server. Discounts were not applied.";
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "Something went wrong while applying discounts. Please try again.";
            }
        }
    }

    [RelayCommand]
    private async Task DropOnAllItemsAsync(object parameter)
    {
        if (parameter is not List<ItemDto> items || items.Count == 0) return;

        try
        {
            foreach (var item in items)
            {
                var dto = new UpdateItemDto
                {
                    Name = item.Name,
                    Description = item.Description,
                    TypeId = item.TypeId,
                    CategoryId = item.CategoryId,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Discount = 0
                };
                await _itemApiService.UpdateItemAsync(item.Id, dto);
            }

            await LoadDataAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Discounts were not removed.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong while removing discounts. Please try again.";
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = SelectedAllItems.ToList()
            .Concat(SelectedDiscountedItems.ToList())
            .GroupBy(i => i.Id)
            .Select(g => g.First())
            .ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show("No items selected.", "Delete",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete {selected.Count} selected item(s)?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            foreach (var item in selected)
            {
                await _itemApiService.DeleteItemAsync(item.Id);
            }

            await LoadDataAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Items were not deleted.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong while deleting items. Please try again.";
        }
    }

    private void UpdateCounts()
    {
        var allFiltered = AllItemsView?.Cast<object>().Count() ?? 0;
        var allTotal = AllItems.Count;
        AllItemsCountText = $"{allFiltered} / {allTotal} items";

        var discFiltered = DiscountedItemsView?.Cast<object>().Count() ?? 0;
        var discTotal = DiscountedItems.Count;
        DiscountedItemsCountText = $"{discFiltered} / {discTotal} items";
    }
}
