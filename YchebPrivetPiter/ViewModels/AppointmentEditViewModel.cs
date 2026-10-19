using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class AppointmentEditViewModel : ViewModelBase
{
    private readonly AppointmentService _appointments;
    private readonly CarService _cars;
    private readonly int _appointmentId;   // 0 = новая

    public string Title => _appointmentId == 0
        ? "Новая запись на обслуживание"
        : "Редактирование записи";

    // ---------- Справочники ----------
    public ObservableCollection<Client> Clients { get; } = new();
    public ObservableCollection<Car> ClientCars { get; } = new();   // зависит от клиента
    public ObservableCollection<Service> Services { get; } = new();
    public ObservableCollection<Mechanic> Mechanics { get; } = new();
    public ObservableCollection<RepairPlace> RepairPlaces { get; } = new();

    public Array AllStatuses { get; } = Enum.GetValues(typeof(AppointmentStatus));

    // ---------- Выбранные значения ----------
    private Client? _selectedClient;
    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
                _ = LoadClientCarsAsync();
        }
    }

    private Car? _selectedCar;
    public Car? SelectedCar { get => _selectedCar; set => SetProperty(ref _selectedCar, value); }

    private Service? _selectedService;
    public Service? SelectedService { get => _selectedService; set => SetProperty(ref _selectedService, value); }

    private Mechanic? _selectedMechanic;
    public Mechanic? SelectedMechanic { get => _selectedMechanic; set => SetProperty(ref _selectedMechanic, value); }

    private RepairPlace? _selectedPlace;
    public RepairPlace? SelectedPlace { get => _selectedPlace; set => SetProperty(ref _selectedPlace, value); }

    private AppointmentStatus _selectedStatus = AppointmentStatus.planned;
    public AppointmentStatus SelectedStatus { get => _selectedStatus; set => SetProperty(ref _selectedStatus, value); }

    // ---------- Дата/время ----------
    private DateTimeOffset? _date = DateTimeOffset.Now;
    public DateTimeOffset? Date { get => _date; set => SetProperty(ref _date, value); }

    private TimeSpan? _time = DateTime.Now.TimeOfDay;
    public TimeSpan? Time { get => _time; set => SetProperty(ref _time, value); }

    private string? _comment;
    public string? Comment { get => _comment; set => SetProperty(ref _comment, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public bool IsSaved { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? CloseRequested;

    // Поля из существующей записи — чтобы подставить после загрузки справочников
    private readonly int? _presetClientId;
    private readonly int? _presetCarId;
    private readonly int? _presetServiceId;
    private readonly int? _presetMechanicId;
    private readonly int? _presetPlaceId;

    public AppointmentEditViewModel(AppointmentService appointments, CarService cars,
        Appointment? existing = null)
    {
        _appointments = appointments;
        _cars = cars;
        _appointmentId = existing?.AppointmentId ?? 0;

        if (existing is not null)
        {
            _presetClientId = existing.ClientId;
            _presetCarId = existing.CarId;
            _presetServiceId = existing.ServiceId;
            _presetMechanicId = existing.MechanicId;
            _presetPlaceId = existing.RepairPlaceId;

            _selectedStatus = existing.Status;
            _date = new DateTimeOffset(existing.ScheduledAt, TimeSpan.Zero);
            _time = existing.ScheduledAt.TimeOfDay;
            _comment = existing.Comment;
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
            var clients = await _appointments.GetClientsAsync();
            Clients.Clear();
            foreach (var c in clients) Clients.Add(c);

            var services = await _appointments.GetServicesAsync();
            Services.Clear();
            foreach (var s in services) Services.Add(s);

            var mechanics = await _appointments.GetMechanicsAsync();
            Mechanics.Clear();
            foreach (var m in mechanics) Mechanics.Add(m);

            var places = await _appointments.GetRepairPlacesAsync();
            RepairPlaces.Clear();
            foreach (var p in places) RepairPlaces.Add(p);

            // Восстановление значений для редактирования
            if (_presetClientId.HasValue)
                SelectedClient = Clients.FirstOrDefault(c => c.ClientId == _presetClientId.Value);

            // Авто загрузятся триггером от SelectedClient,
            // но если их надо восстановить — сделаем это после загрузки ClientCars
            if (_presetCarId.HasValue)
            {
                await Task.Delay(50);   // небольшая пауза, чтобы LoadClientCarsAsync успел
                SelectedCar = ClientCars.FirstOrDefault(c => c.CarId == _presetCarId.Value);
            }

            if (_presetServiceId.HasValue)
                SelectedService = Services.FirstOrDefault(s => s.ServiceId == _presetServiceId.Value);

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

    private async Task LoadClientCarsAsync()
    {
        ClientCars.Clear();
        SelectedCar = null;

        if (SelectedClient is null) return;

        try
        {
            var cars = await _cars.GetByClientAsync(SelectedClient.ClientId);
            foreach (var c in cars) ClientCars.Add(c);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (SelectedClient is null) { ErrorMessage = "Выберите клиента"; return; }
        if (SelectedCar is null) { ErrorMessage = "Выберите автомобиль"; return; }
        if (SelectedService is null) { ErrorMessage = "Выберите услугу"; return; }
        if (Date is null) { ErrorMessage = "Укажите дату"; return; }
        if (Time is null) { ErrorMessage = "Укажите время"; return; }

        IsBusy = true;
        try
        {
            var date = Date.Value.Date;
            var time = Time.Value;
            var scheduledAt = new DateTime(
                date.Year, date.Month, date.Day,
                time.Hours, time.Minutes, 0, DateTimeKind.Utc);

            var entity = new Appointment
            {
                AppointmentId = _appointmentId,
                ClientId = SelectedClient.ClientId,
                CarId = SelectedCar.CarId,
                ServiceId = SelectedService.ServiceId,
                MechanicId = SelectedMechanic?.MechanicId,
                RepairPlaceId = SelectedPlace?.PlaceId,
                ScheduledAt = scheduledAt,
                Status = SelectedStatus,
                Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim()
            };

            if (_appointmentId == 0)
                await _appointments.CreateAsync(entity);
            else
                await _appointments.UpdateAsync(entity);

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