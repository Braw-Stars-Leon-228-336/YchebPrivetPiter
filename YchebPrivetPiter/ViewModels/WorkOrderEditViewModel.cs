using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class WorkOrderEditViewModel : ViewModelBase
{
    private readonly WorkOrderServices _orders;
    private readonly CarService _cars;
    private readonly int _workOrderId;

    public string Title => _workOrderId == 0 ? "Новый заказ-наряд" : "Заказ-наряд";

    // ---------- Текущий механик / осмотр ----------
    public int? CurrentMechanicId { get; }

    private InspectionResult? _existingInspection;
    public bool HasInspection => _existingInspection is not null;
    public string InspectionButtonText => HasInspection
        ? "🔍 Техосмотр (редактировать)"
        : "🔍 Техосмотр";

    // ---------- Справочники ----------
    public ObservableCollection<Client> Clients { get; } = new();
    public ObservableCollection<Car> ClientCars { get; } = new();
    public ObservableCollection<Mechanic> Mechanics { get; } = new();
    public ObservableCollection<Service> AllServices { get; } = new();
    public ObservableCollection<Part> AllParts { get; } = new();

    public Array AllStatuses { get; } = Enum.GetValues(typeof(WorkOrderStatus));

    // ---------- Поля заказа ----------
    private Client? _selectedClient;
    public Client? SelectedClient
    {
        get => _selectedClient;
        set { if (SetProperty(ref _selectedClient, value)) _ = LoadClientCarsAsync(); }
    }

    private Car? _selectedCar;
    public Car? SelectedCar { get => _selectedCar; set => SetProperty(ref _selectedCar, value); }

    private Mechanic? _selectedMechanic;
    public Mechanic? SelectedMechanic { get => _selectedMechanic; set => SetProperty(ref _selectedMechanic, value); }

    private WorkOrderStatus _status = WorkOrderStatus.draft;
    public WorkOrderStatus Status { get => _status; set => SetProperty(ref _status, value); }

    private string? _description;
    public string? Description { get => _description; set => SetProperty(ref _description, value); }

    // ---------- Услуги и запчасти в заказе ----------
    public ObservableCollection<WorkOrderService> Services { get; } = new();
    public ObservableCollection<WorkOrderPart> Parts { get; } = new();

    // ---------- Выбор для добавления ----------
    private Service? _newService;
    public Service? NewService { get => _newService; set => SetProperty(ref _newService, value); }

    private int _newServiceQty = 1;
    public int NewServiceQty { get => _newServiceQty; set => SetProperty(ref _newServiceQty, value); }

    private decimal _newServicePrice;
    public decimal NewServicePrice { get => _newServicePrice; set => SetProperty(ref _newServicePrice, value); }

    private Part? _newPart;
    public Part? NewPart { get => _newPart; set => SetProperty(ref _newPart, value); }

    private int _newPartQty = 1;
    public int NewPartQty { get => _newPartQty; set => SetProperty(ref _newPartQty, value); }

    private decimal _newPartPrice;
    public decimal NewPartPrice { get => _newPartPrice; set => SetProperty(ref _newPartPrice, value); }

    // ---------- Итоги ----------
    public decimal ServicesTotal => Services.Sum(s => s.Quantity * s.UnitPrice);
    public decimal PartsTotal => Parts.Sum(p => p.Quantity * p.UnitPrice);
    public decimal GrandTotal => ServicesTotal + PartsTotal;

    // ---------- Выбранные строки ----------
    private WorkOrderService? _selectedServiceRow;
    public WorkOrderService? SelectedServiceRow
    {
        get => _selectedServiceRow;
        set => SetProperty(ref _selectedServiceRow, value);
    }

    private WorkOrderPart? _selectedPartRow;
    public WorkOrderPart? SelectedPartRow
    {
        get => _selectedPartRow;
        set => SetProperty(ref _selectedPartRow, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public bool IsSaved { get; private set; }

    public ICommand AddServiceCommand { get; }
    public ICommand RemoveServiceCommand { get; }
    public ICommand AddPartCommand { get; }
    public ICommand RemovePartCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? CloseRequested;

    private readonly int? _presetClientId;
    private readonly int? _presetCarId;
    private readonly int? _presetMechanicId;

    // ---------- Конструктор ----------
    public WorkOrderEditViewModel(
        WorkOrderServices orders,
        CarService cars,
        WorkOrder? existing = null,
        int? currentMechanicId = null)
    {
        _orders = orders;
        _cars = cars;
        _workOrderId = existing?.WorkOrderId ?? 0;
        CurrentMechanicId = currentMechanicId;

        if (existing is not null)
        {
            _presetClientId = existing.ClientId;
            _presetCarId = existing.CarId;
            _presetMechanicId = existing.MechanicId;
            _status = existing.Status;
            _description = existing.Description;
        }

        AddServiceCommand = new RelayCommand(AddService);
        RemoveServiceCommand = new RelayCommand(RemoveService);
        AddPartCommand = new RelayCommand(AddPart);
        RemovePartCommand = new RelayCommand(RemovePart);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());

        _ = LoadAllAsync(existing);
    }

    // ---------- Загрузка ----------
    private async Task LoadAllAsync(WorkOrder? existing)
    {
        IsBusy = true;
        try
        {
            var clients = await _orders.GetClientsAsync();
            Clients.Clear();
            foreach (var c in clients) Clients.Add(c);

            var mech = await _orders.GetMechanicsAsync();
            Mechanics.Clear();
            foreach (var m in mech) Mechanics.Add(m);

            var services = await _orders.GetServicesAsync();
            AllServices.Clear();
            foreach (var s in services) AllServices.Add(s);

            var parts = await _orders.GetPartsAsync();
            AllParts.Clear();
            foreach (var p in parts) AllParts.Add(p);

            if (existing is not null)
            {
                if (_presetClientId.HasValue)
                    SelectedClient = Clients.FirstOrDefault(c => c.ClientId == _presetClientId.Value);

                await Task.Delay(80);   // даём LoadClientCarsAsync шанс отработать
                if (_presetCarId.HasValue)
                    SelectedCar = ClientCars.FirstOrDefault(c => c.CarId == _presetCarId.Value);

                if (_presetMechanicId.HasValue)
                    SelectedMechanic = Mechanics.FirstOrDefault(m => m.MechanicId == _presetMechanicId.Value);

                var wo = await _orders.GetByIdAsync(existing.WorkOrderId);
                if (wo is not null)
                {
                    Services.Clear();
                    foreach (var s in wo.WorkOrderServices) Services.Add(s);

                    Parts.Clear();
                    foreach (var p in wo.WorkOrderParts) Parts.Add(p);

                    // ---------- Осмотр ----------
                    _existingInspection = wo.InspectionResults?
                        .OrderByDescending(i => i.CreatedAt)
                        .FirstOrDefault();

                    OnPropertyChanged(nameof(HasInspection));
                    OnPropertyChanged(nameof(InspectionButtonText));

                    RefreshTotals();
                }
            }
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
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    // ---------- Услуги ----------
    private void AddService()
    {
        if (NewService is null) return;

        var existing = Services.FirstOrDefault(s =>
            s.ServiceId == NewService.ServiceId && s.MechanicId == SelectedMechanic?.MechanicId);

        if (existing is not null)
        {
            existing.Quantity += NewServiceQty;
        }
        else
        {
            Services.Add(new WorkOrderService
            {
                ServiceId = NewService.ServiceId,
                Service = NewService,
                MechanicId = SelectedMechanic?.MechanicId,
                Quantity = NewServiceQty,
                UnitPrice = NewServicePrice > 0 ? NewServicePrice : NewService.BasePrice
            });
        }

        NewService = null;
        NewServiceQty = 1;
        NewServicePrice = 0;
        RefreshTotals();
    }

    private void RemoveService()
    {
        if (SelectedServiceRow is null) return;
        Services.Remove(SelectedServiceRow);
        SelectedServiceRow = null;
        RefreshTotals();
    }

    // ---------- Запчасти ----------
    private void AddPart()
    {
        if (NewPart is null) return;

        var existing = Parts.FirstOrDefault(p => p.PartId == NewPart.PartId);
        if (existing is not null)
        {
            existing.Quantity += NewPartQty;
        }
        else
        {
            Parts.Add(new WorkOrderPart
            {
                PartId = NewPart.PartId,
                Part = NewPart,
                Quantity = NewPartQty,
                UnitPrice = NewPartPrice > 0 ? NewPartPrice : NewPart.SalePrice
            });
        }

        NewPart = null;
        NewPartQty = 1;
        NewPartPrice = 0;
        RefreshTotals();
    }

    private void RemovePart()
    {
        if (SelectedPartRow is null) return;
        Parts.Remove(SelectedPartRow);
        SelectedPartRow = null;
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(ServicesTotal));
        OnPropertyChanged(nameof(PartsTotal));
        OnPropertyChanged(nameof(GrandTotal));
    }

    // ---------- Сохранение ----------
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (SelectedClient is null) { ErrorMessage = "Выберите клиента"; return; }
        if (SelectedCar is null) { ErrorMessage = "Выберите автомобиль"; return; }

        IsBusy = true;
        try
        {
            var wo = new WorkOrder
            {
                WorkOrderId = _workOrderId,
                ClientId = SelectedClient.ClientId,
                CarId = SelectedCar.CarId,
                MechanicId = SelectedMechanic?.MechanicId,
                Status = Status,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
            };

            await _orders.SaveWithDetailsAsync(wo, Services.ToList(), Parts.ToList());

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

    // ---------- Методы для окна (открытие техосмотра) ----------
    public int GetWorkOrderId() => _workOrderId;

    public async Task ReloadInspectionAsync()
    {
        if (_workOrderId == 0) return;

        var wo = await _orders.GetByIdAsync(_workOrderId);
        _existingInspection = wo?.InspectionResults?
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefault();

        OnPropertyChanged(nameof(HasInspection));
        OnPropertyChanged(nameof(InspectionButtonText));
    }
}