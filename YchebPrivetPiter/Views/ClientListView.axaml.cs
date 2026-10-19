using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class ClientListView : UserControl
{
    public ClientListView() => InitializeComponent();

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ClientListViewModel vm) return;
        await ShowEditDialogAsync(vm, null);
    }

    private async void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ClientListViewModel vm) return;
        if (vm.SelectedClient is null) return;
        await ShowEditDialogAsync(vm, vm.SelectedClient);
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ClientListViewModel vm) return;

        if (vm.SelectedClient is null)
        {
            await ShowMessageAsync("Выберите клиента для удаления", "Внимание");
            return;
        }

        var client = vm.SelectedClient;

        var confirm = await ShowConfirmAsync(
            $"Удалить клиента «{client.FullName}»?\n" +
            "Все его автомобили, записи и заказы тоже будут удалены (каскадно).",
            "Подтверждение удаления");

        if (!confirm) return;

        var ok = await vm.DeleteSelectedAsync();
        if (!ok)
        {
            await ShowMessageAsync("Не удалось удалить клиента. Возможно, есть связанные заказы.",
                "Ошибка");
        }
    }

    private async void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ClientListViewModel vm) return;
        if (vm.SelectedClient is null) return;
        await ShowEditDialogAsync(vm, vm.SelectedClient);
    }

    private async Task ShowEditDialogAsync(ClientListViewModel listVm, Client? client)
    {
        var editVm = new ClientEditViewModel(listVm.ClientService, client);
        var window = new ClientEditWindow { DataContext = editVm };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        await window.ShowDialog(owner);

        if (editVm.IsSaved)
            await listVm.ReloadAsync();
    }

    // ---------- helpers ----------

    private async Task<bool> ShowConfirmAsync(string message, string title)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return false;

        var result = false;
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#FF1E1E2E"))
        };

        var yes = new Button
        {
            Content = "Удалить",
            Background = new SolidColorBrush(Color.Parse("#FFEF5350")),
            Foreground = Brushes.White,
            Padding = new Avalonia.Thickness(18, 8)
        };
        var no = new Button
        {
            Content = "Отмена",
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#55FFFFFF")),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(18, 8)
        };

        yes.Click += (_, _) => { result = true; dialog.Close(); };
        no.Click += (_, _) => { result = false; dialog.Close(); };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 20,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { no, yes }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
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
            Background = new SolidColorBrush(Color.Parse("#FF1E1E2E"))
        };

        var ok = new Button
        {
            Content = "OK",
            Background = new SolidColorBrush(Color.Parse("#FF7C4DFF")),
            Foreground = Brushes.White,
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
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                },
                new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { ok }
                }
            }
        };

        await dialog.ShowDialog(owner);
    }
}