using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace RunixLauncher.Controls.SubPage.Indexer.Imports;

public partial class Indexer_Import_Folder : UserControl, IIndexer_Import
{
    private FileManager.ImportEntry_Folder? bin;

    public Indexer_Import_Folder()
    {
        InitializeComponent();
    }

    public Indexer_Import_Folder(string path)
    {
        bin = new FileManager.ImportEntry_Folder(path);

        InitializeComponent();
        lbl_Path.Content = bin.folderPath;

        inp_Binaries.Setup(bin.binaries, 0, UpdateSelectedBin);
    }

    public Control GetElement() => this;

    public FileManager.IImportEntry? GetImport()
    {
        if (string.IsNullOrEmpty(bin?.getBinaryPath))
            return null;

        return bin;
    }

    private void UpdateSelectedBin() => bin!.selectedBinary = inp_Binaries.selectedIndex;
}