using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.Logic;
using RunixLauncher.Controls.SubPage.Indexer;

namespace RunixLauncher.Controls.SubPage;

public partial class Popup_AddGames : UserControl
{
    public Func<Task>? onReimportGames;

    public Popup_AddGames()
    {
        InitializeComponent();

        //_ = cont_ImportView.Setup(this);
        //_ = cont_FolderView.Setup(this);
    }


    public void Setup(Func<Task> onReimport)
    {
        onReimportGames = onReimport;
    }

    public async Task OnOpen()
    {
        this.IsVisible = true;

    }

}