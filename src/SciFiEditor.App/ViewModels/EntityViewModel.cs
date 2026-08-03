using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SciFiEditor.Core.Entities;
using SciFiEditor.Domain;

namespace SciFiEditor.App.ViewModels;

public sealed partial class EntityViewModel : ObservableObject
{
    private readonly EntityService _entityService;

    public EntityViewModel(EntityService entityService)
    {
        _entityService = entityService;
        LoadEntities();
    }

    public ObservableCollection<StoryEntity> Characters { get; } = new();
    public ObservableCollection<StoryEntity> Locations { get; } = new();

    public bool EntitiesChanged { get; private set; }

    [ObservableProperty]
    private string _newCharacterName = string.Empty;

    [ObservableProperty]
    private string _newLocationName = string.Empty;

    [ObservableProperty]
    private StoryEntity? _selectedCharacter;

    [ObservableProperty]
    private StoryEntity? _selectedLocation;

    [RelayCommand]
    private void AddCharacter()
    {
        if (string.IsNullOrWhiteSpace(NewCharacterName))
        {
            return;
        }

        _entityService.AddEntity(EntityType.Character, NewCharacterName.Trim(), string.Empty);
        NewCharacterName = string.Empty;
        EntitiesChanged = true;
        LoadEntities();
    }

    [RelayCommand]
    private void AddLocation()
    {
        if (string.IsNullOrWhiteSpace(NewLocationName))
        {
            return;
        }

        _entityService.AddEntity(EntityType.Location, NewLocationName.Trim(), string.Empty);
        NewLocationName = string.Empty;
        EntitiesChanged = true;
        LoadEntities();
    }

    [RelayCommand]
    private void DeleteCharacter()
    {
        if (SelectedCharacter is null)
        {
            return;
        }

        _entityService.DeleteEntity(SelectedCharacter.Id);
        EntitiesChanged = true;
        LoadEntities();
    }

    [RelayCommand]
    private void DeleteLocation()
    {
        if (SelectedLocation is null)
        {
            return;
        }

        _entityService.DeleteEntity(SelectedLocation.Id);
        EntitiesChanged = true;
        LoadEntities();
    }

    private void LoadEntities()
    {
        Characters.Clear();
        foreach (var entity in _entityService.GetAll(EntityType.Character))
        {
            Characters.Add(entity);
        }

        Locations.Clear();
        foreach (var entity in _entityService.GetAll(EntityType.Location))
        {
            Locations.Add(entity);
        }
    }
}
