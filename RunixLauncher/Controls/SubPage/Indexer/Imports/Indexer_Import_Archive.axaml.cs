using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GameLibrary.Logic;

namespace RunixLauncher.Controls.SubPage.Indexer.Imports;

public partial class Indexer_Import_Archive : UserControl, IIndexer_Import
{
    private FileManager.ImportEntry_Archive? archiveData;

    private CancellationTokenSource? extractToken;
    private Popup_AddGames? master;

    public Indexer_Import_Archive()
    {
        InitializeComponent();
    }

    public Indexer_Import_Archive(string file, Popup_AddGames master, Action onRemove)
    {
        this.master = master;
        archiveData = new FileManager.ImportEntry_Archive(file);

        InitializeComponent();

        lbl_Extract.Content = archiveData.archivePath;
        btn_Extract.RegisterClick(Extract, "Cancel");
        btn_Explore.RegisterClick(RequestFolderView, "exploring");

        btn_Remove.RegisterClick(onRemove);

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
        Progress<int> progress = new Progress<int>((int per) =>
        {
            Dispatcher.UIThread.Post(() => inp_ProgressBar.Value = per);
        });

        inp_ProgressBar.IsVisible = true;
        archiveData!.extractedFolder = await FileManager.RequestExtract(archiveData!.archivePath, progress, extractToken.Token);

        RecheckExtract();
    }

    private void RecheckExtract()
    {
        inp_ProgressBar.IsVisible = false;

        btn_Extract.IsVisible = true;
        btn_Explore.IsVisible = false;

        lbl_Folder.Content = string.Empty;

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
        }
    }

    private async Task RequestFolderView()
    {
        master!.OpenFolderView(archiveData!.extractedFolder!, Callback);

        void Callback(string path)
        {
            master!.CloseFolderView();

            if (string.IsNullOrEmpty(path))
                return;

            archiveData.selectedBinary = path;
            RecheckExtract();
        }
    }
}