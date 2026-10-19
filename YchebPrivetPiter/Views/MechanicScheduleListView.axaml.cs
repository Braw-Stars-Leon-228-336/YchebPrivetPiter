using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class MechanicScheduleListView : UserControl
{
    public MechanicScheduleListView() => InitializeComponent();

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MechanicScheduleListViewModel vm) return;
        await ShowEditDialogAsync(vm, null);
    }

    private async void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MechanicScheduleListViewModel vm) return;
        if (vm.SelectedSchedule is null) return;
        await ShowEditDialogAsync(vm, vm.SelectedSchedule);
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MechanicScheduleListViewModel vm) return;

        if (vm.SelectedSchedule is null)
        {
            await ShowMessageAsync("Выберите слот для удаления", "Внимание");
            return;
        }

        var s = vm.SelectedSchedule;
        var confirm = await ShowConfirmAsync(
            $"Удалить слот {s.WorkDate:dd.MM.yyyy} {s.StartTime:hh\\:mm}–{s.EndTime:hh\\:mm}?",
            "Подтверждение удаления");

        if (!confirm) return;

        var ok = await vm.DeleteSelectedAsync();
        if (!ok)
            await ShowMessageAsync("Не удалось удалить слот.", "Ошибка");
    }

    private async void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MechanicScheduleListViewModel vm) return;
        if (vm.SelectedSchedule is null) return;
        await ShowEditDialogAsync(vm, vm.SelectedSchedule);
    }

    private async Task ShowEditDialogAsync(MechanicScheduleListViewModel listVm, MechanicSchedule? existing)
    {
        var editVm = new MechanicScheduleEditViewModel(listVm.ScheduleService, existing);
        var window = new MechanicScheduleEditWindow { DataContext = editVm };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return;

        await window.ShowDialog(owner);

        if (editVm.IsSaved)
            await listVm.ReloadAsync();
    }

    private async Task<bool> ShowConfirmAsync(string message, string title)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) return false;

        var result = false;
        var dialog = new Window
        {
            Title = title,
            Width = 480,
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