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
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;

namespace RunixLauncher.Controls.SubPage.Indexer;

public partial class Indexer_FolderView : UserControl
{
    private string? rootFolder;

    private Action? actionCallback;
    private Action? action2Callback;

    private Action<string>? binarySelection;

    public Indexer_FolderView()
    {
        InitializeComponent();

        folderStructure.SelectionMode = SelectionMode.Toggle;
        folderStructure.SelectionChanged += (_, __) => HandleFolderSelection();

        btn_Action.RegisterClick(() => actionCallback?.Invoke());
        btn_Action2.RegisterClick(() => action2Callback?.Invoke());
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


        switch (extension)
        {
            case ".exe":
            case ".appimage":
                btn_Action.IsVisible = true;
                btn_Action2.IsVisible = true;

                btn_Action.Label = "Select";
                btn_Action2.Label = "Launch";

                actionCallback = () => SelectBinary(selectedItem!.fullPath);
                action2Callback = () => _ = LaunchBinary(selectedItem!.fullPath);
                break;

            case ".7z":
            case ".rar":
            case ".iso":
            case ".zip":
            case ".bin":
                btn_Action.IsVisible = true;
                btn_Action.Label = "Extract";

                actionCallback = () => _ = ExtractArchive(selectedItem!.fullPath);
                break;

            default:
                actionCallback = null;
                action2Callback = null;


                btn_Action.IsVisible = false;
                btn_Action2.IsVisible = false;
                break;
        }
    }

    private void SelectBinary(string path)
    {
        this.IsVisible = false;
        this.binarySelection?.Invoke(path);
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
        Progress<double> prog = new Progress<double>();
        await FileManager.RequestExtract(path, prog, CancellationToken.None);

        Explore(rootFolder!, binarySelection);
    }
}