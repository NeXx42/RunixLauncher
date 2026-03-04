using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GameLibrary.Controller;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;
using RunixLauncher.Utils;

namespace RunixLauncher.Controls.Pages.Library;

public partial class Library_Game : UserControl, IControlChild
{
    private static IBrush? noBGBrush
    {
        get
        {
            m_noBGBrush ??= new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            return m_noBGBrush;
        }
    }
    private static IBrush? m_noBGBrush;

    public Action<bool>? pointerStatusChange;

    private CancellationTokenSource? loadToken;

    private ImageBrush? cachedImageBrush;
    private Action? onClick;
    private int? gameId;

    public ImageBrush? getImage => cachedImageBrush;

    public Library_Game()
    {
        InitializeComponent();

        this.PointerPressed += (_, __) => onClick?.Invoke();

        this.PointerEntered += (_, __) => pointerStatusChange?.Invoke(true);
        this.PointerExited += (_, __) => pointerStatusChange?.Invoke(false);
    }

    public void DrawSkeleton()
    {
        onClick = null;
        gameId = null;

        img.Background = noBGBrush;
        title.Text = "Loading";

        lbl_NoIcon.IsVisible = true;
        lbl_NoIcon.Text = "Loading";
        lbl_LastPlayed.Content = "";
        lbl_TimePlayed.Content = "";

        tag_1.IsVisible = true;
        tag_2.IsVisible = true;
        tag_3.IsVisible = true;

        ToggleHover(false);
    }

    public async Task Draw(int gameId, Func<int?, Task> onLaunch)
    {
        if (this.gameId == gameId)
            return;

        DrawSkeleton();

        await (loadToken?.CancelAsync() ?? Task.CompletedTask);
        loadToken = new CancellationTokenSource();

        this.gameId = gameId;
        Game? game = await LibraryManager.GetGame(gameId, loadToken.Token);

        if (game == null)
        {
            this.IsVisible = false;
            return;
        }

        RedrawGameDetails(game);
        await ImageManager.GetGameImage<ImageBrush>(game, RedrawIcon);

        loadToken = null;

        onClick = () => onLaunch?.Invoke(gameId);
    }

    public void RedrawGameDetails(Game game)
    {
        title.Text = game.gameName;
        lbl_NoIcon.Text = game.gameName;
        lbl_LastPlayed.Content = game.GetLastPlayedFormatted();
        lbl_TimePlayed.Content = game.GetTimePlayedFormatted();

        DrawTags(game);
    }

    public void RedrawIcon(int gameId, ImageBrush? bitmapImg)
    {
        if (this.gameId != gameId)
            return;

        cachedImageBrush = bitmapImg;

        img.Background = bitmapImg ?? noBGBrush;
        lbl_NoIcon.IsVisible = bitmapImg == null;
    }

    private void DrawTags(Game game)
    {
        TagDto[] tags = TagManager.GetTagsForAGame(game);

        for (int i = 0; i < 3; i++)
        {
            if (i < tags.Count())
            {
                GetTag(i)?.Draw(tags.ElementAt(i));
            }
            else
            {
                GetTag(i)?.Draw(null);
            }
        }

        Library_TagUnselectable? GetTag(int pos)
        {
            switch (pos)
            {
                case 0: return tag_1;
                case 1: return tag_2;
                case 2: return tag_3;
            }

            return null;
        }
    }

    public void ToggleHover(bool to)
    {
        if (to)
        {
            ctrl.Classes.Add("hovered");
        }
        else
        {
            ctrl.Classes.Remove("hovered");
        }
    }

    public Task Enter() => Task.CompletedTask;
    public Task<bool> Move(int x, int y) => Task.FromResult(false);

    public Task<bool> PressButton(ControllerButton btn)
    {
        if (btn == ControllerButton.A)
        {
            onClick?.Invoke();
        }

        return Task.FromResult(false);
    }
}