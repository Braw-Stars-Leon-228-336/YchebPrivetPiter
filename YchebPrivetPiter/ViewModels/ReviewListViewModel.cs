using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;

namespace YchebPrivetPiter.ViewModels;

public class ReviewListViewModel : PagedListViewModelBase
{
    private readonly ReviewService _reviews;

    public ObservableCollection<Review> Items { get; } = new();

    private int? _minRating;
    public int? MinRating
    {
        get => _minRating;
        set { if (SetProperty(ref _minRating, value)) _ = SafeLoadAsync(); }
    }

    private int? _maxRating;
    public int? MaxRating
    {
        get => _maxRating;
        set { if (SetProperty(ref _maxRating, value)) _ = SafeLoadAsync(); }
    }

    public int[] Ratings { get; } = { 1, 2, 3, 4, 5 };

    public ReviewListViewModel(ReviewService reviews)
    {
        _reviews = reviews;
        _ = SafeLoadAsync();
    }

    protected override async Task LoadAsync()
    {
        var result = await _reviews.GetPagedAsync(Page, PageSize, MinRating, MaxRating);

        Items.Clear();
        foreach (var r in result.Items) Items.Add(r);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }
}