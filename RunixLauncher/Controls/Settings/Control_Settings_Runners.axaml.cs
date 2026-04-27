using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GameLibrary.Logic;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using RunixLauncher.Controls.Modals;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.Settings;

public partial class Control_Settings_Runners : UserControl, ISettingControl
{
    private SettingBase? setting;
    private RunnerUI[]? profiles;

    private int? selectedProfile
    {
        set
        {
            if (m_selectedProfile.HasValue)
            {
                profiles![m_selectedProfile.Value].Toggle(false);
                btn_Default.IsVisible = false;
            }

            m_selectedProfile = value;
            btn_Add.Label = value.HasValue ? "Edit" : "Add";

            if (m_selectedProfile.HasValue)
            {
                profiles![m_selectedProfile.Value].Toggle(true);
                btn_Default.IsVisible = true;
            }
        }
        get => m_selectedProfile;
    }
    private int? m_selectedProfile;

    public Control_Settings_Runners()
    {
        InitializeComponent();

        selectedProfile = null;
        btn_Add.RegisterClick(OpenEditMenu);
        btn_Remove.RegisterClick(DeleteSelectedProfile, "Deleting");

        btn_Default.RegisterClick(MakeDefaultProfile);

        btn_Default.IsVisible = false;
    }

    public ISettingControl Draw(SettingBase setting, SettingsUI_Runners ui)
    {
        this.setting = setting;
        return this;
    }

    public async Task LoadValue()
    {
        selectedProfile = null;

        RunnerDto[] runners = await setting!.LoadSetting<RunnerDto[]>([]);
        profiles = new RunnerUI[runners.Length];

        container.Children.Clear();

        for (int i = 0; i < runners.Length; i++)
            profiles[i] = new RunnerUI(i, runners[i], container, this);
    }

    private void SoftRefresh()
    {
        foreach (RunnerUI ui in profiles!)
            ui.RedrawName();
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
            await MainWindow.instance!.DisplayModalAsync<Modal_Settings_Runner>(EditModal);
        }
        else
        {
            int? desiredType = await DependencyManager.OpenMultiModal("Runner type", System.Enum.GetNames(typeof(RunnerDto.RunnerType)));

            if (desiredType.HasValue)
            {
                string? path = await DependencyManager.OpenFolderDialog("Runner root");

                if (string.IsNullOrEmpty(path))
                    return;

                await RunnerManager.CreateProfile(path, (RunnerDto.RunnerType)desiredType.Value, string.Empty);
                await LoadValue();
            }
        }


        async Task EditModal(Modal_Settings_Runner modal)
        {
            await modal.HandleOpen(profiles![selectedProfile.Value].runner.runnerId);
            SoftRefresh();
        }
    }

    private async Task MakeDefaultProfile()
    {
        if (!selectedProfile.HasValue)
            return;

        await RunnerManager.SetDefaultRunner(profiles![selectedProfile.Value].runner.runnerId);
        SoftRefresh();
    }

    private async Task DeleteSelectedProfile()
    {
        if (!selectedProfile.HasValue)
            return;

        RunnerDto runner = profiles![selectedProfile.Value].runner;
        int gamesAffected = await RunnerManager.GetGameCountForRunner(runner.runnerId);

        await DependencyManager.OpenConfirmationAsync($"Delete the runner {runner.runnerName}?",
            $"{gamesAffected} games use the profile.\nWill delete the following directory\n\n{runner.runnerRoot}?",
            ("Delete", HandleDeletion, "Deleting"));

        async Task HandleDeletion()
        {
            if (await RunnerManager.DeleteRunnerProfile(runner.runnerId))
                await LoadValue(); // causes duplicate recache on the runner profiles, but whatever. should be fast (makes sense the deletion logic actually does the recache not the settings ui)
        }
    }

    struct RunnerUI
    {
        public RunnerDto runner;

        public Border ui;
        public Label label;


        public RunnerUI(int id, RunnerDto runner, StackPanel container, Control_Settings_Runners master)
        {
            this.runner = runner;

            ui = new Border();

            ui.CornerRadius = new CornerRadius(2);
            ui.Height = 30;
            ui.HorizontalAlignment = HorizontalAlignment.Stretch;

            label = new Label();

            label.VerticalAlignment = VerticalAlignment.Center;
            label.Margin = new Thickness(5);

            ui.Child = label;
            ui.PointerPressed += (_, __) => master.SelectProfile(id);

            container.Children.Add(ui);

            RedrawName();
            Toggle(false);
        }

        public void RedrawName()
        {
            label.Content = $"{(runner.isDefault ? "* " : "")} {runner.runnerName}";
        }

        public void Toggle(bool to)
        {
            ui.Background = to ? CommonColours.SettingsList_selectedBrush : CommonColours.SettingsList_unselectedBrush;
        }
    }

}