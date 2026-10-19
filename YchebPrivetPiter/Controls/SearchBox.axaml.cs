using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace YchebPrivetPiter.Controls;

public partial class SearchBox : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(Text),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(Watermark), "Поиск...");

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public static readonly StyledProperty<System.Windows.Input.ICommand?> ClearCommandProperty =
        AvaloniaProperty.Register<SearchBox, System.Windows.Input.ICommand?>(nameof(ClearCommand));

    public System.Windows.Input.ICommand? ClearCommand
    {
        get => GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    public SearchBox()
    {
        InitializeComponent();
        ClearCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Text = null);
    }
}