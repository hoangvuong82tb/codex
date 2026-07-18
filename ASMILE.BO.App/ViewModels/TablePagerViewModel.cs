using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class TablePagerViewModel<T> : ObservableObject
{
    private readonly List<T> _source = new();

    public ObservableCollection<T> Items { get; } = new();
    public ObservableCollection<string> PageSizeOptions { get; } = new() { "15 / trang", "20 / trang", "50 / trang", "100 / trang" };
    public ObservableCollection<TablePageButton> PageButtons { get; } = new();

    [ObservableProperty] private string selectedPageSize = "15 / trang";
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private string itemName = "bản ghi";

    public int TotalCount => _source.Count;
    public int PageSize => ParsePageSize(SelectedPageSize);
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));

    public string PageText
    {
        get
        {
            if (TotalCount <= 0)
            {
                return $"Hiển thị 0 của 0 {ItemName}";
            }

            var start = ((CurrentPage - 1) * PageSize) + 1;
            var end = Math.Min(CurrentPage * PageSize, TotalCount);
            return $"Hiển thị {start} - {end} của {TotalCount} {ItemName}";
        }
    }

    public IReadOnlyList<T> CurrentItems => Items;

    public void SetSource(IEnumerable<T> rows, bool resetPage = true)
    {
        _source.Clear();
        _source.AddRange(rows);
        if (resetPage)
        {
            CurrentPage = 1;
        }

        RefreshPage();
    }

    partial void OnSelectedPageSizeChanged(string value)
    {
        CurrentPage = 1;
        RefreshPage();
    }

    partial void OnCurrentPageChanged(int value)
    {
        RefreshPage();
    }

    partial void OnItemNameChanged(string value)
    {
        OnPropertyChanged(nameof(PageText));
    }

    [RelayCommand]
    private void GoToPage(TablePageButton? page)
    {
        if (page?.PageNumber is null)
        {
            return;
        }

        CurrentPage = Math.Clamp(page.PageNumber.Value, 1, TotalPages);
    }

    [RelayCommand]
    private void PreviousPage()
    {
        CurrentPage = Math.Max(1, CurrentPage - 1);
    }

    [RelayCommand]
    private void NextPage()
    {
        CurrentPage = Math.Min(TotalPages, CurrentPage + 1);
    }

    private void RefreshPage()
    {
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);
        Items.Clear();
        foreach (var row in _source.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
        {
            Items.Add(row);
        }

        RebuildButtons();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(PageSize));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageText));
    }

    private void RebuildButtons()
    {
        PageButtons.Clear();
        foreach (var page in BuildPages(CurrentPage, TotalPages))
        {
            PageButtons.Add(page is null
                ? new TablePageButton("...", null, false, true)
                : new TablePageButton(page.Value.ToString(CultureInfo.InvariantCulture), page.Value, page.Value == CurrentPage, false));
        }
    }

    private static IEnumerable<int?> BuildPages(int current, int total)
    {
        if (total <= 5)
        {
            for (var i = 1; i <= total; i++)
            {
                yield return i;
            }

            yield break;
        }

        if (current <= 3)
        {
            for (var i = 1; i <= Math.Min(5, total); i++)
            {
                yield return i;
            }

            yield return null;
            yield return total;
            yield break;
        }

        if (current >= total - 2)
        {
            yield return 1;
            yield return null;
            for (var i = Math.Max(2, total - 4); i <= total; i++)
            {
                yield return i;
            }

            yield break;
        }

        yield return 1;
        yield return null;
        yield return current - 1;
        yield return current;
        yield return current + 1;
        yield return null;
        yield return total;
    }

    private static int ParsePageSize(string? value)
    {
        var first = value?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : 15;
    }
}

public sealed record TablePageButton(string Label, int? PageNumber, bool IsCurrent, bool IsEllipsis);
