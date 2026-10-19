using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public partial class AppointmentListViewModel : PagedListViewModelBase
{
    private readonly AppointmentService _appointments;
    private readonly CarService _cars;

    // ---------- Доступ к сервисам из code-behind ----------
    public AppointmentService AppointmentService => _appointments;
    public CarService CarService => _cars;

    // ---------- Список ----------
    public ObservableCollection<Appointment> Items { get; } = new();

    // ---------- Выбранный элемент ----------
    private Appointment? _selectedAppointment;
    public Appointment? SelectedAppointment
    {
        get => _selectedAppointment;
        set => SetProperty(ref _selectedAppointment, value);
    }

    // ---------- Фильтр по дате ----------
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

    // ---------- Фильтр по статусу ----------
    private AppointmentStatus? _statusFilter;
    public AppointmentStatus? StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) _ = SafeLoadAsync(); }
    }

    public Array AllStatuses { get; } = Enum.GetValues(typeof(AppointmentStatus));

    // ---------- Фильтр по механику ----------
    private int? _mechanicFilter;
    public int? MechanicFilter
    {
        get => _mechanicFilter;
        set { if (SetProperty(ref _mechanicFilter, value)) _ = SafeLoadAsync(); }
    }

    public ObservableCollection<Mechanic> Mechanics { get; } = new();

    // ---------- Сортировка ----------
    private string _sortBy = "date";
    public string SortBy
    {
        get => _sortBy;
        set { if (SetProperty(ref _sortBy, value)) _ = SafeLoadAsync(); }
    }

    // ---------- Конструктор ----------
    public AppointmentListViewModel(AppointmentService appointments, CarService cars)
    {
        _appointments = appointments;
        _cars = cars;

        _ = LoadMechanicsAsync();
        _ = SafeLoadAsync();
    }

    private async Task LoadMechanicsAsync()
    {
        try
        {
            var mech = await _appointments.GetMechanicsAsync();
            Mechanics.Clear();
            foreach (var m in mech) Mechanics.Add(m);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ---------- Загрузка списка ----------
    protected override async Task LoadAsync()
    {
        var result = await _appointments.GetPagedAsync(
            Page, PageSize,
            From, To,
            StatusFilter,
            MechanicFilter,
            sortBy: SortBy);

        Items.Clear();
        foreach (var a in result.Items) Items.Add(a);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    // ---------- Публичная перезагрузка ----------
    public Task ReloadAsync() => SafeLoadAsync();

    // ---------- Удаление ----------
    public async Task<bool> DeleteSelectedAsync()
    {
        if (SelectedAppointment is null) return false;

        try
        {
            var ok = await _appointments.DeleteAsync(SelectedAppointment.AppointmentId);
            if (ok)
            {
                SelectedAppointment = null;
                await SafeLoadAsync();
            }
            return ok;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }
}