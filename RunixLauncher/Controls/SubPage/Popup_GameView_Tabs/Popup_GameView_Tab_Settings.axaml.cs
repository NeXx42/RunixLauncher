using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using Runix.Logic.Helpers;
using Runix.Structure.Enums;
using RunixLauncher.Controls.Modals;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.SubPages.Popup_GameView_Tabs;

public partial class Popup_GameView_Tab_Settings : Popup_GameView_TabBase
{
    private UITabGroup tabGroup;
    private Tab? thisTab;

    public Popup_GameView_Tab_Settings()
    {
        InitializeComponent();
        DataContext = this;

        tabGroup = new UITabGroup(
            new UITabGroup_GroupToggleButton(tab_General, tabBtn_General),
            new UITabGroup_GroupToggleButton(tab_LaunchSettings, tabBtn_Launching),
            new UITabGroup_GroupToggleButton(tab_RunnerSettings, tabBtn_Runner)
        );

        _ = tabGroup.ChangeSelection(0);

        inp_Wine_DLLOverride_custom.Setup(() =>
        {
            Grid r = new Grid();
            r.Height = 25;
            r.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)];

            TextBox n = new TextBox();
            n.KeyUp += (_, __) => _ = inp_Wine_DLLOverride_custom.RequestUpdate();
            n.TextAlignment = TextAlignment.Left;

            Common_Dropdown d = new Common_Dropdown();
            d.Setup(Enum.GetValues<DLLOverrideBehaviour>(), (int)DLLOverrideBehaviour.Default, inp_Wine_DLLOverride_custom.RequestUpdate);
            Grid.SetColumn(d, 1);

            r.Children.Add(n);
            r.Children.Add(d);
            return r;
        },
        (Grid c, Data_DLLOverride o) =>
        {
            (c.Children[0] as TextBox)!.Text = o.dllName;
            (c.Children[1] as Common_Dropdown)!.SilentlyChangeValue((int)o.behaviour);
        },
        () =>
        {
            return new Data_DLLOverride()
            {
                behaviour = DLLOverrideBehaviour.Default
            };
        },
        async (List<Grid> els) =>
        {
            Data_DLLOverride[] dat = els.Select(c => new Data_DLLOverride()
            {
                dllName = (c.Children[0] as TextBox)!.Text!,
                behaviour = (DLLOverrideBehaviour)(c.Children[1] as Common_Dropdown)!.selectedIndex
            }).ToArray();

            await thisTab!.inspectingGame.config.SaveList(Game_Config.Launcher_dllOverride_Custom, dat);
        });

        inp_Wine_CustomEnvironmentVariables.Setup(() =>
        {
            Grid r = new Grid();
            r.Height = 25;
            r.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)];

            TextBox n = new TextBox();
            n.KeyUp += (_, __) => _ = inp_Wine_CustomEnvironmentVariables.RequestUpdate();
            n.TextAlignment = TextAlignment.Left;

            TextBox d = new TextBox();
            d.KeyUp += (_, __) => _ = inp_Wine_CustomEnvironmentVariables.RequestUpdate();
            d.TextAlignment = TextAlignment.Left;
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
        () => new Data_EnvironmentVar(),
        async (List<Grid> els) =>
        {
            Data_EnvironmentVar[] dat = els.Select(c => new Data_EnvironmentVar()
            {
                key = (c.Children[0] as TextBox)!.Text!,
                value = (c.Children[1] as TextBox)!.Text!,
            }).ToArray();

            await thisTab!.inspectingGame.config.SaveList(Game_Config.Launcher_Wine_CustomEnvironmentVariables, dat);
        });
    }

    public override Tab CreateGroup(Common_ButtonToggle btn, Panel parent)
    {
        parent.Children.Add(this);
        thisTab = new Tab_LaunchSettings(this, btn);

        return thisTab;
    }

    public class Tab_LaunchSettings : Tab
    {
        private new Popup_GameView_Tab_Settings element;

        private LibraryDto[]? possibleLibraries;
        private RunnerDto[]? possibleRunners;

        private ConfigChangerBase[] configOptions;

        public Tab_LaunchSettings(Popup_GameView_Tab_Settings element, Common_ButtonToggle btn) : base(element, btn)
        {
            configOptions = [];
            this.element = element;

            this.element.btn_CleanProfile.RegisterClick(TryToCleanProfile);
        }

        protected override void InternalSetup(TabGroup master)
        {
            RunnerDto.RunnerType[] wineRunnerTypes = [
                RunnerDto.RunnerType.Wine,
                RunnerDto.RunnerType.Wine_GE,
                RunnerDto.RunnerType.Proton_GE,
                RunnerDto.RunnerType.umu_Launcher
            ];

            configOptions = [
                new ConfigChanger_InputField(element.inp_Library_SteamId, Game_Config.Library_SteamId, () => inspectingGame),
                
                // running settings

                new ConfigChanger_Toggle(element.inp_Emulate, Game_Config.General_LocaleEmulation, () => inspectingGame),
                new ConfigChanger_Dropdown(element.inp_CaptureLogs, Game_Config.General_LoggingLevel, Enum.GetNames<LoggingLevel>(), (int)LoggingLevel.Off, () => inspectingGame),
                new ConfigChanger_InputField(element.inp_Arguments, Game_Config.General_Arguments, () => inspectingGame),

                new ConfigChanger_Toggle(element.inp_Wine_Windowed, Game_Config.Wine_Windowed, () => inspectingGame, wineRunnerTypes),
                new ConfigChanger_Toggle(element.inp_IsolatePrefix, Game_Config.Wine_IsolatedPrefix, () => inspectingGame, wineRunnerTypes),
                new ConfigChanger_Toggle(element.inp_Wine_VirtualDesktop, Game_Config.Wine_ExplorerLaunch, () => inspectingGame, wineRunnerTypes),

                new ConfigChanger_Toggle(element.inp_Wine_LaunchAsConsole, Game_Config.Wine_ConsoleLaunched, () => inspectingGame, wineRunnerTypes),
                new ConfigChanger_Toggle(element.inp_SteamBridge, Game_Config.Launcher_UseSteamBridge, () => inspectingGame),

                new ConfigChanger_InputField(element.inp_umu_Id, Game_Config.Launcher_umu_Id, () => inspectingGame, RunnerDto.RunnerType.umu_Launcher),

                new ConfigChanger_Dropdown(element.inp_Wine_DLLOverride_steamapi64, Game_Config.Launcher_dllOverride_steamapi64, Enum.GetNames<DLLOverrideBehaviour>(), (int)DLLOverrideBehaviour.Default, () => inspectingGame, wineRunnerTypes),

                new ConfigChanger_Dropdown(element.inp_ControllerType, Game_Config.ControllerType, Enum.GetNames<ControllerType>(), (int)ControllerType.Disabled, () => inspectingGame, wineRunnerTypes),
            ];

            element.btn_RefreshSteamMetaData.RegisterClick(TryToRefreshSteamMetaData);
            element.btn_EditWineProfile.RegisterClick(TryToEditSelectedProfile);
        }

        protected override async Task OpenWithGame(Game? game, bool isNewGame)
        {
            if (game is Game_Steam)
            {
                element.IsVisible = false;
                return;
            }

            DrawLibraries(game!);
            DrawRunners(game!);
            DrawBinaries(game!);
            await UpdateSupportedSettings();

            foreach (ConfigChangerBase config in configOptions)
                await config.Load(game!);

            element.inp_Wine_DLLOverride_custom.Load(inspectingGame.config.GetList<Data_DLLOverride>(Game_Config.Launcher_dllOverride_Custom));
            element.inp_Wine_CustomEnvironmentVariables.Load(inspectingGame.config.GetList<Data_EnvironmentVar>(Game_Config.Launcher_Wine_CustomEnvironmentVariables));
        }

        private void DrawLibraries(Game game)
        {
            possibleLibraries = LibraryManager.GetLibraries();

            element.inp_Library_Library.Setup((string[])["None", .. possibleLibraries.Select(x => x.getName)], 0, OnUpdate);
            CorrectSelectedLib();

            async Task OnUpdate()
            {
                LibraryDto? desiredLib = element.inp_Library_Library.selectedIndex == 0 ? null : possibleLibraries[element.inp_Library_Library.selectedIndex - 1];
                string msg = desiredLib == null ? "Are you sure you want to unlink this game from a library?"
                                                : $"Are you sure you want to move this game to the following library?\n{desiredLib.root}";

                await DependencyManager.OpenYesNoModalAsync("Change Library", msg, () => game.ChangeLibrary(desiredLib), "Moving");
                CorrectSelectedLib();
            }

            void CorrectSelectedLib()
            {
                int currentLib = game.libraryId.HasValue ? possibleLibraries.Select(x => x.libraryId).ToList().IndexOf(game.libraryId.Value) + 1 : 0;
                element.inp_Library_Library.SilentlyChangeValue(currentLib);
            }
        }

        private void DrawRunners(Game game)
        {
            possibleRunners = RunnerManager.GetRunnerProfiles();
            string firstProfile = possibleRunners.FirstOrDefault(x => x.isDefault)?.runnerName ?? "NO DEFAULT";

            string[] profileOptions = [$"Default ({firstProfile})", .. possibleRunners!.Select(x => x.runnerName)!.ToArray()];
            int selectedProfile = possibleRunners.Select(x => x.runnerId).ToList().IndexOf(game.runnerId ?? -1);

            element.inp_WineProfile.IsVisible = true;
            element.inp_WineProfile.Setup(profileOptions, selectedProfile >= 0 ? (selectedProfile + 1) : 0, HandleWineProfileChange);

            async Task HandleWineProfileChange()
            {
                int? newProfileId = null;
                int selectedIndex = element.inp_WineProfile.selectedIndex;

                if (selectedIndex != 0) // default profile
                {
                    newProfileId = possibleRunners![selectedIndex - 1].runnerId;
                }

                await inspectingGame!.ChangeRunnerId(newProfileId);
                await UpdateSupportedSettings();
            }
        }

        private void DrawBinaries(Game game)
        {
            (int? currentExecutable, string[] possibleBinaries)? options = game.GetPossibleBinaries();

            if (options != null)
            {
                (element.Parent as Control)!.IsVisible = true;
                element.inp_binary.Setup(options.Value.possibleBinaries.Select(x => Path.GetFileName(x)), options.Value.currentExecutable, HandleBinaryChange);
            }
            else
            {
                (element.inp_binary.Parent as Control)!.IsVisible = false;
            }

            async Task HandleBinaryChange() => await inspectingGame!.ChangeBinaryLocation(element.inp_binary.selectedValue?.ToString());
        }

        private async Task UpdateSupportedSettings()
        {
            ((Visual)element.inp_binary.Parent!).IsVisible = inspectingGame.runnerType == RunnerDto.RunnerType.Proton_GE
                                            || inspectingGame.runnerType == RunnerDto.RunnerType.Wine
                                            || inspectingGame.runnerType == RunnerDto.RunnerType.Wine_GE
                                            || inspectingGame.runnerType == RunnerDto.RunnerType.umu_Launcher;

            foreach (ConfigChangerBase config in configOptions)
                config.HandleSupportedType(inspectingGame.runnerType ?? RunnerDto.RunnerType.None);
        }

        private async Task TryToCleanProfile()
        {
            if (inspectingGame?.runnerId == null)
                return;

            RunnerDto? runner = RunnerManager.GetRunnerProfile(inspectingGame.runnerId);

            if (runner == null)
                return;

            if (await DependencyManager.OpenYesNoModal("Clean profile", "Are you sure you want to clean the profile? All data for this game will be lost"))
                await DependencyManager.OpenLoadingModal(true, new LoadingTask("Cleaning profile", "Deleting...", () => runner.CleanProfile(inspectingGame)));
        }

        private async Task TryToRefreshSteamMetaData()
        {
            if (long.TryParse(element.inp_Library_SteamId.getText, out long id))
                await DependencyManager.OpenLoadingModal(true, () => SteamHelper.UpdateExistingGame(id, inspectingGame));
        }

        private async Task TryToEditSelectedProfile()
        {
            RunnerDto? runner = RunnerManager.GetRunnerProfile(inspectingGame.runnerId);

            if (runner == null)
                return;

            await MainWindow.instance!.DisplayModalAsync<Modal_Settings_Runner>(EditModal);
            async Task EditModal(Modal_Settings_Runner modal) => await modal.HandleOpen(runner.runnerId);
        }


        internal abstract class ConfigChangerBase
        {
            protected Game_Config key;
            protected Func<Game?> inspectingGameFetcher;

            private RunnerDto.RunnerType[] supportedTypes;
            private Visual control;

            public ConfigChangerBase(Visual group, Game_Config key, Func<Game?> getInspectingGame, RunnerDto.RunnerType[] supportedTypes)
            {
                this.key = key;
                this.inspectingGameFetcher = getInspectingGame;

                this.supportedTypes = supportedTypes;
                this.control = group;
            }

            public abstract Task Load(Game game);

            public void HandleSupportedType(RunnerDto.RunnerType selectedType)
            {
                bool supported = supportedTypes.Length > 0 ? supportedTypes.Contains(selectedType) : true;
                control.IsVisible = supported;
            }
        }

        internal sealed class ConfigChanger_Toggle : ConfigChangerBase
        {
            private Common_Toggle ui;

            public ConfigChanger_Toggle(Common_Toggle ui, Game_Config key, Func<Game?> getInspectingGame, params RunnerDto.RunnerType[] supportedTypes)
                : base((Visual)ui.Parent!, key, getInspectingGame, supportedTypes)
            {
                this.ui = ui;
                this.ui.RegisterOnChange(SaveToggle);
            }

            public override Task Load(Game game)
            {
                ui.SilentSetValue(game.config.GetBoolean(key, false));
                return Task.CompletedTask;
            }

            private async Task SaveToggle(bool to)
            {
                Game? game = inspectingGameFetcher();

                if (game == null)
                    return;

                await game.config.SaveBool(key, to);
            }
        }

        internal sealed class ConfigChanger_Dropdown : ConfigChangerBase
        {
            private Common_Dropdown ui;
            private int? defaultVal;

            public ConfigChanger_Dropdown(Common_Dropdown ui, Game_Config key, string[] options, int? defaultOption, Func<Game?> getInspectingGame, params RunnerDto.RunnerType[] supportedTypes)
                : base((Visual)ui.Parent!, key, getInspectingGame, supportedTypes)
            {
                this.ui = ui;
                this.defaultVal = defaultOption;

                this.ui.Setup(options, defaultOption ?? 0, Save);
            }

            public override Task Load(Game game)
            {
                ui.SilentlyChangeValue(game.config.GetInteger(key, defaultVal ?? 0));
                return Task.CompletedTask;
            }

            private async Task Save()
            {
                Game? game = inspectingGameFetcher();

                if (game == null)
                    return;

                await game.config.SaveInteger(key, ui.selectedIndex);
            }
        }

        internal sealed class ConfigChanger_InputField : ConfigChangerBase
        {
            private Common_InputField ui;

            public ConfigChanger_InputField(Common_InputField ui, Game_Config key, Func<Game?> getInspectingGame, params RunnerDto.RunnerType[] supportedTypes)
                : base((Visual)ui.Parent!, key, getInspectingGame, supportedTypes)
            {
                this.ui = ui;
                this.ui.OnChange(Save);
            }

            public override Task Load(Game game)
            {
                ui.SilentlyChangeValue(game.config.GetValue(key));
                return Task.CompletedTask;
            }

            private async Task Save()
            {
                Game? game = inspectingGameFetcher();

                if (game == null)
                    return;

                await game.config.SaveValue(key, ui.getText);
            }
        }
    }
}