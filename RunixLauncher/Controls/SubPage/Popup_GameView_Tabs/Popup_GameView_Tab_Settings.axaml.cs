using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.SubPages.Popup_GameView_Tabs;

public partial class Popup_GameView_Tab_Settings : Popup_GameView_TabBase
{
    private UITabGroup tabGroup;

    public Popup_GameView_Tab_Settings()
    {
        InitializeComponent();

        tabGroup = new UITabGroup(
            new UITabGroup_GroupToggleButton(tab_General, tabBtn_General),
            new UITabGroup_GroupToggleButton(tab_LaunchSettings, tabBtn_Launching),
            new UITabGroup_GroupToggleButton(tab_RunnerSettings, tabBtn_Runner)
        );

        _ = tabGroup.ChangeSelection(0);
    }

    public override Tab CreateGroup(Common_ButtonToggle btn, Panel parent)
    {
        parent.Children.Add(this);
        return new Tab_LaunchSettings(this, btn);
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
                new ConfigChanger_Dropdown(element.inp_CaptureLogs, Game_Config.General_LoggingLevel, Enum.GetNames<LoggingLevel>(), () => inspectingGame),
                new ConfigChanger_InputField(element.inp_Arguments, Game_Config.General_Arguments, () => inspectingGame),

                new ConfigChanger_Toggle(element.inp_Wine_Windowed, Game_Config.Wine_Windowed, () => inspectingGame, wineRunnerTypes),
                new ConfigChanger_Toggle(element.inp_IsolatePrefix, Game_Config.Wine_IsolatedPrefix, () => inspectingGame, wineRunnerTypes),
                new ConfigChanger_Toggle(element.inp_Wine_VirtualDesktop, Game_Config.Wine_ExplorerLaunch, () => inspectingGame, wineRunnerTypes),
                new ConfigChanger_Toggle(element.inp_Wine_LaunchAsConsole, Game_Config.Wine_ConsoleLaunched, () => inspectingGame, wineRunnerTypes),

                new ConfigChanger_InputField(element.inp_umu_Id, Game_Config.Launcher_umu_Id, () => inspectingGame, RunnerDto.RunnerType.umu_Launcher),
            ];
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

            public ConfigChanger_Dropdown(Common_Dropdown ui, Game_Config key, string[] options, Func<Game?> getInspectingGame, params RunnerDto.RunnerType[] supportedTypes)
                : base((Visual)ui.Parent!, key, getInspectingGame, supportedTypes)
            {
                this.ui = ui;
                this.ui.Setup(options, 0, Save);
            }

            public override Task Load(Game game)
            {
                ui.SilentlyChangeValue(game.config.GetInteger(key, 0));
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