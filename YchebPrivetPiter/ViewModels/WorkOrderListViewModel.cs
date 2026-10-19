using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class WorkOrderListViewModel : PagedListViewModelBase
{
    private readonly WorkOrderServices _orders;
    private readonly CarService _cars;
    private readonly InspectionResultService _inspections;
    private readonly User? _currentUser;
    private readonly int? _fixedMechanicId;

    public WorkOrderServices WorkOrderServices => _orders;
    public CarService CarService => _cars;
    public InspectionResultService InspectionResultService => _inspections;
    public User? CurrentUser => _currentUser;

    public ObservableCollection<WorkOrder> Items { get; } = new();

    private WorkOrder? _selectedWorkOrder;
    public WorkOrder? SelectedWorkOrder
    {
        get => _selectedWorkOrder;
        set => SetProperty(ref _selectedWorkOrder, value);
    }

    private WorkOrderStatus? _statusFilter;
    public WorkOrderStatus? StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) ResetAndReload(); }
    }

    public Array AllStatuses { get; } = Enum.GetValues(typeof(WorkOrderStatus));

    private string _sortBy = "date";
    public string SortBy
    {
        get => _sortBy;
        set { if (SetProperty(ref _sortBy, value)) ResetAndReload(); }
    }

    private bool _sortDesc = true;
    public bool SortDesc
    {
        get => _sortDesc;
        set { if (SetProperty(ref _sortDesc, value)) ResetAndReload(); }
    }

    public string Header => _fixedMechanicId.HasValue ? "Мои заказы" : "Заказ-наряды";

    public WorkOrderListViewModel(
        WorkOrderServices orders,
        CarService cars,
        InspectionResultService inspections,
        User? currentUser = null)
    {
        _orders = orders;
        _cars = cars;
        _inspections = inspections;
        _currentUser = currentUser;

        if (currentUser?.Role?.Code == "mechanic" && currentUser.Mechanic != null)
            _fixedMechanicId = currentUser.Mechanic.MechanicId;

        _ = SafeLoadAsync();
    }

    protected override async Task LoadAsync()
    {
        var result = await _orders.GetPagedAsync(
            Page, PageSize,
            StatusFilter,
            _fixedMechanicId,
            sortBy: SortBy, desc: SortDesc);

        Items.Clear();
        foreach (var w in result.Items) Items.Add(w);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    public Task ReloadAsync() => SafeLoadAsync();
}