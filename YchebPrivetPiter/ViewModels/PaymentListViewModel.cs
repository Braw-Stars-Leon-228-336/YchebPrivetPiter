using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public partial class PaymentListViewModel : PagedListViewModelBase
{
    private readonly PaymentService _payments;

    public ObservableCollection<Payment> Items { get; } = new();

    private DateTime? _from;
    public DateTime? From
    {
        get => _from;
        set { if (SetProperty(ref _from, value)) _ = SafeLoadAsync(); }
    }

    private DateTime? _to;
    public DateTime? To
    {
        get => _to;
        set { if (SetProperty(ref _to, value)) _ = SafeLoadAsync(); }
    }

    private PaymentMethod? _methodFilter;
    public PaymentMethod? MethodFilter
    {
        get => _methodFilter;
        set { if (SetProperty(ref _methodFilter, value)) _ = SafeLoadAsync(); }
    }

    private PaymentStatus? _statusFilter;
    public PaymentStatus? StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) _ = SafeLoadAsync(); }
    }

    public Array AllMethods { get; } = Enum.GetValues(typeof(PaymentMethod));
    public Array AllStatuses { get; } = Enum.GetValues(typeof(PaymentStatus));

    public PaymentListViewModel(PaymentService payments)
    {
        _payments = payments;
        _ = SafeLoadAsync();
    }

    protected override async Task LoadAsync()
    {
        var result = await _payments.GetPagedAsync(Page, PageSize, From, To, MethodFilter, StatusFilter);

        Items.Clear();
        foreach (var p in result.Items) Items.Add(p);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }
}