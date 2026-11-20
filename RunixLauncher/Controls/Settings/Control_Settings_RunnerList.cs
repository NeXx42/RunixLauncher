using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using RunixLauncher.Controls.Modals;

namespace RunixLauncher.Controls.Settings;

public class Control_Settings_RunnerList : Common_ItemList_Generic<Label, RunnerDto>, ISettingControl
{
    public Control_Settings_RunnerList()
    {
        Label = "Runners";
        Register(() => OpenEditMenu(null));
    }

    public Task LoadValue()
    {
        SoftRefresh();
        return Task.CompletedTask;
    }


    private void SoftRefresh()
    {
        DrawListContent(RunnerManager.GetRunnerProfiles(), OpenContextMenu);
    }

    public override void OnDrawListEntry(Label ui, int id, RunnerDto dat)
    {
        ui.Content = dat.isDefault ? $"* {dat.runnerName}" : dat.runnerName;
    }


    private async Task OpenContextMenu(RunnerDto forRunner)
    {
        int? options = await DependencyManager.OpenMultiModal(forRunner.runnerName, true, [
            "Edit",
            "Make Default",
            "Delete"
        ]);


        switch (options)
        {
            case 0:
                await OpenEditMenu(forRunner);
                break;

            case 1:
                await MakeDefaultProfile(forRunner);
                break;

            case 2:
                await DeleteSelectedProfile(forRunner);
                break;
        }
    }

    private async Task OpenEditMenu(RunnerDto? selectedProfile = null)
    {
        if (selectedProfile != null)
        {
            await MainWindow.instance!.DisplayModalAsync<Modal_Settings_Runner>(EditModal);
        }
        else
        {
            (string, RunnerDto.RunnerType?)[] options = [
                ("Cancel", null), ..
                ((RunnerDto.RunnerType[])System.Enum.GetValues(typeof(RunnerDto.RunnerType)))
                    .Where(r => r != RunnerDto.RunnerType.None)
                    .Select(r => (r.ToString(), r))
            ];

            int? desiredType = await DependencyManager.OpenMultiModal("Runner type", true, options.Select(o => o.Item1).ToArray());
            RunnerDto.RunnerType? selectedRunner = desiredType.HasValue ? options[desiredType.Value].Item2 : null;

            if (selectedRunner.HasValue)
            {
                string? path = await DependencyManager.OpenFolderDialog("Runner root");

                if (string.IsNullOrEmpty(path))
                    return;

                await RunnerManager.CreateProfile(path, selectedRunner.Value, string.Empty);
                await LoadValue();
            }
        }


        async Task EditModal(Modal_Settings_Runner modal)
        {
            await modal.HandleOpen(selectedProfile.runnerId);
            SoftRefresh();
        }
    }


    private async Task MakeDefaultProfile(RunnerDto runner)
    {
        await RunnerManager.SetDefaultRunner(runner.runnerId);
        SoftRefresh();
    }

    private async Task DeleteSelectedProfile(RunnerDto runner)
    {
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

}
