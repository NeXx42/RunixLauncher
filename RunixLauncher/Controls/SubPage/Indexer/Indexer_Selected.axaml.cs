using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using RunixLauncher.Controls.SubPage.Indexer.Imports;

namespace RunixLauncher.Controls.SubPage.Indexer;

public partial class Indexer_Selected : UserControl
{
    private LibraryDto[] libs;
    private Dictionary<string, IIndexer_Import> imports = new Dictionary<string, IIndexer_Import>();

    public Indexer_Selected()
    {
        InitializeComponent();
        FileBrowser.IsVisible = false;

        btn_Add.RegisterClick(AddGames);
        btn_Import.RegisterClick(HandleImport);

        libs = LibraryManager.GetLibraries();
        inp_Library.Setup((string[])["None", .. libs.Select(x => x.root)], 0, null);
    }

    private async Task AddGames()
    {
        string[] options = ["Archive", "Folder", "File", "EXE Installer"];
        int? option = await DependencyManager.OpenMultiModal("Import", options);

        switch (option)
        {
            case 0: await HandleArchive(); break;
            case 1: await HandleFolder(); break;
            case 2: await HandleFile(); break;
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

    private async Task HandleArchive()
    {
        string? file = await DependencyManager.OpenFileDialog("Select Archive", "bin", "zip", "7z", "rar", "iso");

        if (IsSelectionInvalid(file, out string path))
            return;

        AddNewImport(path, new Indexer_Import_Archive(path, RequestFolderView));
    }

    private async Task HandleFolder()
    {
        string? file = await DependencyManager.OpenFolderDialog("Select Folder");

        if (IsSelectionInvalid(file, out string path))
            return;
    }

    private async Task HandleFile()
    {
        string? file = await DependencyManager.OpenFileDialog("Select File", RunnerManager.GetAcceptableTypes());

        if (IsSelectionInvalid(file, out string path))
            return;

        AddNewImport(path, new Indexer_Import_File(path));
    }

    private async Task HandleInstaller()
    {
        string? file = await DependencyManager.OpenFileDialog("Select File", "exe");

        if (IsSelectionInvalid(file, out string path))
            return;
    }

    private void AddNewImport(string path, IIndexer_Import entry)
    {
        cont_FoundGames.Children.Add(entry.GetElement());
        imports.Add(path, entry);
    }

    public Indexer_FolderView RequestFolderView()
    {
        FileBrowser.IsVisible = true;
        return FileBrowser;
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
}