using Avalonia;
using Avalonia.Controls;
using GameLibrary.Logic;

namespace RunixLauncher.Controls.SubPage.Indexer.Imports;

public interface IIndexer_Import
{
    public Control GetElement();
    public FileManager.IImportEntry? GetImport();
}
