using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Objects.Tags;
using RunixLauncher.Controls.Pages.Library;

namespace RunixLauncher.Controls.SubPages.Popup_GameView_Tabs;

public partial class Popup_GameView_Tab_Tags : Popup_GameView_TabBase
{
    public Popup_GameView_Tab_Tags()
    {
        InitializeComponent();
    }

    public override Tab CreateGroup(Common_ButtonToggle btn, Panel parent)
    {
        parent.Children.Add(this);
        return new Tab_Tags(this, btn);
    }

    internal class Tab_Tags : Tab
    {
        private new Popup_GameView_Tab_Tags element;
        private Dictionary<TagDto, Library_Tag> allTags = new Dictionary<TagDto, Library_Tag>();

        public Tab_Tags(Popup_GameView_Tab_Tags element, Common_ButtonToggle btn) : base(element, btn)
        {
            this.element = element;
        }

        protected override void InternalSetup(TabGroup master) { }

        protected override async Task OpenWithGame(Game? game, bool isNewGame)
        {
            await CheckForNewTags();
            await RedrawSelectedTags(game!);
        }

        public async Task CheckForNewTags()
        {
            TagDto[] newTags = await TagManager.GetAllTags();

            if (allTags.Count == newTags.Length)
                return;

            allTags.Clear();
            element.cont_AllTags.Children.Clear();

            foreach (TagDto tag in newTags)
            {
                GenerateTag(tag);
            }

            void GenerateTag(TagDto tag)
            {
                Library_Tag tagUI = new Library_Tag();

                tagUI.Draw(tag, HandleTagToggle);

                element.cont_AllTags.Children.Add(tagUI);
                allTags.Add(tag, tagUI);
            }
        }

        private async Task HandleTagToggle(TagDto tag)
        {
            if (tag is TagDto_Managed)
                return;

            await inspectingGame!.ToggleTag(tag.id);
            await RedrawSelectedTags(inspectingGame);
        }

        private async Task RedrawSelectedTags(Game game)
        {
            foreach (KeyValuePair<TagDto, Library_Tag> tag in allTags)
                tag.Value.Toggle(tag.Key.DoesFitGame(game));
        }
    }
}