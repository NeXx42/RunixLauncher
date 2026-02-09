using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace RunixLauncher.Controls.SubPage.Indexer.Imports;

public partial class Indexer_Import_File : UserControl, IIndexer_Import
{
    private FileManager.ImportEntry_Binary? bin;

    public Indexer_Import_File()
    {
        InitializeComponent();
    }

    public Indexer_Import_File(string path, Action onRemove)
    {
        bin = new FileManager.ImportEntry_Binary(path);

        InitializeComponent();
        btn_Remove.RegisterClick(onRemove);

        lbl_Path.Content = bin.binaryLocation;
    }

    public Control GetElement() => this;
    public FileManager.IImportEntry? GetImport() => bin;
}