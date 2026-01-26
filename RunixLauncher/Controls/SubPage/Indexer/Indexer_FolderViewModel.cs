using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace RunixLauncher.Controls.SubPage.Indexer;

public class Indexer_FolderViewModel
{
    public ObservableCollection<Indexer_FolderViewModel_Node> Nodes { get; }

    public Indexer_FolderViewModel(string root)
    {
        Nodes = new()
        {
            new Indexer_FolderViewModel_Node(root, SearchChildren(root))
        };

        IEnumerable<Indexer_FolderViewModel_Node> SearchChildren(string root)
        {
            string[] dirs = Directory.GetDirectories(root);
            string[] files = Directory.GetFiles(root);

            List<Indexer_FolderViewModel_Node> nodes = dirs.Select(x => new Indexer_FolderViewModel_Node(x, SearchChildren(x))).ToList();
            nodes.AddRange(files.Select(x => new Indexer_FolderViewModel_Node(x)));

            return nodes;
        }
    }
}

public class Indexer_FolderViewModel_Node
{
    public string Title { get; }
    public ObservableCollection<Indexer_FolderViewModel_Node> SubNodes { get; } = new();

    public readonly string fullPath;

    public Indexer_FolderViewModel_Node(string title)
    {
        fullPath = title;
        Title = Path.GetFileName(title);
    }

    public Indexer_FolderViewModel_Node(string title, IEnumerable<Indexer_FolderViewModel_Node> subNodes)
    {
        fullPath = title;
        Title = Path.GetFileName(title);

        SubNodes = new ObservableCollection<Indexer_FolderViewModel_Node>(subNodes);
    }
}