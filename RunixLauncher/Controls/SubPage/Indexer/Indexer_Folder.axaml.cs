using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.AvaloniaUI.Controls.SubPage.Indexer;

public partial class Indexer_Folder : UserControl
{
    private FileManager.ImportEntry_Folder? folder;
    private Action<FileManager.ImportEntry_Folder, Indexer_Folder>? requestFolderView;

    public Indexer_Folder()
    {
        InitializeComponent();

        btn_Extract.RegisterClick(ExtractArchive);
        btn_Explore.RegisterClick(ExploreExtracted);
    }

    public void Draw(FileManager.ImportEntry_Folder folder, Action<FileManager.ImportEntry_Folder, Indexer_Folder> requestFolderView)
    {
        this.requestFolderView = requestFolderView;
        this.folder = folder;

        RedrawUI();
    }

    public void RedrawUI()
    {
        lbl_Extract.Content = folder?.archiveFile;
        lbl_Folder.Content = folder?.selectedBinary ?? folder?.extractedEntry;

        btn_Extract.IsVisible = string.IsNullOrEmpty(folder?.extractedEntry) && !string.IsNullOrEmpty(folder?.archiveFile);
        btn_Explore.IsVisible = !string.IsNullOrEmpty(folder?.extractedEntry);
    }

    private async Task ExtractArchive()
    {
        if (folder == null)
            return;

        string? result = await FileManager.RequestExtract(folder.archiveFile ?? string.Empty, new Progress<double>(), new CancellationTokenSource().Token);

        if (string.IsNullOrEmpty(result))
            return;

        folder.extractedEntry = result;
        folder.CrawlForExecutable(result);

        RedrawUI();
    }

    private async Task ExploreExtracted()
    {
        requestFolderView?.Invoke(folder!, this);
    }
}