using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GameLibrary.Controller;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;
using RunixLauncher.Controls.Pages.Library;
using RunixLauncher.Helpers;
using RunixLauncher.Utils;

namespace RunixLauncher.Controls.Pages;

public partial class Page_Library : UserControl, IControlChild
{
    private HashSet<TagDto> activeTags = new HashSet<TagDto>();
    private Dictionary<TagDto, Library_Tag> generatedTagUI = new Dictionary<TagDto, Library_Tag>();

    private bool currentSortAscending = true;
    private GameFilterRequest.OrderType currentSort = GameFilterRequest.OrderType.Id;

    private IControlChild? openedMenu;
    private LibraryPageBase? gameList;
    private bool disableBackgroundImages;

    private CancellationTokenSource? subPageCancellationToken;

    private Dictionary<string, Library_ActiveProcess> activeGameUI = new Dictionary<string, Library_ActiveProcess>();

    public Page_Library()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        BindButtons();
        _ = ToggleMenu(false);

        _ = DrawEverything();
        _ = CreateGameList();

        LibraryManager.onGameDetailsUpdate += (int gameId) => _ = RedrawGameFromGameList(gameId);
        LibraryManager.onGameDeletion += () => _ = RedrawGameList();

        RunnerManager.onGameStatusChange += UpdateActiveGameList;
        TagManager.onTagChange += (TagDto? tag, bool didDelete) => _ = HandleTagChange(tag, didDelete);
    }

    private async Task DrawEverything()
    {
        RedrawSortNames();
        await DrawTags();
    }

    private async Task CreateGameList()
    {
        gameList = null;
        disableBackgroundImages = ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Appearance_BackgroundImage, false);

        switch (ConfigHandler.configProvider!.GetInteger(ConfigKeys.Appearance_Layout, 0))
        {
            case 1:
                gameList = new LibraryPage_Endless(this);
                break;

            default:
                gameList = new LibraryPage_Grid(this);
                break;
        }

        cont_GameList.Children.Add(gameList);

        await gameList!.DrawGames();
    }


    private void BindButtons()
    {
        Indexer.Setup(RedrawGameList);

        GameViewer.PointerPressed += (_, e) => e.Handled = true;
        Indexer.PointerPressed += (_, e) => e.Handled = true;
        Settings.PointerPressed += (_, e) => e.Handled = true;
        TagEditor.PointerPressed += (_, e) => e.Handled = true;

        cont_MenuView.PointerPressed += (_, __) => _ = ToggleMenu(false);

        btn_Tags.RegisterClick(OpenTagManager);
        btn_Indexer.RegisterClick(OpenIndexer);
        btn_Settings.RegisterClick(OpenSettings);

        inp_Search.OnChange(RedrawGameList);
        btn_SortDir.RegisterClick(UpdateSortDirection);

        btn_SorType.Setup(GameFilterRequest.getOrderOptionsNames, 0, UpdateSortType);

        (int type, bool asc) = GameFilterRequest.DecodeOrder(ConfigHandler.configProvider!.GetInteger(ConfigKeys.Appearance_DefaultFilter, 0));

        btn_SorType.ChangeValue(type);
        currentSortAscending = asc;

        RedrawSortNames();
    }

    private async Task SwapTagMode(TagDto tagId)
    {
        if (generatedTagUI.TryGetValue(tagId, out Library_Tag? ui))
        {
            if (activeTags.Contains(tagId))
            {
                activeTags.Remove(tagId);
                ui.Toggle(false);
            }
            else
            {
                activeTags.Add(tagId);
                ui.Toggle(true);
            }

            await RedrawGameList(true);
        }
    }

    private async Task RedrawGameList() => await RedrawGameList(false);
    private async Task RedrawGameList(bool resetLayout)
    {
        if (gameList == null)
            return;

        if (resetLayout)
            gameList!.ResetLayout();

        await gameList!.DrawGames();
    }

    private async Task RedrawGameFromGameList(int gameId)
    {
        if (gameList == null)
            return;

        await gameList.RefreshGame(gameId);
    }

    public async Task ToggleGameView(int gameId)
    {
        Game? game = await LibraryManager.GetGame(gameId, CancellationToken.None);

        if (game == null)
        {
            await ToggleMenu(false);
            return;
        }

        await ToggleMenu(true);
        GameViewer.IsVisible = true;

        await Task.WhenAll(
            GameViewer.Draw(game, subPageCancellationToken?.Token ?? CancellationToken.None),
            AnimationHelper.MoveUpAndFade(GameViewer, subPageCancellationToken?.Token ?? CancellationToken.None)
        );

        openedMenu = GameViewer;

    }

    public async Task ToggleMenu(bool to)
    {
        openedMenu = null;

        eff_Blur.Effect = to ? new ImmutableBlurEffect(20) : null;
        cont_MenuView.IsVisible = to;

        GameViewer.IsVisible = false;
        Settings.IsVisible = false;
        Indexer.IsVisible = false;
        TagEditor.IsVisible = false;

        await (subPageCancellationToken?.CancelAsync() ?? Task.CompletedTask);
        subPageCancellationToken = new CancellationTokenSource();
    }

    private async Task OpenIndexer()
    {
        await ToggleMenu(true);
        Indexer.IsVisible = false;

        await Task.WhenAll(
            Indexer.OnOpen(subPageCancellationToken?.Token ?? CancellationToken.None),
            AnimationHelper.MoveUpAndFade(Indexer, subPageCancellationToken?.Token ?? CancellationToken.None)
        );
    }
    private async Task OpenSettings()
    {
        await ToggleMenu(true);
        Settings.IsVisible = true;

        await Task.WhenAll(
            Settings.OnOpen(subPageCancellationToken?.Token ?? CancellationToken.None),
            AnimationHelper.MoveUpAndFade(Indexer, subPageCancellationToken?.Token ?? CancellationToken.None)
        );

        openedMenu = Settings;
    }

    private async Task OpenTagManager()
    {
        await ToggleMenu(true);
        TagEditor.IsVisible = true;

        await Task.WhenAll(
            TagEditor.OnOpen(subPageCancellationToken?.Token ?? CancellationToken.None),
            AnimationHelper.MoveUpAndFade(Indexer, subPageCancellationToken?.Token ?? CancellationToken.None)
        );
    }



    private void UpdateActiveGameList(string path, bool status)
    {
        Dispatcher.UIThread.Post(Internal);

        void Internal()
        {
            Library_ActiveProcess ui;

            if (activeGameUI.TryGetValue(path, out ui!))
            {
                if (!status)
                {
                    cont_ActiveGames.Children.Remove(ui);
                    activeGameUI.Remove(path);
                }

                return;
            }

            ui = new Library_ActiveProcess();
            ui.Draw(path);

            cont_ActiveGames.Children.Add(ui);
            activeGameUI[path] = ui;
        }
    }

    private async Task HandleTagChange(TagDto? tag, bool didDelete)
    {
        if (tag != null && !didDelete)
        {
            generatedTagUI[tag].DrawName(tag);
            return;
        }

        if (tag != null && didDelete)
        {
            generatedTagUI[tag].IsVisible = false; // should be deleting
            generatedTagUI.Remove(tag);

            activeTags.Remove(tag);
        }

        await DrawTags();
    }

    public async Task DrawTags()
    {
        TagDto[] tags = await TagManager.GetAllTags();

        foreach (TagDto newTag in tags)
        {
            if (generatedTagUI.ContainsKey(newTag))
                continue;

            CreateTag(newTag);
        }

        void CreateTag(TagDto tag)
        {
            Library_Tag ui = new Library_Tag();
            ui.Draw(tag, SwapTagMode);

            cont_AllTags.Children.Add(ui);
            generatedTagUI[tag] = ui;
        }
    }


    // sort
    private async Task UpdateSortDirection()
    {
        currentSortAscending = !currentSortAscending;
        RedrawSortNames();

        await RedrawGameList(true);
    }

    private async Task UpdateSortType()
    {
        currentSort = (GameFilterRequest.OrderType)btn_SorType.selectedIndex;
        await RedrawGameList(true);
    }

    private void RedrawSortNames()
    {
        btn_SortDir.Label = currentSortAscending ? "Ascending" : "Descending";
    }

    public GameFilterRequest GetGameFilter(int page, int contentPerPage)
    {
        return new GameFilterRequest()
        {
            nameFilter = inp_Search.getText,
            tagList = activeTags,
            orderDirection = currentSortAscending,
            orderType = currentSort,
            page = page,
            take = contentPerPage
        };
    }

    public void UpdateBackgroundImage(ImageBrush? brush)
    {
        if (disableBackgroundImages || brush == null)
            return;

        img_Bg.Source = (IImage)brush!.Source!;
    }



    // page controls

    public Task Enter()
    {
        return Task.CompletedTask;
    }

    public async Task<bool> Move(int x, int y)
    {
        return await ((openedMenu ?? gameList)?.Move(x, y) ?? Task.FromResult(true));
    }

    public async Task<bool> PressButton(ControllerButton btn)
    {
        if (openedMenu != null)
        {
            bool close = await openedMenu.PressButton(btn);

            if (close)
            {
                await ToggleMenu(false);
            }

            return false;
        }

        if (btn == ControllerButton.Settings)
        {
            await OpenSettings();
            return false;
        }

        return await (gameList?.PressButton(btn) ?? Task.FromResult(true));
    }
}