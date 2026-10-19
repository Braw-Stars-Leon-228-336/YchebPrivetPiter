using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class MechanicScheduleListViewModel : PagedListViewModelBase
{
    private readonly MechanicScheduleService _schedules;

    public MechanicScheduleService ScheduleService => _schedules;

    public ObservableCollection<MechanicSchedule> Items { get; } = new();

    // ---------- Фильтры ----------
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

    private int? _mechanicFilter;
    public int? MechanicFilter
    {
        get => _mechanicFilter;
        set { if (SetProperty(ref _mechanicFilter, value)) _ = SafeLoadAsync(); }
    }

    private ScheduleStatus? _statusFilter;
    public ScheduleStatus? StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) _ = SafeLoadAsync(); }
    }

    public Array AllStatuses { get; } = Enum.GetValues(typeof(ScheduleStatus));
    public ObservableCollection<Mechanic> Mechanics { get; } = new();

    // ---------- Выбранный слот ----------
    private MechanicSchedule? _selectedSchedule;
    public MechanicSchedule? SelectedSchedule
    {
        get => _selectedSchedule;
        set => SetProperty(ref _selectedSchedule, value);
    }

    public MechanicScheduleListViewModel(MechanicScheduleService schedules)
    {
        _schedules = schedules;
        _ = LoadMechanicsAsync();
        _ = SafeLoadAsync();
    }

    private async Task LoadMechanicsAsync()
    {
        try
        {
            var mech = await _schedules.GetMechanicsAsync();
            Mechanics.Clear();
            foreach (var m in mech) Mechanics.Add(m);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    protected override async Task LoadAsync()
    {
        var result = await _schedules.GetPagedAsync(
            Page, PageSize, From, To, MechanicFilter, StatusFilter);

        Items.Clear();
        foreach (var s in result.Items) Items.Add(s);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    public Task ReloadAsync() => SafeLoadAsync();

    public async Task<bool> DeleteSelectedAsync()
    {
        if (SelectedSchedule is null) return false;

        try
        {
            var ok = await _schedules.DeleteAsync(SelectedSchedule.ScheduleId);
            if (ok)
            {
                SelectedSchedule = null;
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