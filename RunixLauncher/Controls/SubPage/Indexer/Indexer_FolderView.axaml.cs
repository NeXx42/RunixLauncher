using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace RunixLauncher.Controls.SubPage.Indexer;

public partial class Indexer_FolderView : UserControl
{
    enum SelectedFileType
    {
        None,
        Executable,
        Archive
    }

    private string? rootFolder;
    private (string file, SelectedFileType type)? selectedFile;

    private CancellationTokenSource? extractionCancellation;
    private Action<string>? binarySelection;

    public Indexer_FolderView()
    {
        InitializeComponent();

        folderStructure.SelectionMode = SelectionMode.Toggle;
        folderStructure.SelectionChanged += (_, __) => HandleFolderSelection();

        btn_Action.RegisterClick(ActionCallback1, "Working");
        btn_Action2.RegisterClick(ActionCallback2, "Working");
        btn_Progress_Cancel.RegisterClick(CancelExtraction, "Cancelling");
        btn_Close.RegisterClick(() => binarySelection?.Invoke(string.Empty));

        cont_Progress.IsVisible = false;
    }

    public void Explore(string root, Action<string>? binarySelection)
    {
        this.rootFolder = root;
        this.binarySelection = binarySelection;

        DataContext = new Indexer_FolderViewModel(root);
    }

    private void HandleFolderSelection()
    {
        Indexer_FolderViewModel_Node selectedItem = (Indexer_FolderViewModel_Node)folderStructure.SelectedItem!;
        string extension = string.Empty;

        if (selectedItem != null)
        {
            extension = Path.GetExtension(selectedItem.fullPath);
        }

        btn_Action.IsVisible = false;
        btn_Action2.IsVisible = false;

        selectedFile = null;

        switch (extension.ToLower())
        {
            case ".exe":
            case ".appimage":
                btn_Action.IsVisible = true;
                btn_Action2.IsVisible = true;

                btn_Action.Label = "Select";
                btn_Action2.Label = "Launch";

                selectedFile = (selectedItem!.fullPath, SelectedFileType.Executable);
                break;

            case ".7z":
            case ".rar":
            case ".iso":
            case ".zip":
            case ".bin":
                btn_Action.IsVisible = true;
                btn_Action.Label = "Extract";

                selectedFile = (selectedItem!.fullPath, SelectedFileType.Archive);
                break;
        }
    }

    private async Task ActionCallback1()
    {
        switch (selectedFile?.type ?? SelectedFileType.None)
        {
            case SelectedFileType.Executable:
                await SelectBinary(selectedFile!.Value.file);
                break;

            case SelectedFileType.Archive:
                await ExtractArchive(selectedFile!.Value.file);
                break;
        }
    }

    private async Task ActionCallback2()
    {
        switch (selectedFile?.type ?? SelectedFileType.None)
        {
            case SelectedFileType.Executable:
                await LaunchBinary(selectedFile!.Value.file);
                break;

        }
    }

    private Task SelectBinary(string path)
    {
        this.binarySelection?.Invoke(path);
        return Task.CompletedTask;
    }

    private async Task LaunchBinary(string path)
    {
        RunnerDto[] runners = RunnerManager.GetRunnerProfiles();
        int? selected = await DependencyManager.OpenMultiModal("Runner", runners.Select(x => x.runnerName).ToArray());

        if (selected == null)
            return;


        await RunnerManager.RunGame(new RunnerManager.LaunchRequest()
        {
            identifier = path,
            path = path,
            runnerId = runners[selected.Value].runnerId,
            extraWhitelist = [rootFolder!]
        });
    }

    private async Task ExtractArchive(string path)
    {
        if (extractionCancellation != null)
            await extractionCancellation.CancelAsync();

        cont_Progress.IsVisible = true;

        Progress<int> prog = new Progress<int>((int amount) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                inp_ProgressBar.Value = amount;
            });
        });

        extractionCancellation = new CancellationTokenSource();
        await FileManager.RequestExtract(path, prog, extractionCancellation!.Token);

        Explore(rootFolder!, binarySelection);
        cont_Progress.IsVisible = false;
    }

    private async Task CancelExtraction()
    {
        await (extractionCancellation?.CancelAsync() ?? Task.CompletedTask);
        extractionCancellation = null;
    }
}