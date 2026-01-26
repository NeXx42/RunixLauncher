using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace RunixLauncher.Controls.SubPage.Indexer.Imports;

public partial class Indexer_Import_Archive : UserControl, IIndexer_Import
{
    private FileManager.ImportEntry_Archive? archiveData;

    private CancellationTokenSource? extractToken;
    private Func<Indexer_FolderView>? folderView;

    public Indexer_Import_Archive()
    {
        InitializeComponent();
    }

    public Indexer_Import_Archive(string file, Func<Indexer_FolderView> folderView)
    {
        this.folderView = folderView;
        archiveData = new FileManager.ImportEntry_Archive(file);

        InitializeComponent();

        lbl_Extract.Content = archiveData.archivePath;
        btn_Extract.RegisterClick(Extract, "Cancel");
        btn_Explore.RegisterClick(RequestFolderView, "exploring");

        RecheckExtract();
    }

    public Control GetElement() => this;

    public FileManager.IImportEntry? GetImport()
    {
        if (string.IsNullOrEmpty(archiveData?.selectedBinary))
            return null;

        return archiveData;
    }

    private async Task Extract()
    {
        if (extractToken != null)
        {
            await extractToken.CancelAsync();
            return;
        }

        extractToken = new CancellationTokenSource();

        Progress<double> progress = new Progress<double>();
        archiveData!.extractedFolder = await FileManager.RequestExtract(archiveData!.archivePath, progress, extractToken.Token);

        RecheckExtract();
    }

    private void RecheckExtract()
    {
        btn_Extract.IsVisible = true;
        btn_Explore.IsVisible = false;

        if (File.Exists(archiveData!.selectedBinary))
        {
            btn_Extract.IsVisible = false;
            btn_Explore.IsVisible = true;

            lbl_Folder.Content = archiveData!.selectedBinary;

            return;
        }

        if (Directory.Exists(archiveData!.extractedFolder))
        {
            btn_Extract.IsVisible = false;
            btn_Explore.IsVisible = true;

            lbl_Folder.Content = archiveData!.extractedFolder;
        }
    }

    private async Task RequestFolderView()
    {
        var viewer = folderView!.Invoke();
        viewer.Explore(archiveData!.extractedFolder!, Callback);

        void Callback(string path)
        {
            archiveData.selectedBinary = path;
            RecheckExtract();
        }
    }
}