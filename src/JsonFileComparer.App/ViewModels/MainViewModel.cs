using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JsonFileComparer.Core;
using JsonFileComparer.Core.Models;
using JsonFileComparer.Core.Reporting;

namespace JsonFileComparer.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string LeftFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RightFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ArrayComparisonMode SelectedArrayMode { get; set; } = ArrayComparisonMode.Auto;

    [ObservableProperty]
    public partial bool CaseSensitivePropertyNames { get; set; } = true;

    [ObservableProperty]
    public partial bool TreatNullAsMissing { get; set; }

    [ObservableProperty]
    public partial bool ShowUnchanged { get; set; }

    [ObservableProperty]
    public partial double NumericTolerance { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Choose two JSON files and click Compare.";

    [ObservableProperty]
    public partial string SummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasResult { get; set; }

    public ObservableCollection<DiffRowViewModel> DiffRows { get; } = [];

    public IReadOnlyList<ArrayComparisonMode> ArrayModes { get; } = Enum.GetValues<ArrayComparisonMode>();

    private ComparisonResult? _lastResult;
    private string _leftLabel = "Left";
    private string _rightLabel = "Right";

    public string LeftLabel => _leftLabel;
    public string RightLabel => _rightLabel;

    [RelayCommand]
    private void Compare()
    {
        DiffRows.Clear();
        HasResult = false;
        _lastResult = null;

        if (string.IsNullOrWhiteSpace(LeftFilePath) || string.IsNullOrWhiteSpace(RightFilePath))
        {
            StatusMessage = "Please select both a left and a right JSON file.";
            return;
        }

        try
        {
            using var leftDoc = JsonFileLoader.Load(LeftFilePath);
            using var rightDoc = JsonFileLoader.Load(RightFilePath);

            var options = new JsonCompareOptions
            {
                ArrayComparisonMode = SelectedArrayMode,
                CaseSensitivePropertyNames = CaseSensitivePropertyNames,
                TreatNullAsMissing = TreatNullAsMissing,
                NumericTolerance = NumericTolerance,
                IncludeUnchanged = ShowUnchanged
            };

            var comparer = new JsonComparer(options);
            var result = comparer.Compare(leftDoc, rightDoc);
            _lastResult = result;
            _leftLabel = Path.GetFileName(LeftFilePath);
            _rightLabel = Path.GetFileName(RightFilePath);

            foreach (var entry in result.Entries)
            {
                if (!ShowUnchanged && entry.Type == DiffType.Unchanged)
                {
                    continue;
                }

                DiffRows.Add(new DiffRowViewModel(entry));
            }

            SummaryText = result.AreEqual
                ? "The files are equivalent."
                : $"Added: {result.AddedCount}   Removed: {result.RemovedCount}   Changed: {result.ChangedCount}   Unchanged: {result.UnchangedCount}";

            StatusMessage = $"Compared {_leftLabel} against {_rightLabel}.";
            HasResult = true;
        }
        catch (JsonFileLoadException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public bool CanExport => _lastResult is not null;

    public void ExportJson(string path)
    {
        if (_lastResult is null) return;
        JsonReportWriter.WriteToFile(_lastResult, path);
        StatusMessage = $"JSON report saved to {path}";
    }

    public void ExportHtml(string path)
    {
        if (_lastResult is null) return;
        HtmlReportWriter.WriteToFile(_lastResult, _leftLabel, _rightLabel, path);
        StatusMessage = $"HTML report saved to {path}";
    }
}
