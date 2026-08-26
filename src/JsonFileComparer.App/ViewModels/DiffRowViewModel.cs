using CommunityToolkit.Mvvm.ComponentModel;
using JsonFileComparer.Core;
using JsonFileComparer.Core.Models;

namespace JsonFileComparer.App.ViewModels;

public sealed partial class DiffRowViewModel : ObservableObject
{
    public DiffRowViewModel(DiffEntry entry, MergeSide defaultSide)
    {
        Path = entry.Path;
        Type = entry.Type.ToString();
        LeftValue = entry.LeftValue ?? "(missing)";
        RightValue = entry.RightValue ?? "(missing)";
        DiffType = entry.Type;
        SelectedSide = defaultSide;
    }

    public string Path { get; }

    public string Type { get; }

    public string LeftValue { get; }

    public string RightValue { get; }

    public DiffType DiffType { get; }

    [ObservableProperty]
    public partial MergeSide SelectedSide { get; set; }

    public bool IsLeftSelected
    {
        get => SelectedSide == MergeSide.Left;
        set
        {
            if (value) SelectedSide = MergeSide.Left;
        }
    }

    public bool IsRightSelected
    {
        get => SelectedSide == MergeSide.Right;
        set
        {
            if (value) SelectedSide = MergeSide.Right;
        }
    }

    partial void OnSelectedSideChanged(MergeSide value)
    {
        OnPropertyChanged(nameof(IsLeftSelected));
        OnPropertyChanged(nameof(IsRightSelected));
    }
}
