using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using DynamicData;
using GameLibrary.Logic;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Objects;
using Runix.Logic.Objects.Runners;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.Modals;

public partial class Modal_Settings_Runner : UserControl
{
    private TaskCompletionSource? modalRes;

    private RunnerDto? selectedRunner;
    private string? selectedRoot;
    private string? selectedUmuRoot;
    private string? selectedVersion;

    private (string, string)[]? winePrefixes;
    private (string, string)? selectedPrefix;

    private UITabGroup tabGroup;

    public Modal_Settings_Runner()
    {
        InitializeComponent();

        btn_Close.RegisterClick(Close);
        btn_Save.RegisterClick(Save, "Saving");

        btn_Dir.RegisterClick(SelectDirectory);

        btn_Wine_Tools.Setup(["CFG", "Wine Tricks", "CMD", "Registry", "Joystick", "Control Panel"], HandleWineToolsCallback);
        btn_Wine_SharedDocuments.Register(ShareDocuments, "Updating");
        btn_WinePrefix_Browse.RegisterClick(BrowsePrefix, "Browsing");
        btn_WinePrefix_Delete.RegisterClick(DeletePrefix, "Deleting");

        btn_Prefix_Installer.RegisterClick(DownloadVersion, "Downloading");
        btn_VersionDirSelector.RegisterClick(SetVersion, "Updating");
        btn_Umu_Location.RegisterClick(UpdateUmuRoot, "Updating");

        btn_Ryujinx_Launch.RegisterClick(LaunchLauncher, "Launching");

        tabGroup = new UITabGroup(TabGroup_Buttons, TabGroup_Content, true);

        inp_CustomEnvironmentVariables.Setup(() =>
        {
            Grid r = new Grid();
            r.Height = 25;
            r.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)];

            TextBox n = new TextBox();
            n.TextAlignment = Avalonia.Media.TextAlignment.Left;

            TextBox d = new TextBox();
            d.TextAlignment = Avalonia.Media.TextAlignment.Left;
            Grid.SetColumn(d, 1);

            r.Children.Add(n);
            r.Children.Add(d);
            return r;
        },
        (Grid c, Data_EnvironmentVar o) =>
        {
            (c.Children[0] as TextBox)!.Text = o.key;
            (c.Children[1] as TextBox)!.Text = o.value;
        },
        () => new Data_EnvironmentVar(), _ => Task.CompletedTask);
    }

    public Task HandleOpen(int runnerId)
    {
        selectedRunner = null;
        selectedUmuRoot = null;

        modalRes = new TaskCompletionSource();

        _ = Draw(runnerId);
        return modalRes.Task;
    }

    private async Task Draw(int runnerId)
    {
        selectedRunner = RunnerManager.GetRunnerProfile(runnerId);

        inp_CustomEnvironmentVariables.Load(selectedRunner?.globalRunnerValues.GetList<Data_EnvironmentVar>(RunnerDto.RunnerConfigValues.Generic_EnvironmentVars) ?? []);

        await UpdateDefaultDetails();
        await UpdateWineDetails();
        await ChangeRunnerType();

        await tabGroup.ChangeSelection(0);
    }

    private async Task UpdateDefaultDetails()
    {
        inp_Name.Text = selectedRunner?.runnerName ?? string.Empty;

        selectedRoot = selectedRunner?.runnerRoot ?? string.Empty;
        btn_Dir.Label = selectedRoot ?? "Select directory";

        selectedVersion = selectedRunner?.runnerVersion ?? string.Empty;
        btn_VersionDirSelector.Label = selectedVersion ?? "SelectVersion";
    }

    private async Task UpdateWineDetails()
    {
        string? sharedDocuments = string.Empty;
        selectedRunner?.globalRunnerValues.TryGetValue(RunnerDto.RunnerConfigValues.Wine_SharedDocuments, out sharedDocuments);

        btn_Wine_SharedDocuments.Label = sharedDocuments ?? "Select folder";
        btn_Wine_SharedDocuments.isSelected = !string.IsNullOrEmpty(sharedDocuments);
    }




    private void Close()
    {
        modalRes?.SetResult();
    }

    private async Task Save()
    {
        if (!ValidateInput())
            return;


        selectedRunner!.runnerName = inp_Name.Text!;
        selectedRunner!.runnerRoot = selectedRoot!;
        selectedRunner!.runnerVersion = selectedVersion ?? string.Empty;

        Data_EnvironmentVar[] envVars = inp_CustomEnvironmentVariables.GetData<Grid>().Select(x => new Data_EnvironmentVar()
        {
            key = (x.Children[0] as TextBox)!.Text!,
            value = (x.Children[1] as TextBox)!.Text!,
        }).ToArray();

        await selectedRunner!.globalRunnerValues.SaveList(RunnerDto.RunnerConfigValues.Generic_EnvironmentVars, envVars);
        await selectedRunner.UpdateDatabaseEntry();

        switch (selectedRunner!.runnerType)
        {
            case RunnerDto.RunnerType.umu_Launcher:
                if (!string.IsNullOrEmpty(selectedUmuRoot))
                {
                    await selectedRunner.globalRunnerValues.SaveValue(RunnerDto.RunnerConfigValues.Umu_Root, selectedUmuRoot);
                }

                break;
        }

        modalRes?.SetResult();
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrEmpty(selectedRoot)) return false;
        if (string.IsNullOrEmpty(inp_Name.Text)) return false;

        return true;
    }

    private async Task SelectDirectory()
    {
        string? folder = await DependencyManager.OpenFolderDialog("Pick folder");

        if (string.IsNullOrEmpty(folder))
            return;

        selectedRoot = folder;
        btn_Dir.Label = selectedRoot;
    }

    private async Task ChangeRunnerType()
    {
        UpdateVersionInstallerButton();

        inp_Version.IsVisible = false;
        btn_Umu_Location.IsVisible = false;
        btn_VersionDirSelector.IsVisible = false;

        if (RunnerDto.IsWineDerivative(selectedRunner!.runnerType) && selectedRunner is IWineRunner wineRunner)
        {
            tabGroup.ToggleGroupVisibility(1, true);

            winePrefixes = await wineRunner.GetPrefixes();
            inp_WinePrefix.Setup(winePrefixes.Select(w => w.Item1), 0, ChangeSelectedPrefix);
            await ChangeSelectedPrefix();

            if (selectedRunner!.runnerType == RunnerDto.RunnerType.umu_Launcher)
            {
                btn_Umu_Location.Label = (selectedRunner as RunnerDto_umu)!.getRuntimeLocationRoot;
                btn_Umu_Location.IsVisible = true;
            }
        }
        else
        {
            tabGroup.ToggleGroupVisibility(1, false);
        }

        string[]? versionOptions = await selectedRunner!.GetRunnerVersion();

        if (versionOptions != null)
        {
            int selectedVersion = versionOptions.IndexOf(selectedRunner?.runnerVersion);

            inp_Version.IsVisible = true;
            inp_Version.Setup(versionOptions, selectedVersion == -1 ? 0 : selectedVersion, ChangeVersionDropdownSelection);
        }
        else
        {
            if (selectedRunner.runnerType == RunnerDto.RunnerType.Ryujinx)
            {
                btn_VersionDirSelector.IsVisible = true;
            }
        }
    }

    private async Task<bool> EnsureExistingProfile()
    {
        if (selectedRunner == null)
        {
            await DependencyManager.OpenYesNoModal("Failed!", "Cannot complete operation on an uncreated profile.\nSave before attempting again.");
            return false;
        }

        return true;
    }

    private async Task HandleWineToolsCallback(int id)
    {
        if (!await EnsureExistingProfile() || selectedPrefix == null)
            return;

        RunnerManager.SpecialLaunchRequest? req = null;
        switch (id)
        {
            case 0: req = RunnerManager.SpecialLaunchRequest.WineConfig; break;
            case 1: req = RunnerManager.SpecialLaunchRequest.WineTricks; break;
            case 2: req = RunnerManager.SpecialLaunchRequest.WineCMD; break;
            case 3: req = RunnerManager.SpecialLaunchRequest.WineRegistry; break;
            case 4: req = RunnerManager.SpecialLaunchRequest.WineJoystick; break;
            case 5: req = RunnerManager.SpecialLaunchRequest.WineControl; break;
        }

        if (req.HasValue)
            await RunnerManager.RunWineTricks(selectedRunner!.runnerId, req.Value, selectedPrefix.Value.Item2);
    }

    private async Task ShareDocuments(bool val)
    {
        if (!await EnsureExistingProfile())
            return;

        string? selectedPath = await DependencyManager.OpenFolderDialog("Select shared folder");

        if (string.IsNullOrEmpty(selectedPath))
            return;

        int result = await DependencyManager.OpenConfirmationAsync(
            "Share prefix documents?",
            "This is a destructive action,\nProceeding will delete the AppData, and Documents folder of the prefix and link to a shared directory.\nthis CAN NOT be undone!",
            ("Link", async () => await selectedRunner!.SharePrefixDocuments(selectedPath), "working")
        );

        if (result != -1)
        {
            await UpdateWineDetails();
        }
    }

    private void ChangeVersionDropdownSelection()
    {
        selectedVersion = inp_Version.selectedValue?.ToString() ?? string.Empty;
        UpdateVersionInstallerButton();
    }

    private async Task SetVersion()
    {
        string? dir = await DependencyManager.OpenFileDialog("Select ryujinx", "AppImage");

        if (string.IsNullOrEmpty(dir) || !File.Exists(dir))
            return;

        selectedVersion = dir;
        btn_VersionDirSelector.Label = dir ?? "Select Version";

        UpdateVersionInstallerButton();
    }

    private void UpdateVersionInstallerButton()
    {
        if (selectedRunner?.IsInstalled(selectedVersion) ?? false)
        {
            btn_Prefix_Installer.IsVisible = false;
        }
        else
        {
            btn_Prefix_Installer.IsVisible = true;
        }
    }

    private async Task DownloadVersion()
    {
        if (!string.IsNullOrEmpty(selectedVersion) && !selectedRunner!.IsInstalled(selectedVersion))
        {
            await DependencyManager.OpenLoadingModal(true,
                selectedRunner.DownloadVersion(selectedVersion!)
            );
        }
    }

    private async Task UpdateUmuRoot()
    {
        if (selectedRunner == null)
            return;

        string? path = await DependencyManager.OpenFolderDialog("Runtime location");

        if (!string.IsNullOrEmpty(path))
        {
            selectedUmuRoot = path;
            btn_Umu_Location.Label = path;
        }
    }

    private async Task ChangeSelectedPrefix()
    {
        int pos = inp_WinePrefix.selectedIndex;

        if (pos < 0 || pos >= (winePrefixes?.Length ?? 0))
        {
            selectedPrefix = null;
            cont_WinePrefixSettings.IsVisible = false;
            return;
        }

        cont_WinePrefixSettings.IsVisible = true;
        selectedPrefix = (winePrefixes![pos].Item1, winePrefixes[pos].Item2);
    }

    private async Task BrowsePrefix()
    {
        if (selectedPrefix == null)
            return;

        DependencyManager.BrowseLocation(selectedPrefix.Value.Item2);
    }

    private async Task DeletePrefix()
    {
        if (selectedPrefix == null)
            return;

        if (!await DependencyManager.OpenYesNoModal("Delete Prefix?", $"Are you sure you want to delete the prefix?\nPath is {selectedPrefix.Value.Item2}"))
            return;

        Directory.Delete(selectedPrefix.Value.Item2, true);
        await ChangeRunnerType();
    }

    private async Task LaunchLauncher() => selectedRunner?.LaunchLauncher();
}