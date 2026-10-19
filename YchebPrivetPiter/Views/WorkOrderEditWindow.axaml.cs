using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using YchebPrivetPiter.Services;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class WorkOrderEditWindow : Window
{
    private readonly InspectionResultService _inspections;

    public WorkOrderEditWindow(InspectionResultService inspections)
    {
        _inspections = inspections;
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is WorkOrderEditViewModel vm)
                vm.CloseRequested += Close;
        };
    }

    private async void OnInspectionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkOrderEditViewModel vm) return;

        var workOrderId = vm.GetWorkOrderId();
        if (workOrderId == 0)
        {
            await ShowMessageAsync("Сначала сохраните заказ, потом открывайте техосмотр.");
            return;
        }

        var existing = await _inspections.GetByWorkOrderAsync(workOrderId);
        var inspVm = new InspectionEditViewModel(
            _inspections,
            workOrderId,
            vm.CurrentMechanicId,
            existing);

        var win = new InspectionEditWindow { DataContext = inspVm };
        await win.ShowDialog(this);

        if (inspVm.IsSaved)
            await vm.ReloadInspectionAsync();
    }

    private async Task ShowMessageAsync(string message)
    {
        var dialog = new Window
        {
            Title = "Внимание",
            Width = 400,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.Parse("#FF1E1E2E"))
        };

        var ok = new Button
        {
            Content = "OK",
            Background = new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.Parse("#FF7C4DFF")),
            Foreground = Avalonia.Media.Brushes.White,
            Padding = new Avalonia.Thickness(18, 8)
        };
        ok.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    Foreground = Avalonia.Media.Brushes.White,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 14
                },
                new StackPanel
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Children = { ok }
                }
            }
        };

        await dialog.ShowDialog(this);
    }
}