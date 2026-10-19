using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly AnalyticsService _analytics;
    private readonly ReviewService _reviews;

    public DashboardViewModel(AnalyticsService analytics, ReviewService reviews)
    {
        _analytics = analytics;
        _reviews = reviews;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadAsync();
    }

    public ICommand RefreshCommand { get; }

    // ---------- KPI ----------
    private int _ordersCount;
    public int OrdersCount { get => _ordersCount; set => SetProperty(ref _ordersCount, value); }

    private decimal _revenue;
    public decimal Revenue { get => _revenue; set => SetProperty(ref _revenue, value); }

    private decimal _averageRepair;
    public decimal AverageRepair { get => _averageRepair; set => SetProperty(ref _averageRepair, value); }

    private int _lowStockCount;
    public int LowStockCount { get => _lowStockCount; set => SetProperty(ref _lowStockCount, value); }

    private double _averageRating;
    public double AverageRating { get => _averageRating; set => SetProperty(ref _averageRating, value); }

    private int _reviewsCount;
    public int ReviewsCount { get => _reviewsCount; set => SetProperty(ref _reviewsCount, value); }

    // ---------- Коллекции ----------
    public ObservableCollection<TopServiceDto> TopServices { get; } = new();
    public ObservableCollection<MechanicWorkloadDto> Workload { get; } = new();
    public ObservableCollection<RepeatClientDto> RepeatClients { get; } = new();
    public ObservableCollection<Part> LowStockParts { get; } = new();

    // ---------- Данные для графика (ручная отрисовка) ----------
    public ObservableCollection<ChartBarItem> RevenueBars { get; } = new();

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    // ---------- Загрузка ----------
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            OrdersCount = await _analytics.GetOrdersCountAsync();
            Revenue = await _analytics.GetRevenueAsync();
            AverageRepair = await _analytics.GetAverageRepairCostAsync();
            AverageRating = await _analytics.GetAverageRatingAsync();
            ReviewsCount = await _analytics.GetReviewsCountAsync();

            var lowStock = await _analytics.GetLowStockPartsAsync();
            LowStockCount = lowStock.Count;

            var top = await _analytics.GetTopServicesAsync(5);
            TopServices.Clear();
            foreach (var t in top) TopServices.Add(t);

            var workload = await _analytics.GetMechanicsWorkloadAsync();
            Workload.Clear();
            foreach (var w in workload) Workload.Add(w);

            var repeat = await _analytics.GetRepeatClientsAsync(2);
            RepeatClients.Clear();
            foreach (var r in repeat) RepeatClients.Add(r);

            LowStockParts.Clear();
            foreach (var p in lowStock) LowStockParts.Add(p);

            await LoadChartAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadChartAsync()
    {
        var data = await _analytics.GetDailyRevenueAsync(30);
        if (data.Count == 0)
        {
            RevenueBars.Clear();
            return;
        }

        var max = data.Max(d => (double)d.Revenue);
        if (max <= 0) max = 1;

        RevenueBars.Clear();
        foreach (var d in data)
        {
            RevenueBars.Add(new ChartBarItem
            {
                Label = d.Date.ToString("dd.MM"),
                Value = (double)d.Revenue,
                HeightPercent = (double)d.Revenue / max * 180.0   // высота в пикселях
            });
        }
    }
}