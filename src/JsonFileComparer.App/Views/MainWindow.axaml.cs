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

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

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
