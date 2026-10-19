using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public partial class PartListViewModel : PagedListViewModelBase
{
    private readonly PartService _parts;

    public ObservableCollection<Part> Items { get; } = new();

    private string? _search;
    public string? Search
    {
        get => _search;
        set { if (SetProperty(ref _search, value)) DebouncedReload(); }
    }

    private bool _onlyLowStock;
    public bool OnlyLowStock
    {
        get => _onlyLowStock;
        set { if (SetProperty(ref _onlyLowStock, value)) ResetAndReload(); }
    }

    private string _sortBy = "name";
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

    public PartListViewModel(PartService parts)
    {
        _parts = parts;
        _ = SafeLoadAsync();
    }

    protected override async Task LoadAsync()
    {
        var result = await _parts.GetPagedAsync(
            Page, PageSize, Search, onlyLowStock: OnlyLowStock,
            sortBy: SortBy, desc: SortDesc);

        Items.Clear();
        foreach (var p in result.Items) Items.Add(p);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }
}