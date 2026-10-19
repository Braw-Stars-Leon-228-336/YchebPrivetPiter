using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class WorkOrderListView : UserControl
{
    public WorkOrderListView() => InitializeComponent();

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkOrderListViewModel vm) return;
        await ShowEditDialogAsync(vm, null);
    }

    private async void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkOrderListViewModel vm) return;
        if (vm.SelectedWorkOrder is null)
        {
            await ShowMessageAsync("Выберите заказ для открытия", "Внимание");
            return;
        }
        await ShowEditDialogAsync(vm, vm.SelectedWorkOrder);
    }

    private async void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not WorkOrderListViewModel vm) return;
        if (vm.SelectedWorkOrder is null) return;
        await ShowEditDialogAsync(vm, vm.SelectedWorkOrder);
    }

    private async Task ShowEditDialogAsync(WorkOrderListViewModel listVm, WorkOrder? existing)
    {
        // Передаём InspectionResultService и текущего механика (если вошёл механик)
        var editVm = new WorkOrderEditViewModel(
            listVm.WorkOrderServices,
            listVm.CarService,
            existing,
            listVm.CurrentUser?.Mechanic?.MechanicId);

        var window = new WorkOrderEditWindow(listVm.InspectionResultService)
        {
            DataContext = editVm
        };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        await window.ShowDialog(owner);

        if (editVm.IsSaved)
            await listVm.ReloadAsync();
    }

    private async Task ShowMessageAsync(string message, string title)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
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
            Spacing = 20,
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

        await dialog.ShowDialog(owner);
    }
}