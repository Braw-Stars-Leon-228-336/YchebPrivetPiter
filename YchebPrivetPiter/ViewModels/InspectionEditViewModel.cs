using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class InspectionEditViewModel : ViewModelBase
{
    private readonly InspectionResultService _inspections;
    private readonly int _workOrderId;
    private readonly int? _mechanicId;
    private int _inspectionId;   // 0 = новый

    public string Title => _workOrderId == 0
        ? "Техосмотр"
        : $"Техосмотр — заказ #{_workOrderId}";

    // ---------- Поля ----------
    private string? _engineCondition;
    public string? EngineCondition { get => _engineCondition; set => SetProperty(ref _engineCondition, value); }

    private string? _brakeCondition;
    public string? BrakeCondition { get => _brakeCondition; set => SetProperty(ref _brakeCondition, value); }

    private string? _suspensionCondition;
    public string? SuspensionCondition { get => _suspensionCondition; set => SetProperty(ref _suspensionCondition, value); }

    private string? _electricalCondition;
    public string? ElectricalCondition { get => _electricalCondition; set => SetProperty(ref _electricalCondition, value); }

    private string? _recommendations;
    public string? Recommendations { get => _recommendations; set => SetProperty(ref _recommendations, value); }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    public bool IsSaved { get; private set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? CloseRequested;

    public InspectionEditViewModel(InspectionResultService inspections,
        int workOrderId, int? mechanicId, InspectionResult? existing = null)
    {
        _inspections = inspections;
        _workOrderId = workOrderId;
        _mechanicId = mechanicId;

        if (existing is not null)
        {
            _inspectionId = existing.InspectionId;
            _engineCondition = existing.EngineCondition;
            _brakeCondition = existing.BrakeCondition;
            _suspensionCondition = existing.SuspensionCondition;
            _electricalCondition = existing.ElectricalCondition;
            _recommendations = existing.Recommendations;
        }

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(EngineCondition) &&
            string.IsNullOrWhiteSpace(BrakeCondition) &&
            string.IsNullOrWhiteSpace(SuspensionCondition) &&
            string.IsNullOrWhiteSpace(ElectricalCondition))
        {
            ErrorMessage = "Заполните хотя бы одну строку осмотра";
            return;
        }

        IsBusy = true;
        try
        {
            var entity = new InspectionResult
            {
                InspectionId = _inspectionId,
                WorkOrderId = _workOrderId,
                MechanicId = _mechanicId,
                EngineCondition = string.IsNullOrWhiteSpace(EngineCondition) ? null : EngineCondition.Trim(),
                BrakeCondition = string.IsNullOrWhiteSpace(BrakeCondition) ? null : BrakeCondition.Trim(),
                SuspensionCondition = string.IsNullOrWhiteSpace(SuspensionCondition) ? null : SuspensionCondition.Trim(),
                ElectricalCondition = string.IsNullOrWhiteSpace(ElectricalCondition) ? null : ElectricalCondition.Trim(),
                Recommendations = string.IsNullOrWhiteSpace(Recommendations) ? null : Recommendations.Trim()
            };

            if (_inspectionId == 0)
                await _inspections.CreateAsync(entity);
            else
                await _inspections.UpdateAsync(entity);

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