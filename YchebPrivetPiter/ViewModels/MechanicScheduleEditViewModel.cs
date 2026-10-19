using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class MechanicScheduleEditViewModel : ViewModelBase
{
    private readonly MechanicScheduleService _schedules;
    private readonly int _scheduleId;

    public string Title => _scheduleId == 0
        ? "Новый слот расписания"
        : "Редактирование слота";

    public ObservableCollection<Mechanic> Mechanics { get; } = new();
    public ObservableCollection<RepairPlace> RepairPlaces { get; } = new();

    public Array AllStatuses { get; } = Enum.GetValues(typeof(ScheduleStatus));

    private Mechanic? _selectedMechanic;
    public Mechanic? SelectedMechanic { get => _selectedMechanic; set => SetProperty(ref _selectedMechanic, value); }

    private RepairPlace? _selectedPlace;
    public RepairPlace? SelectedPlace { get => _selectedPlace; set => SetProperty(ref _selectedPlace, value); }

    private DateTimeOffset? _date = DateTimeOffset.Now;
    public DateTimeOffset? Date { get => _date; set => SetProperty(ref _date, value); }

    private TimeSpan? _startTime = new TimeSpan(9, 0, 0);
    public TimeSpan? StartTime { get => _startTime; set => SetProperty(ref _startTime, value); }

    private TimeSpan? _endTime = new TimeSpan(18, 0, 0);
    public TimeSpan? EndTime { get => _endTime; set => SetProperty(ref _endTime, value); }

    private ScheduleStatus _status = ScheduleStatus.available;
    public ScheduleStatus Status { get => _status; set => SetProperty(ref _status, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public bool IsSaved { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? CloseRequested;

    private readonly int? _presetMechanicId;
    private readonly int? _presetPlaceId;

    public MechanicScheduleEditViewModel(MechanicScheduleService schedules,
        MechanicSchedule? existing = null)
    {
        _schedules = schedules;
        _scheduleId = existing?.ScheduleId ?? 0;

        if (existing is not null)
        {
            _presetMechanicId = existing.MechanicId;
            _presetPlaceId = existing.RepairPlaceId;
            _date = new DateTimeOffset(existing.WorkDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            _startTime = existing.StartTime.ToTimeSpan();
            _endTime = existing.EndTime.ToTimeSpan();
            _status = existing.Status;
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());

        _ = LoadReferencesAsync();
    }

    private async Task LoadReferencesAsync()
    {
        IsBusy = true;
        try
        {
            var mech = await _schedules.GetMechanicsAsync();
            Mechanics.Clear();
            foreach (var m in mech) Mechanics.Add(m);

            var places = await _schedules.GetRepairPlacesAsync();
            RepairPlaces.Clear();
            foreach (var p in places) RepairPlaces.Add(p);

            if (_presetMechanicId.HasValue)
                SelectedMechanic = Mechanics.FirstOrDefault(m => m.MechanicId == _presetMechanicId.Value);

            if (_presetPlaceId.HasValue)
                SelectedPlace = RepairPlaces.FirstOrDefault(p => p.PlaceId == _presetPlaceId.Value);
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

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (SelectedMechanic is null) { ErrorMessage = "Выберите механика"; return; }
        if (Date is null) { ErrorMessage = "Укажите дату"; return; }
        if (StartTime is null) { ErrorMessage = "Укажите время начала"; return; }
        if (EndTime is null) { ErrorMessage = "Укажите время окончания"; return; }
        if (EndTime <= StartTime) { ErrorMessage = "Время окончания должно быть позже начала"; return; }

        IsBusy = true;
        try
        {
            var entity = new MechanicSchedule
            {
                ScheduleId = _scheduleId,
                MechanicId = SelectedMechanic.MechanicId,
                RepairPlaceId = SelectedPlace?.PlaceId,
                WorkDate = DateOnly.FromDateTime(Date.Value.Date),
                StartTime = TimeOnly.FromTimeSpan(StartTime.Value),
                EndTime = TimeOnly.FromTimeSpan(EndTime.Value),
                Status = Status
            };

            if (_scheduleId == 0)
                await _schedules.CreateAsync(entity);
            else
                await _schedules.UpdateAsync(entity);

            IsSaved = true;
            CloseRequested?.Invoke();
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
}