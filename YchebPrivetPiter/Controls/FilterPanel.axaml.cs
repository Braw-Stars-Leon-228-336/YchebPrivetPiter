using Avalonia;
using Avalonia.Controls;

namespace YchebPrivetPiter.Controls;

public partial class FilterPanel : UserControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<FilterPanel, string?>(nameof(Header), "Фильтры");

    public static readonly StyledProperty<object?> FilterContentProperty =
        AvaloniaProperty.Register<FilterPanel, object?>(nameof(FilterContent));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? FilterContent
    {
        get => GetValue(FilterContentProperty);
        set => SetValue(FilterContentProperty, value);
    }

    public FilterPanel() => InitializeComponent();
}