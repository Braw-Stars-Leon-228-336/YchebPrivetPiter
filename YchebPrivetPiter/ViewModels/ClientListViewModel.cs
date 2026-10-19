using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public partial class ClientListViewModel : PagedListViewModelBase
{
    private Client? _selectedClient;
    public Client? SelectedClient
    {
        get => _selectedClient;
        set => SetProperty(ref _selectedClient, value);
    }

    public ClientService ClientService => _clients;

    public Task ReloadAsync() => SafeLoadAsync();

    private readonly ClientService _clients;

    public ObservableCollection<Client> Items { get; } = new();

    private string? _search;
    public string? Search
    {
        get => _search;
        set { if (SetProperty(ref _search, value)) DebouncedReload(); }
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

    public ClientListViewModel(ClientService clients)
    {
        _clients = clients;
        _ = SafeLoadAsync();
    }

    protected override async Task LoadAsync()
    {
        var result = await _clients.GetPagedAsync(Page, PageSize, Search, SortBy, SortDesc);

        Items.Clear();
        foreach (var c in result.Items) Items.Add(c);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    public async Task<bool> DeleteSelectedAsync()
    {
        if (SelectedClient is null) return false;

        try
        {
            var ok = await _clients.DeleteAsync(SelectedClient.ClientId);
            if (ok)
            {
                SelectedClient = null;
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