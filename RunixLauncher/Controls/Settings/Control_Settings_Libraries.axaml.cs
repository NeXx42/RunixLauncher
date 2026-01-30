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

    private Border[]? profileUIS;
    private LibraryDto[]? existingProfiles;

    private ImmutableSolidColorBrush selectedBrush;
    private ImmutableSolidColorBrush unselectedBrush;

    private int? selectedProfile
    {
        set
        {
            if (m_selectedProfile.HasValue)
            {
                profileUIS![m_selectedProfile.Value].Background = unselectedBrush;
            }

            m_selectedProfile = value;
            btn_Add.Label = value.HasValue ? "Edit" : "Add";

            if (m_selectedProfile.HasValue)
            {
                profileUIS![m_selectedProfile.Value].Background = selectedBrush;
            }
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

        existingProfiles = await setting!.LoadSetting<LibraryDto[]>(null!);
        profileUIS = new Border[existingProfiles?.Length ?? 0];

        container.Children.Clear();

        if (existingProfiles != null)
        {
            for (int i = 0; i < existingProfiles.Length; i++)
            {
                DrawProfile(i);
            }
        }
    }

    private void DrawProfile(int uiIndex)
    {
        Border grid = new Border();

        grid.CornerRadius = new CornerRadius(2);
        grid.Height = 30;
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.Background = unselectedBrush;

        Label l = new Label();
        l.Content = existingProfiles![uiIndex].root;
        l.VerticalAlignment = VerticalAlignment.Center;
        l.Margin = new Thickness(5);

        grid.Child = l;
        grid.PointerPressed += (_, __) => SelectProfile(uiIndex);

        container.Children.Add(grid);
        profileUIS![uiIndex] = grid;
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
            return;

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

    }
}