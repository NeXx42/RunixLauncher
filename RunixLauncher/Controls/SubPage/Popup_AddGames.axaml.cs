using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using RunixLauncher.Controls.SubPage.Indexer;
using RunixLauncher.Controls.SubPage.Indexer.Imports;

namespace RunixLauncher.Controls.SubPage;

public partial class Popup_AddGames : UserControl
{
    public Func<Task>? onReimportGames;

    private LibraryDto[] libs;
    private Dictionary<string, IIndexer_Import> imports = new Dictionary<string, IIndexer_Import>();

    public Popup_AddGames()
    {
        InitializeComponent();
        cont_FileBrowser.IsVisible = false;

        btn_Add.RegisterClick(AddGames);
        btn_Import.RegisterClick(HandleImport);

        libs = LibraryManager.GetLibraries();
        inp_Library.Setup((string[])["None", .. libs.Select(x => x.getFullName)], 0, null);
    }


    public void Setup(Func<Task> onReimport)
    {
        onReimportGames = onReimport;
    }

    public async Task OnOpen(CancellationToken cancellationToken)
    {
        this.IsVisible = true;

    }

    private async Task AddGames()
    {
        string[] options = ["Archive", "Folder", "File", "EXE Installer"];
        int? option = await DependencyManager.OpenMultiModal("Import", options);

        switch (option)
        {
            case 0: await HandleArchives(); break;
            case 1: await HandleFolder(); break;
            case 2: await HandleFiles(); break;
            case 3: await HandleInstaller(); break;
        }
    }

    private bool IsSelectionInvalid(string? inp, out string realPath)
    {
        realPath = string.Empty;

        if (string.IsNullOrEmpty(inp))
            return true;

        if (imports.ContainsKey(inp))
            return true;

        realPath = inp.Replace("%20", " ");
        return false;
    }

    private async Task HandleArchives()
    {
        string[]? files = await DependencyManager.OpenFilesDialog("Select Archive(s)", "bin", "zip", "7z", "rar", "iso");

        if (files == null)
            return;

        foreach (string file in files)
        {
            if (IsSelectionInvalid(file, out string path))
                return;

            AddNewImport(path, new Indexer_Import_Archive(path, this, () => RemoveEntry(path)));
        }
    }

    private async Task HandleFolder()
    {
        string? file = await DependencyManager.OpenFolderDialog("Select Folder");

        if (IsSelectionInvalid(file, out string path))
            return;

        AddNewImport(path, new Indexer_Import_Folder(path, () => RemoveEntry(path)));
    }

    private async Task HandleFiles()
    {
        string[]? files = await DependencyManager.OpenFilesDialog("Select File(s)");

        if (files == null)
            return;

        foreach (string file in files)
        {
            if (IsSelectionInvalid(file, out string path))
                return;

            AddNewImport(path, new Indexer_Import_File(path, () => RemoveEntry(path)));
        }
    }

    private async Task HandleInstaller()
    {
        string? file = await DependencyManager.OpenFileDialog("Select File", "exe");

        if (IsSelectionInvalid(file, out string path))
            return;

        await LaunchBinary(path);
    }

    private void AddNewImport(string path, IIndexer_Import entry)
    {
        cont_FoundGames.Children.Add(entry.GetElement());
        imports.Add(path, entry);
    }

    public void OpenFolderView(string root, Action<string> binarySelection)
    {
        cont_FileBrowser.IsVisible = true;
        FileBrowser.Explore(root, binarySelection);
    }

    public void CloseFolderView()
    {
        cont_FileBrowser.IsVisible = false;
    }


    private async Task HandleImport()
    {
        LibraryDto? selectedLibrary = inp_Library.selectedIndex == 0 ? null : libs[inp_Library.selectedIndex - 1];
        string paragraph = selectedLibrary == null ? "Import without moving" : $"Import and move files into the following directory? \n\n{selectedLibrary!.root}";

        if (!await DependencyManager.OpenYesNoModalAsync("Import?", paragraph, Import, "Importing"))
            return;

        async Task Import()
        {
            Dictionary<string, FileManager.IImportEntry?> toImport = imports.ToDictionary(x => x.Key, x => x.Value.GetImport());
            List<string> successful = await LibraryManager.ImportGames(toImport, selectedLibrary?.libraryId);

            foreach (string success in successful)
            {
                if (imports.TryGetValue(success, out IIndexer_Import? ui))
                {
                    cont_FoundGames.Children.Remove(ui.GetElement());
                    imports.Remove(success);
                }
            }
        }
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
        });
    }

    private void RemoveEntry(string path)
    {
        if (!imports.TryGetValue(path, out IIndexer_Import? indexer))
            return;

        cont_FoundGames.Children.Remove(indexer.GetElement());
        imports.Remove(path);
    }
}