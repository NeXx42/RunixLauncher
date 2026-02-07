using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Settings;
using Runix.Logic.Settings.UI;
using RunixLauncher.Controls.Modals;

namespace RunixLauncher.Controls.Settings;

public partial class Control_Settings_Libraries : UserControl, ISettingControl
{
    private SettingBase? setting;

    private LibraryUI[]? elements;

    private static ImmutableSolidColorBrush? selectedBrush;
    private static ImmutableSolidColorBrush? unselectedBrush;

    private int? selectedProfile
    {
        set
        {
            if (m_selectedProfile.HasValue)
                elements![m_selectedProfile.Value].ToggleSelection(false);

            m_selectedProfile = value;
            btn_Add.Label = value.HasValue ? "Edit" : "Add";

            if (m_selectedProfile.HasValue)
                elements![m_selectedProfile.Value].ToggleSelection(true);
        }
        get => m_selectedProfile;
    }
    private int? m_selectedProfile;

    public Control_Settings_Libraries()
    {
        InitializeComponent();

        selectedBrush = new ImmutableSolidColorBrush(Color.FromRgb(0, 0, 0));
        unselectedBrush = new ImmutableSolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        selectedProfile = null;
        btn_Add.RegisterClick(OpenEditMenu);
        btn_Remove.RegisterClick(DeleteSelectedProfile, "Deleting");
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Libraries ui)
    {
        this.setting = setting;
        return this;
    }

    public async Task LoadValue()
    {
        selectedProfile = null;

        LibraryDto[] libs = await setting!.LoadSetting<LibraryDto[]>([]);
        elements = new LibraryUI[libs.Length];

        container.Children.Clear();

        for (int i = 0; i < elements.Length; i++)
            elements[i] = new LibraryUI(i, libs[i], container, this);
    }

    private void SelectProfile(int profileId)
    {
        if (selectedProfile == profileId)
        {
            selectedProfile = null;
            return;
        }

        selectedProfile = profileId;
    }

    private async Task OpenEditMenu()
    {
        if (selectedProfile.HasValue)
        {
            string? alias = await DependencyManager.OpenStringInputModal("New Alias");

            if (string.IsNullOrEmpty(alias))
                return;

            await elements![selectedProfile.Value].RenameAlias(alias);
            return;
        }

        if (await DependencyManager.OpenConfirmationAsync("Select Root", "Select a folder that will be the basis of the library.", ("Select Folder", HandleModal, "Selecting")) == -1)
            return;

        async Task HandleModal()
        {
            string? path = await DependencyManager.OpenFolderDialog("Root");

            if (string.IsNullOrEmpty(path))
                return;

            await LibraryManager.GenerateLibrary(path);
            await LoadValue();
        }
    }

    private async Task DeleteSelectedProfile()
    {
        if (!selectedProfile.HasValue)
            return;


    }


    private struct LibraryUI
    {
        public readonly LibraryDto lib;

        public readonly Border border;
        public readonly Label label;

        public LibraryUI(int index, LibraryDto lib, StackPanel container, Control_Settings_Libraries master)
        {
            this.lib = lib;

            border = new Border();

            border.CornerRadius = new CornerRadius(2);
            border.Height = 30;
            border.HorizontalAlignment = HorizontalAlignment.Stretch;
            border.Background = unselectedBrush;

            label = new Label();
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Margin = new Thickness(5);

            border.Child = label;
            border.PointerPressed += (_, __) => master.SelectProfile(index);

            container.Children.Add(border);
            Redraw();
        }

        public void Redraw()
        {
            label.Content = $"[{lib.alias}] - {lib.root}";
        }

        public void ToggleSelection(bool to)
        {
            border.Background = to ? Control_Settings_Libraries.selectedBrush : Control_Settings_Libraries.unselectedBrush;
        }

        public async Task RenameAlias(string to)
        {
            await lib.UpdateAlias(to);
            Redraw();
        }
    }
}