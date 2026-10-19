using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.ViewModels;

namespace YchebPrivetPiter.Views;

public partial class InvoiceListView : UserControl
{
    public InvoiceListView() => InitializeComponent();

    private async void OnPayClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InvoiceListViewModel vm) return;

        if (vm.SelectedInvoice is null)
        {
            await ShowMessageAsync("Выберите счёт для оплаты", "Внимание");
            return;
        }

        if (vm.SelectedInvoice.Status == InvoiceStatus.paid)
        {
            await ShowMessageAsync("Этот счёт уже полностью оплачен", "Внимание");
            return;
        }

        await ShowPaymentDialogAsync(vm, vm.SelectedInvoice);
    }

    private async void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not InvoiceListViewModel vm) return;
        if (vm.SelectedInvoice is null) return;

        if (vm.SelectedInvoice.Status == InvoiceStatus.paid)
        {
            await ShowMessageAsync("Счёт уже полностью оплачен", "Внимание");
            return;
        }

        await ShowPaymentDialogAsync(vm, vm.SelectedInvoice);
    }

    private async void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        // Создание счёта из заказ-наряда — заготовка.
        // Полноценный диалог выбора заказ-наряда сделаем отдельно, если понадобится.
        await ShowMessageAsync(
            "Формирование счёта из заказ-наряда: откройте заказ-наряд и нажмите «Сформировать счёт».",
            "Информация");
    }

    private async Task ShowPaymentDialogAsync(InvoiceListViewModel listVm, Invoice invoice)
    {
        var editVm = new PaymentEditViewModel(listVm.PaymentService, invoice);
        var window = new PaymentEditWindow { DataContext = editVm };

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
            Width = 440,
            Height = 200,
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