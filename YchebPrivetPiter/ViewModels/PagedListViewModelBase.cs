using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace YchebPrivetPiter.ViewModels;

public abstract partial class PagedListViewModelBase : ViewModelBase
{
    // ---------- Paginator state ----------
    private int _page = 1;
    public int Page
    {
        get => _page;
        set
        {
            if (SetProperty(ref _page, value))
            {
                OnPropertyChanged(nameof(PageInfo));
                OnPropertyChanged(nameof(CanGoFirst));
                OnPropertyChanged(nameof(CanGoPrev));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoLast));
                _ = SafeLoadAsync();
            }
        }
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                _page = 1;
                OnPropertyChanged(nameof(Page));
                OnPropertyChanged(nameof(PageInfo));
                _ = SafeLoadAsync();
            }
        }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        protected set
        {
            if (SetProperty(ref _totalCount, value))
                OnPropertyChanged(nameof(PageInfo));
        }
    }

    private int _totalPages;
    public int TotalPages
    {
        get => _totalPages;
        protected set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(PageInfo));
                OnPropertyChanged(nameof(CanGoFirst));
                OnPropertyChanged(nameof(CanGoPrev));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoLast));
            }
        }
    }

    public int[] PageSizes { get; } = { 10, 20, 50, 100 };

    public bool CanGoFirst => Page > 1;
    public bool CanGoPrev => Page > 1;
    public bool CanGoNext => Page < TotalPages;
    public bool CanGoLast => Page < TotalPages;
    public string PageInfo => $"Стр. {Page} из {Math.Max(1, TotalPages)} · {TotalCount} записей";

    // ---------- Busy flag ----------
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    // ---------- Abstract load ----------
    protected abstract Task LoadAsync();

    protected async Task SafeLoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await LoadAsync(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    [RelayCommand] private async Task FirstPageAsync() { if (CanGoFirst) { Page = 1; await Task.CompletedTask; } }
    [RelayCommand] private async Task PrevPageAsync() { if (CanGoPrev) { Page--; await Task.CompletedTask; } }
    [RelayCommand] private async Task NextPageAsync() { if (CanGoNext) { Page++; await Task.CompletedTask; } }
    [RelayCommand] private async Task LastPageAsync() { if (CanGoLast) { Page = TotalPages; await Task.CompletedTask; } }
    [RelayCommand] private Task RefreshAsync() => SafeLoadAsync();

    private System.Threading.CancellationTokenSource? _searchCts;

    protected void DebouncedReload()
    {
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var token = _searchCts.Token;
        _ = Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { Page = 1; });
        }, token);
    }
    protected void ResetAndReload()
    {
        if (Page != 1)
            Page = 1;           
        else
            _ = SafeLoadAsync();
    }
}