using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MiniDashboard.DTOs.Classes;

namespace MiniDashboard.App.Views;

public class DragAdorner : Adorner
{
    private readonly List<string> _lines;
    private Point _offset;
    private Brush _borderBrush = Brushes.Red;

    public DragAdorner(UIElement adornedElement, List<ItemDto> items) : base(adornedElement)
    {
        IsHitTestVisible = false;
        _lines = items.Select(i => $"{i.ProductCode} - {i.Name}").ToList();
    }

    public void UpdatePosition(Point position)
    {
        _offset = position;
        InvalidateVisual();
    }

    public void SetCanDrop(bool canDrop)
    {
        _borderBrush = canDrop ? Brushes.Green : Brushes.Red;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var typeface = new Typeface("Segoe UI");
        const double fontSize = 12;
        const double padding = 8;
        const double lineHeight = 18;

        double maxWidth = 0;
        var formattedTexts = new List<FormattedText>();

        foreach (var line in _lines)
        {
            var ft = new FormattedText(line, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            formattedTexts.Add(ft);
            if (ft.Width > maxWidth) maxWidth = ft.Width;
        }

        var rectWidth = maxWidth + padding * 2;
        var rectHeight = _lines.Count * lineHeight + padding * 2;

        var rect = new Rect(_offset.X + 12, _offset.Y + 12, rectWidth, rectHeight);

        dc.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
            new Pen(_borderBrush, 2), rect, 4, 4);

        for (int i = 0; i < formattedTexts.Count; i++)
        {
            dc.DrawText(formattedTexts[i],
                new Point(rect.X + padding, rect.Y + padding + i * lineHeight));
        }
    }
}
