using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    public partial MergeSide MergeTargetSide { get; set; } = MergeSide.Left;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Choose two config files (JSON or XML) and click Compare.";

    [ObservableProperty]
    public partial string SummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasResult { get; set; }

    public ObservableCollection<DiffRowViewModel> DiffRows { get; } = [];

    public IReadOnlyList<ArrayComparisonMode> ArrayModes { get; } = Enum.GetValues<ArrayComparisonMode>();

    public IReadOnlyList<MergeSide> MergeSides { get; } = Enum.GetValues<MergeSide>();

    private ComparisonResult? _lastResult;
    private JsonDocument? _leftDocument;
    private JsonDocument? _rightDocument;
    private ConfigFileFormat _leftFormat;
    private ConfigFileFormat _rightFormat;
    private JsonCompareOptions? _lastOptions;
    private string _leftLabel = "Left";
    private string _rightLabel = "Right";

    public string LeftLabel => _leftLabel;
    public string RightLabel => _rightLabel;

    public string MergeTargetFilePath => MergeTargetSide == MergeSide.Left ? LeftFilePath : RightFilePath;

    public string MergeTargetFileName => Path.GetFileName(MergeTargetFilePath);

    public bool IsMergeTargetLeft
    {
        get => MergeTargetSide == MergeSide.Left;
        set { if (value) MergeTargetSide = MergeSide.Left; }
    }

    public bool IsMergeTargetRight
    {
        get => MergeTargetSide == MergeSide.Right;
        set { if (value) MergeTargetSide = MergeSide.Right; }
    }

    partial void OnMergeTargetSideChanged(MergeSide value)
    {
        OnPropertyChanged(nameof(IsMergeTargetLeft));
        OnPropertyChanged(nameof(IsMergeTargetRight));
        OnPropertyChanged(nameof(MergeTargetFilePath));
        OnPropertyChanged(nameof(MergeTargetFileName));
    }

    [RelayCommand]
    private void Compare()
    {
        DiffRows.Clear();
        HasResult = false;
        _lastResult = null;

        if (string.IsNullOrWhiteSpace(LeftFilePath) || string.IsNullOrWhiteSpace(RightFilePath))
        {
            StatusMessage = "Please select both a left and a right config file.";
            return;
        }

        try
        {
            // Not disposed here: ownership of the underlying JsonDocuments transfers to _leftDocument/_rightDocument
            // below, so Apply Merge can reuse them after the user reviews/adjusts row selections.
            var leftFile = ConfigFileLoader.Load(LeftFilePath);
            var rightFile = ConfigFileLoader.Load(RightFilePath);

            var options = new JsonCompareOptions
            {
                ArrayComparisonMode = SelectedArrayMode,
                CaseSensitivePropertyNames = CaseSensitivePropertyNames,
                TreatNullAsMissing = TreatNullAsMissing,
                NumericTolerance = NumericTolerance,
                IncludeUnchanged = ShowUnchanged
            };

            var comparer = new JsonComparer(options);
            var result = comparer.Compare(leftFile.Document, rightFile.Document);
            _lastResult = result;
            _leftLabel = $"{Path.GetFileName(LeftFilePath)} ({leftFile.Format})";
            _rightLabel = $"{Path.GetFileName(RightFilePath)} ({rightFile.Format})";

            foreach (var entry in result.Entries)
            {
                if (!ShowUnchanged && entry.Type == DiffType.Unchanged)
                {
                    continue;
                }

                DiffRows.Add(new DiffRowViewModel(entry, MergeTargetSide));
            }

            SummaryText = result.AreEqual
                ? "The files are equivalent."
                : $"Added: {result.AddedCount}   Removed: {result.RemovedCount}   Changed: {result.ChangedCount}   Unchanged: {result.UnchangedCount}";

            StatusMessage = $"Compared {_leftLabel} against {_rightLabel}.";
            HasResult = true;

            // Keep the parsed documents (and the options used to pair them) alive so Apply Merge can use the
            // exact same tree the diff grid reflects, even after the user reviews/adjusts row selections.
            _leftDocument?.Dispose();
            _rightDocument?.Dispose();
            _leftDocument = leftFile.Document;
            _rightDocument = rightFile.Document;
            _leftFormat = leftFile.Format;
            _rightFormat = rightFile.Format;
            _lastOptions = options;
        }
        catch (ConfigFileLoadException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public void ApplyMerge()
    {
        if (_leftDocument is null || _rightDocument is null || _lastOptions is null)
        {
            return;
        }

        var targetPath = MergeTargetFilePath;
        var targetFormat = MergeTargetSide == MergeSide.Left ? _leftFormat : _rightFormat;

        var resolutions = DiffRows.ToDictionary(r => r.Path, r => r.SelectedSide);
        var merger = new JsonMerger(_lastOptions);
        var mergedNode = merger.Merge(_leftDocument, _rightDocument, MergeTargetSide, resolutions);

        var backupPath = $"{targetPath}.bak-{DateTime.Now:yyyyMMddHHmmss}";
        File.Copy(targetPath, backupPath, overwrite: false);

        var outputText = targetFormat == ConfigFileFormat.Xml
            ? JsonToXmlConverter.ConvertToXmlString(mergedNode)
            : (mergedNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null");

        File.WriteAllText(targetPath, outputText);

        StatusMessage = $"Applied merge to {Path.GetFileName(targetPath)}. Backup saved as {Path.GetFileName(backupPath)}.";
    }

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
