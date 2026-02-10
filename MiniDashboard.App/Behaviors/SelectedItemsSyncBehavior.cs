using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace MiniDashboard.App.Behaviors;

public class SelectedItemsSyncBehavior : Behavior<DataGrid>
{
    private bool _isSyncing;

    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(SelectedItemsSyncBehavior));

    public static readonly DependencyProperty PartnerGridProperty =
        DependencyProperty.Register(nameof(PartnerGrid), typeof(DataGrid), typeof(SelectedItemsSyncBehavior));

    public static readonly DependencyProperty DoubleClickCommandProperty =
        DependencyProperty.Register(nameof(DoubleClickCommand), typeof(ICommand), typeof(SelectedItemsSyncBehavior));

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(SelectedItemsSyncBehavior));

    public IList? SelectedItems
    {
        get => (IList?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    public DataGrid? PartnerGrid
    {
        get => (DataGrid?)GetValue(PartnerGridProperty);
        set => SetValue(PartnerGridProperty, value);
    }

    public ICommand? DoubleClickCommand
    {
        get => (ICommand?)GetValue(DoubleClickCommandProperty);
        set => SetValue(DoubleClickCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += OnSelectionChanged;
        AssociatedObject.MouseDoubleClick += OnMouseDoubleClick;
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.MouseDoubleClick -= OnMouseDoubleClick;
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        base.OnDetaching();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing) return;

        var grid = AssociatedObject;
        if (grid.SelectedItems.Count > 0 && PartnerGrid != null)
        {
            _isSyncing = true;

            // Find the partner's behavior and clear its selection
            var partnerBehaviors = Interaction.GetBehaviors(PartnerGrid);
            foreach (var b in partnerBehaviors)
            {
                if (b is SelectedItemsSyncBehavior partner)
                {
                    partner._isSyncing = true;
                    PartnerGrid.UnselectAll();
                    partner.SyncSelectedItems();
                    partner._isSyncing = false;
                    break;
                }
            }

            _isSyncing = false;
        }

        SyncSelectedItems();
    }

    private void SyncSelectedItems()
    {
        if (SelectedItems is null) return;
        SelectedItems.Clear();
        foreach (var item in AssociatedObject.SelectedItems)
        {
            SelectedItems.Add(item);
        }
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DoubleClickCommand is not null && AssociatedObject.SelectedItem is { } item
            && DoubleClickCommand.CanExecute(item))
        {
            DoubleClickCommand.Execute(item);
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && DeleteCommand is not null && DeleteCommand.CanExecute(null))
        {
            e.Handled = true;
            DeleteCommand.Execute(null);
        }
    }
}
