using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.Modals;

public partial class Modal_Library : UserControl
{
    private LibraryDto? selectedLibrary;
    private TaskCompletionSource? modalRes;

    private UITabGroup tabGroup;
    private List<GameGroup> linkedGames = new List<GameGroup>();


    public Modal_Library()
    {
        InitializeComponent();

        btn_Close.RegisterClick(() => modalRes?.SetResult());
        btn_Move.RegisterClick(() => StartTransfer(), "Transferring");

        tabGroup = new UITabGroup(TabGroup_Buttons, TabGroup_Content, true);
    }

    public Task HandleOpen(LibraryDto? lib)
    {
        selectedLibrary = lib;
        modalRes = new TaskCompletionSource();

        _ = Draw();
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        return modalRes.Task;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
    }

    private async Task Draw()
    {
        await tabGroup.ChangeSelection(0);

        inp_Name.Text = selectedLibrary?.alias;
        btn_Dir.Label = selectedLibrary?.root ?? "Set Root";

        await DrawGameList();
    }

    private async Task DrawGameList()
    {
        linkedGames = new List<GameGroup>();
        cont_Games.Children.Clear();

        if (selectedLibrary != null)
        {
            Game[] games = await LibraryManager.GetGamesPerLibrary(selectedLibrary!.libraryId, CancellationToken.None);

            foreach (Game game in games)
            {
                GameGroup ui = new GameGroup(game);

                linkedGames.Add(ui);
                cont_Games.Children.Add(ui.ui);
            }
        }
    }

    private async Task StartTransfer()
    {
        Game[] toMove = linkedGames.Where(x => x.IsSelected()).Select(x => x.game).ToArray();

        LibraryDto[] possibleLibraries = LibraryManager.GetLibraries().Where(x => x.libraryId != selectedLibrary?.libraryId).ToArray();
        int? selected = await DependencyManager.OpenMultiModal("Target Library", [.. possibleLibraries.Select(x => x.getFullName), "Cancel"]);

        if (!selected.HasValue || selected.Value >= possibleLibraries.Length)
            return;

        string gameNames = string.Join("\n", toMove.Select(x => x.gameName));
        LibraryDto targetLibrary = possibleLibraries[selected.Value];

        if (!await DependencyManager.OpenYesNoModal("Transfer Games?", $"Transfer the following games to\n{targetLibrary.getFullName}\n\n{gameNames}"))
            return;

        Func<Task>[] moveEvents = toMove.Select(x => (Func<Task>)(() => x.ChangeLibrary(targetLibrary))).ToArray();
        await DependencyManager.OpenLoadingModal(true, moveEvents);
    }

    private class GameGroup
    {
        public readonly Game game;
        public readonly Grid ui;

        private readonly CheckBox cb;

        public GameGroup(Game game)
        {
            this.game = game;

            ui = new Grid()
            {
                ColumnDefinitions = new ColumnDefinitions()
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(5, GridUnitType.Pixel),
                    new ColumnDefinition(GridLength.Star)
                }
            };

            cb = new CheckBox();

            Label l = new Label();
            l.Content = game.gameName;
            Grid.SetColumn(l, 2);

            ui.Children.Add(cb);
            ui.Children.Add(l);
        }

        public bool IsSelected() => cb.IsChecked ?? false;
    }
}