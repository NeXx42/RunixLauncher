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

namespace GameLibrary.AvaloniaUI.Controls.SubPages.Popup_GameView_Tabs;

public partial class Popup_GameView_Tab_Settings : Popup_GameView_TabBase
{
    public Popup_GameView_Tab_Settings()
    {
        InitializeComponent();
    }

    public override Tab CreateGroup(Common_ButtonToggle btn, Panel parent)
    {
        parent.Children.Add(this);
        return new Tab_LaunchSettings(this, btn);
    }

    public class Tab_LaunchSettings : Tab
    {
        private new Popup_GameView_Tab_Settings element;

        private List<RunnerDto>? possibleRunners;
        private ConfigChangerBase[] configOptions;

        public Tab_LaunchSettings(Popup_GameView_Tab_Settings element, Common_ButtonToggle btn) : base(element, btn)
        {
            configOptions = [];
            this.element = element;
        }

        protected override void InternalSetup(TabGroup master)
        {
            configOptions = [
                new ConfigChanger_Toggle(element.inp_Emulate, Game_Config.General_LocaleEmulation, () => inspectingGame),
                new ConfigChanger_Dropdown(element.inp_CaptureLogs, Game_Config.General_LoggingLevel, Enum.GetNames<LoggingLevel>(), () => inspectingGame),
                new ConfigChanger_InputField(element.inp_Arguments, Game_Config.General_Arguments, () => inspectingGame),

                new ConfigChanger_Toggle(element.inp_Wine_Windowed, Game_Config.Wine_Windowed, () => inspectingGame, RunnerDto.RunnerType.Wine, RunnerDto.RunnerType.Wine_GE),
                new ConfigChanger_Toggle(element.inp_IsolatePrefix, Game_Config.Wine_IsolatedPrefix, () => inspectingGame, RunnerDto.RunnerType.Wine, RunnerDto.RunnerType.Wine_GE),
                new ConfigChanger_Toggle(element.inp_Wine_VirtualDesktop, Game_Config.Wine_ExplorerLaunch, () => inspectingGame, RunnerDto.RunnerType.Wine, RunnerDto.RunnerType.Wine_GE),
                new ConfigChanger_Toggle(element.inp_Wine_LaunchAsConsole, Game_Config.Wine_ConsoleLaunched, () => inspectingGame, RunnerDto.RunnerType.Wine, RunnerDto.RunnerType.Wine_GE),
            ];
        }

        protected override async Task OpenWithGame(GameDto? game, bool isNewGame)
        {
            if (game is GameDto_Steam)
            {
                element.IsVisible = false;
                return;
            }

            DrawRunners(game!);

            if (isNewGame)
            {
                DrawBinaries(game!);
                await UpdateSupportedSettings();
            }

            foreach (ConfigChangerBase config in configOptions)
                await config.Load(game!);
        }

        private void DrawRunners(GameDto game)
        {
            possibleRunners = RunnerManager.GetRunnerProfiles().ToList();
            string firstProfile = possibleRunners.Count > 0 ? possibleRunners[0].runnerName : "INVALID";

            string[] profileOptions = [$"Default ({firstProfile})", .. possibleRunners!.Select(x => x.runnerName)!.ToArray()];
            int selectedProfile = possibleRunners.Select(x => x.runnerId).ToList().IndexOf(game.runnerId ?? -1);

            element.inp_WineProfile.IsVisible = true;
            element.inp_WineProfile.Setup(profileOptions, selectedProfile >= 0 ? (selectedProfile + 1) : 0, HandleWineProfileChange);
        }

        private void DrawBinaries(GameDto game)
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
        }

        private async Task HandleBinaryChange() => await inspectingGame!.ChangeBinaryLocation(element.inp_binary.selectedValue?.ToString());
        private async Task HandleWineProfileChange()
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

        private async Task UpdateSupportedSettings()
        {
            var runnerType = (await RunnerManager.GetRunnerProfile(inspectingGame!.runnerId)).runnerType;

            ((Visual)element.inp_binary.Parent!).IsVisible = runnerType == RunnerDto.RunnerType.Proton_GE
                                            || runnerType == RunnerDto.RunnerType.Wine
                                            || runnerType == RunnerDto.RunnerType.Wine_GE
                                            || runnerType == RunnerDto.RunnerType.umu_Launcher;

            foreach (ConfigChangerBase config in configOptions)
                config.HandleSupportedType(runnerType);
        }



        internal abstract class ConfigChangerBase
        {
            protected Game_Config key;
            protected Func<GameDto?> inspectingGameFetcher;

            private RunnerDto.RunnerType[] supportedTypes;
            private Visual control;

            public ConfigChangerBase(Visual group, Game_Config key, Func<GameDto?> getInspectingGame, RunnerDto.RunnerType[] supportedTypes)
            {
                this.key = key;
                this.inspectingGameFetcher = getInspectingGame;

                this.supportedTypes = supportedTypes;
                this.control = group;
            }

            public abstract Task Load(GameDto game);

            public void HandleSupportedType(RunnerDto.RunnerType selectedType)
            {
                bool supported = supportedTypes.Length > 0 ? supportedTypes.Contains(selectedType) : true;
                control.IsVisible = supported;
            }
        }

        internal sealed class ConfigChanger_Toggle : ConfigChangerBase
        {
            private Common_Toggle ui;

            public ConfigChanger_Toggle(Common_Toggle ui, Game_Config key, Func<GameDto?> getInspectingGame, params RunnerDto.RunnerType[] supportedTypes)
                : base((Visual)ui.Parent!, key, getInspectingGame, supportedTypes)
            {
                this.ui = ui;
                this.ui.RegisterOnChange(SaveToggle);
            }

            public override Task Load(GameDto game)
            {
                ui.SilentSetValue(game.config.GetBoolean(key, false));
                return Task.CompletedTask;
            }

            private async Task SaveToggle(bool to)
            {
                GameDto? game = inspectingGameFetcher();

                if (game == null)
                    return;

                await game.config.SaveBool(key, to);
            }
        }

        internal sealed class ConfigChanger_Dropdown : ConfigChangerBase
        {
            private Common_Dropdown ui;

            public ConfigChanger_Dropdown(Common_Dropdown ui, Game_Config key, string[] options, Func<GameDto?> getInspectingGame, params RunnerDto.RunnerType[] supportedTypes)
                : base((Visual)ui.Parent!, key, getInspectingGame, supportedTypes)
            {
                this.ui = ui;
                this.ui.Setup(options, 0, Save);
            }

            public override Task Load(GameDto game)
            {
                ui.SilentlyChangeValue(game.config.GetInteger(key, 0));
                return Task.CompletedTask;
            }

            private async Task Save()
            {
                GameDto? game = inspectingGameFetcher();

                if (game == null)
                    return;

                await game.config.SaveInteger(key, ui.selectedIndex);
            }
        }

        internal sealed class ConfigChanger_InputField : ConfigChangerBase
        {
            private Common_InputField ui;

            public ConfigChanger_InputField(Common_InputField ui, Game_Config key, Func<GameDto?> getInspectingGame, params RunnerDto.RunnerType[] supportedTypes)
                : base((Visual)ui.Parent!, key, getInspectingGame, supportedTypes)
            {
                this.ui = ui;
                this.ui.OnChange(Save);
            }

            public override Task Load(GameDto game)
            {
                ui.SilentlyChangeValue(game.config.GetValue(key));
                return Task.CompletedTask;
            }

            private async Task Save()
            {
                GameDto? game = inspectingGameFetcher();

                if (game == null)
                    return;

                await game.config.SaveValue(key, ui.getText);
            }
        }
    }
}