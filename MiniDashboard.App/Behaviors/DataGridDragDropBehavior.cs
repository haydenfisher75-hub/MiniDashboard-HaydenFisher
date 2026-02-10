using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using MiniDashboard.DTOs.Classes;
using MiniDashboard.App.Views;

namespace MiniDashboard.App.Behaviors;

public class DataGridDragDropBehavior : Behavior<DataGrid>
{
    private Point _dragStartPoint;
    private bool _isDragging;
    private DragAdorner? _dragAdorner;
    private AdornerLayer? _adornerLayer;

    public static readonly DependencyProperty PartnerGridProperty =
        DependencyProperty.Register(nameof(PartnerGrid), typeof(DataGrid), typeof(DataGridDragDropBehavior));

    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.Register(nameof(DropCommand), typeof(ICommand), typeof(DataGridDragDropBehavior));

    public DataGrid? PartnerGrid
    {
        get => (DataGrid?)GetValue(PartnerGridProperty);
        set => SetValue(PartnerGridProperty, value);
    }

    public ICommand? DropCommand
    {
        get => (ICommand?)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragOver += OnDragOver;
        AssociatedObject.Drop += OnDrop;
        AssociatedObject.GiveFeedback += OnGiveFeedback;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
        AssociatedObject.DragOver -= OnDragOver;
        AssociatedObject.Drop -= OnDrop;
        AssociatedObject.GiveFeedback -= OnGiveFeedback;
        base.OnDetaching();
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(GetAdornerHost());
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
            return;

        var host = GetAdornerHost();
        var currentPos = e.GetPosition(host);
        var diff = currentPos - _dragStartPoint;

        if (Math.Abs(diff.X) < 5 && Math.Abs(diff.Y) < 5)
            return;

        var items = AssociatedObject.SelectedItems.Cast<ItemDto>().ToList();
        if (items.Count == 0) return;

        _isDragging = true;

        _adornerLayer = AdornerLayer.GetAdornerLayer(host);
        if (_adornerLayer != null)
        {
            _dragAdorner = new DragAdorner(host, items);
            _dragAdorner.UpdatePosition(e.GetPosition(host));
            _adornerLayer.Add(_dragAdorner);
        }

        var data = new DataObject("DragItems", items);
        data.SetData("SourceGrid", AssociatedObject.Name);

        DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);

        RemoveAdorner();
        _isDragging = false;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("DragItems") || !e.Data.GetDataPresent("SourceGrid"))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var sourceGridName = (string)e.Data.GetData("SourceGrid");
        var canDrop = AssociatedObject.Name != sourceGridName;

        e.Effects = canDrop ? DragDropEffects.Move : DragDropEffects.None;

        if (_dragAdorner != null)
        {
            _dragAdorner.SetCanDrop(canDrop);
            _dragAdorner.UpdatePosition(e.GetPosition(GetAdornerHost()));
        }
        else if (canDrop)
        {
            // The adorner is on the source behavior's adorner layer.
            // Find it via the partner grid's behavior.
            var partnerBehavior = GetPartnerDragDropBehavior();
            if (partnerBehavior?._dragAdorner != null)
            {
                partnerBehavior._dragAdorner.SetCanDrop(true);
                partnerBehavior._dragAdorner.UpdatePosition(e.GetPosition(GetAdornerHost()));
            }
        }

        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("DragItems") || !e.Data.GetDataPresent("SourceGrid"))
            return;

        var sourceGridName = (string)e.Data.GetData("SourceGrid");
        if (AssociatedObject.Name == sourceGridName) return;

        var items = (List<ItemDto>)e.Data.GetData("DragItems");
        if (items.Count == 0) return;

        if (DropCommand is not null && DropCommand.CanExecute(items))
        {
            DropCommand.Execute(items);
        }

        e.Handled = true;
    }

    private void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        e.UseDefaultCursors = false;
        Mouse.SetCursor(Cursors.Arrow);
        e.Handled = true;
    }

    private UIElement GetAdornerHost()
    {
        // Walk up to find the UserControl as the adorner host
        FrameworkElement? element = AssociatedObject;
        while (element != null)
        {
            if (element is UserControl uc) return uc;
            element = element.Parent as FrameworkElement;
        }
        return AssociatedObject;
    }

    private DataGridDragDropBehavior? GetPartnerDragDropBehavior()
    {
        if (PartnerGrid is null) return null;
        var behaviors = Interaction.GetBehaviors(PartnerGrid);
        foreach (var b in behaviors)
        {
            if (b is DataGridDragDropBehavior ddBehavior)
                return ddBehavior;
        }
        return null;
    }

    private void RemoveAdorner()
    {
        if (_dragAdorner != null && _adornerLayer != null)
        {
            _adornerLayer.Remove(_dragAdorner);
            _dragAdorner = null;
            _adornerLayer = null;
        }
    }
}
