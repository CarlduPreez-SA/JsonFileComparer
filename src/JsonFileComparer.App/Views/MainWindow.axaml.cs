using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using JsonFileComparer.App.ViewModels;

namespace JsonFileComparer.App.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType ConfigFileType = new("Config files (JSON/XML)")
    {
        Patterns = ["*.json", "*.xml", "*.config"]
    };

    private readonly LineHighlightRenderer _leftHighlighter = new();
    private readonly LineHighlightRenderer _rightHighlighter = new();

    public MainWindow()
    {
        InitializeComponent();
        LeftEditor.TextArea.TextView.BackgroundRenderers.Add(_leftHighlighter);
        RightEditor.TextArea.TextView.BackgroundRenderers.Add(_rightHighlighter);
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    private void OnCompareClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.CompareCommand.Execute(null);
        RefreshTextPanes();
    }

    private void RefreshTextPanes()
    {
        LeftEditor.Text = ViewModel.GetLeftRawText();
        RightEditor.Text = ViewModel.GetRightRawText();
        ApplyLineHighlights(ViewModel.ComputeLineDiff());
    }

    private void ApplyLineHighlights(Core.TextDiff.LineDiffResult diff)
    {
        _leftHighlighter.SetHighlights(diff.LeftLines);
        _rightHighlighter.SetHighlights(diff.RightLines);
        LeftEditor.TextArea.TextView.InvalidateVisual();
        RightEditor.TextArea.TextView.InvalidateVisual();
    }

    private void OnRefreshTextDiffClick(object? sender, RoutedEventArgs e)
    {
        var diff = ViewModel.RefreshLineDiff(LeftEditor.Text, RightEditor.Text);
        ApplyLineHighlights(diff);
    }

    private async void OnSaveLeftTextClick(object? sender, RoutedEventArgs e)
    {
        var confirmed = await ConfirmDialog.ShowAsync(
            this,
            $"This will overwrite \"{System.IO.Path.GetFileName(ViewModel.LeftFilePath)}\" with the text shown in this pane. " +
            "A timestamped backup will be saved alongside it first. Continue?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            ViewModel.SaveLeftText(LeftEditor.Text);
            ViewModel.CompareCommand.Execute(null);
            RefreshTextPanes();
        }
        catch (Exception ex)
        {
            await ConfirmDialog.ShowAsync(this, $"Save failed: {ex.Message}", confirmText: "OK");
        }
    }

    private async void OnSaveRightTextClick(object? sender, RoutedEventArgs e)
    {
        var confirmed = await ConfirmDialog.ShowAsync(
            this,
            $"This will overwrite \"{System.IO.Path.GetFileName(ViewModel.RightFilePath)}\" with the text shown in this pane. " +
            "A timestamped backup will be saved alongside it first. Continue?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            ViewModel.SaveRightText(RightEditor.Text);
            ViewModel.CompareCommand.Execute(null);
            RefreshTextPanes();
        }
        catch (Exception ex)
        {
            await ConfirmDialog.ShowAsync(this, $"Save failed: {ex.Message}", confirmText: "OK");
        }
    }

    private async void OnBrowseLeftClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickOpenFileAsync("Select left config file");
        if (path is not null)
        {
            ViewModel.LeftFilePath = path;
        }
    }

    private async void OnBrowseRightClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickOpenFileAsync("Select right config file");
        if (path is not null)
        {
            ViewModel.RightFilePath = path;
        }
    }

    private async void OnApplyMergeClick(object? sender, RoutedEventArgs e)
    {
        var targetFile = ViewModel.MergeTargetFileName;
        var confirmed = await ConfirmDialog.ShowAsync(
            this,
            $"This will overwrite \"{targetFile}\" with your selected values. " +
            "A timestamped backup of the current file will be saved alongside it first. Continue?");

        if (!confirmed)
        {
            return;
        }

        try
        {
            ViewModel.ApplyMerge();
        }
        catch (Exception ex)
        {
            await ConfirmDialog.ShowAsync(this, $"Merge failed: {ex.Message}", confirmText: "OK");
        }
    }

    private async void OnExportJsonClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickSaveFileAsync("Export JSON report", "diff-report.json",
            new FilePickerFileType("JSON report") { Patterns = ["*.json"] });
        if (path is not null)
        {
            ViewModel.ExportJson(path);
        }
    }

    private async void OnExportHtmlClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickSaveFileAsync("Export HTML report", "diff-report.html",
            new FilePickerFileType("HTML report") { Patterns = ["*.html"] });
        if (path is not null)
        {
            ViewModel.ExportHtml(path);
        }
    }

    private async System.Threading.Tasks.Task<string?> PickOpenFileAsync(string title)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { ConfigFileType, FilePickerFileTypes.All }
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async System.Threading.Tasks.Task<string?> PickSaveFileAsync(string title, string suggestedName, FilePickerFileType fileType)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            FileTypeChoices = new List<FilePickerFileType> { fileType }
        });

        return file?.TryGetLocalPath();
    }
}
