using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public partial class MenuItem : ObservableObject
{
    public string Title { get; }
    public string Icon { get; }
    public Func<ViewModelBase> Factory { get; }

    [ObservableProperty] private bool _isSelected;

    public MenuItem(string title, string icon, Func<ViewModelBase> factory)
    {
        Title = title; Icon = icon; Factory = factory;
    }
}

public partial class ShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;
    private readonly MainViewModel _main;
    private readonly Timer _clock;

    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private MenuItem? _selectedMenuItem;
    [ObservableProperty] private string _clockText = DateTime.Now.ToString("HH:mm:ss");

    public User CurrentUser { get; }
    public string UserDisplay => $"{CurrentUser.FullName}  ({RoleName})";
    public string RoleName => CurrentUser.Role?.Code switch
    {
        "admin" => "Администратор",
        "manager" => "Руководитель",
        "mechanic" => "Механик",
        _ => CurrentUser.Role?.Name ?? "—"
    };

    public ObservableCollection<MenuItem> MenuItems { get; } = new();

    public ShellViewModel(User user, MainViewModel main, IServiceProvider sp)
    {
        CurrentUser = user;
        _main = main;
        _sp = sp;
        BuildMenu();

        if (MenuItems.Count > 0) Select(MenuItems[0]);

        _clock = new Timer(_ =>
        {
            var text = DateTime.Now.ToString("HH:mm:ss");
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ClockText = text);
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void BuildMenu()
    {
        switch (CurrentUser.Role?.Code)
        {
            case "admin":
                MenuItems.Add(new MenuItem("Клиенты", "👤", () => _sp.GetRequiredService<ClientListViewModel>()));
                MenuItems.Add(new MenuItem("Автомобили", "🚗", () => _sp.GetRequiredService<CarListViewModel>()));
                MenuItems.Add(new MenuItem("Записи", "📅", () => _sp.GetRequiredService<AppointmentListViewModel>()));
                MenuItems.Add(new MenuItem("Расписание", "🕐", () => _sp.GetRequiredService<MechanicScheduleListViewModel>()));
                MenuItems.Add(new MenuItem("Заказ-наряды", "🔧", () => _sp.GetRequiredService<WorkOrderListViewModel>()));
                MenuItems.Add(new MenuItem("Счета", "💰", () => _sp.GetRequiredService<InvoiceListViewModel>()));
                MenuItems.Add(new MenuItem("Платежи", "💳", () => _sp.GetRequiredService<PaymentListViewModel>()));
                break;

            case "mechanic":
                MenuItems.Add(new MenuItem("Мои заказы", "🔧",
                    () => ActivatorUtilities.CreateInstance<WorkOrderListViewModel>(_sp, CurrentUser)));
                break;

            case "manager":
                MenuItems.Add(new MenuItem("Дашборд", "📊", () => _sp.GetRequiredService<DashboardViewModel>()));
                MenuItems.Add(new MenuItem("Заказ-наряды", "🔧", () => _sp.GetRequiredService<WorkOrderListViewModel>()));
                MenuItems.Add(new MenuItem("Запчасти", "📦", () => _sp.GetRequiredService<PartListViewModel>()));
                MenuItems.Add(new MenuItem("Отзывы", "⭐", () => _sp.GetRequiredService<ReviewListViewModel>()));
                break;
        }
    }

    [RelayCommand]
    private void Select(MenuItem item)
    {
        foreach (var m in MenuItems) m.IsSelected = false;
        item.IsSelected = true;
        SelectedMenuItem = item;
        CurrentPage = item.Factory();
    }

    [RelayCommand]
    private void Logout() => _main.OnLogout();
}