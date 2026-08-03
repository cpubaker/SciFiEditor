using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SciFiEditor.Core.Search;
using SciFiEditor.Domain;

namespace SciFiEditor.App.ViewModels;

public sealed partial class SearchViewModel : ObservableObject
{
    private readonly SearchService _searchService;
    private readonly Action<Guid> _onNavigate;

    public SearchViewModel(SearchService searchService, Action<Guid> onNavigate)
    {
        _searchService = searchService;
        _onNavigate = onNavigate;
    }

    public ObservableCollection<SearchResult> Results { get; } = new();

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private SearchResult? _selectedResult;

    [RelayCommand]
    private void Search()
    {
        Results.Clear();
        foreach (var result in _searchService.Search(Query))
        {
            Results.Add(result);
        }
    }

    [RelayCommand]
    private void OpenResult(SearchResult? result)
    {
        if (result is not null)
        {
            _onNavigate(result.NodeId);
        }
    }
}
