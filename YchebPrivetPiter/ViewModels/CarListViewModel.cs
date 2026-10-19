using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public partial class CarListViewModel : PagedListViewModelBase
{
    public CarService CarService => _cars;

    private Car? _selectedCar;
    public Car? SelectedCar
    {
        get => _selectedCar;
        set => SetProperty(ref _selectedCar, value);
    }

    public Task ReloadAsync() => SafeLoadAsync();

    public async Task<bool> DeleteSelectedAsync()
    {
        if (SelectedCar is null) return false;

        try
        {
            var ok = await _cars.DeleteAsync(SelectedCar.CarId);
            if (ok)
            {
                SelectedCar = null;
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

    private readonly CarService _cars;

    public ObservableCollection<Car> Items { get; } = new();

    private string? _search;
    public string? Search
    {
        get => _search;
        set { if (SetProperty(ref _sortBy, value)) ResetAndReload(); }
    }

    private string _sortBy = "model";
    public string SortBy
    {
        get => _sortBy;
        set { if (SetProperty(ref _sortBy, value)) ResetAndReload(); }
    }

    private bool _sortDesc;
    public bool SortDesc
    {
        get => _sortDesc;
        set { if (SetProperty(ref _sortDesc, value)) ResetAndReload(); }
    }

    public CarListViewModel(CarService cars)
    {
        _cars = cars;
        _ = SafeLoadAsync();
    }

    protected override async Task LoadAsync()
    {
        var result = await _cars.GetPagedAsync(Page, PageSize, Search, sortBy: SortBy, desc: SortDesc);

        Items.Clear();
        foreach (var c in result.Items) Items.Add(c);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }
}