using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public partial class InvoiceListViewModel : PagedListViewModelBase
{
    private readonly PaymentService _payments;
    public PaymentService PaymentService => _payments;

    private Invoice? _selectedInvoice;
    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set => SetProperty(ref _selectedInvoice, value);
    }

    public Task ReloadAsync() => SafeLoadAsync();

    private readonly InvoiceService _invoices;

    public ObservableCollection<Invoice> Items { get; } = new();

    private InvoiceStatus? _statusFilter;
    public InvoiceStatus? StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) _ = SafeLoadAsync(); }
    }

    public Array AllStatuses { get; } = Enum.GetValues(typeof(InvoiceStatus));

    private string _sortBy = "date";
    public string SortBy
    {
        get => _sortBy;
        set { if (SetProperty(ref _sortBy, value)) _ = SafeLoadAsync(); }
    }

    public InvoiceListViewModel(InvoiceService invoices, PaymentService payments)
    {
        _invoices = invoices;
        _payments = payments;
        _ = SafeLoadAsync();
    }

    protected override async Task LoadAsync()
    {
        var result = await _invoices.GetPagedAsync(Page, PageSize, StatusFilter, sortBy: SortBy);

        Items.Clear();
        foreach (var i in result.Items) Items.Add(i);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }
}