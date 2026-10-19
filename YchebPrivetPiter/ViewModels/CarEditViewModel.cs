using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class CarEditViewModel : ViewModelBase
{
    private readonly CarService _cars;
    private readonly int _carId;   // 0 = новая

    public string Title => _carId == 0 ? "Новый автомобиль" : "Редактирование автомобиля";

    // ---------- ComboBox источники ----------
    public ObservableCollection<Client> Clients { get; } = new();
    public ObservableCollection<CarBrand> Brands { get; } = new();
    public ObservableCollection<CarCategory> Categories { get; } = new();

    // ---------- Поля ----------
    private Client? _selectedClient;
    public Client? SelectedClient { get => _selectedClient; set => SetProperty(ref _selectedClient, value); }

    private CarBrand? _selectedBrand;
    public CarBrand? SelectedBrand { get => _selectedBrand; set => SetProperty(ref _selectedBrand, value); }

    private CarCategory? _selectedCategory;
    public CarCategory? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }

    private string _model = "";
    public string Model { get => _model; set => SetProperty(ref _model, value); }

    private int? _productionYear;
    public int? ProductionYear { get => _productionYear; set => SetProperty(ref _productionYear, value); }

    private string _vin = "";
    public string Vin { get => _vin; set => SetProperty(ref _vin, value); }

    private string _plateNumber = "";
    public string PlateNumber { get => _plateNumber; set => SetProperty(ref _plateNumber, value); }

    private int _mileage;
    public int Mileage { get => _mileage; set => SetProperty(ref _mileage, value); }

    private string? _color;
    public string? Color { get => _color; set => SetProperty(ref _color, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public bool IsSaved { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? CloseRequested;

    public CarEditViewModel(CarService cars, Car? existing = null)
    {
        _cars = cars;
        _carId = existing?.CarId ?? 0;

        if (existing is not null)
        {
            _model = existing.Model;
            _productionYear = existing.ProductionYear;
            _vin = existing.Vin;
            _plateNumber = existing.PlateNumber;
            _mileage = existing.Mileage;
            _color = existing.Color;
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());

        _ = LoadComboBoxesAsync(existing);
    }

    private async Task LoadComboBoxesAsync(Car? existing)
    {
        IsBusy = true;
        try
        {
            var clients = await _cars.GetClientsAsync();
            Clients.Clear();
            foreach (var c in clients) Clients.Add(c);

            var brands = await _cars.GetBrandsAsync();
            Brands.Clear();
            foreach (var b in brands) Brands.Add(b);

            var cats = await _cars.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);

            if (existing is not null)
            {
                SelectedClient = Clients.FirstOrDefault(c => c.ClientId == existing.ClientId);
                SelectedBrand = Brands.FirstOrDefault(b => b.BrandId == existing.BrandId);
                SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == existing.CategoryId);
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

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (SelectedClient is null) { ErrorMessage = "Выберите клиента"; return; }
        if (SelectedBrand is null) { ErrorMessage = "Выберите марку"; return; }
        if (string.IsNullOrWhiteSpace(Model)) { ErrorMessage = "Укажите модель"; return; }
        if (string.IsNullOrWhiteSpace(Vin)) { ErrorMessage = "Укажите VIN"; return; }
        if (Vin.Length != 17) { ErrorMessage = "VIN должен содержать 17 символов"; return; }
        if (string.IsNullOrWhiteSpace(PlateNumber)) { ErrorMessage = "Укажите госномер"; return; }

        IsBusy = true;
        try
        {
            var entity = new Car
            {
                CarId = _carId,
                ClientId = SelectedClient.ClientId,
                BrandId = SelectedBrand.BrandId,
                CategoryId = SelectedCategory?.CategoryId,
                Model = Model.Trim(),
                ProductionYear = (short?)ProductionYear,
                Vin = Vin.Trim().ToUpperInvariant(),
                PlateNumber = PlateNumber.Trim().ToUpperInvariant(),
                Mileage = Mileage,
                Color = string.IsNullOrWhiteSpace(Color) ? null : Color.Trim()
            };

            if (_carId == 0)
                await _cars.CreateAsync(entity);
            else
                await _cars.UpdateAsync(entity);

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