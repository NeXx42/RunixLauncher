using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using RunixLauncher.Controls.Modals;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.Settings;

public partial class Control_Settings_Button : UserControl, ISettingControl
{
    public Control_Settings_Button(SettingsUI_Button info)
    {
        InitializeComponent();

        title.Content = info.title;

        btn.Label = info.btnText;

        if (info.modal.HasValue)
        {
            btn.RegisterClick(() => OpenModal(info.modal.Value));
        }
        else if (info.callback != null)
        {
            btn.RegisterClick(info.callback);
        }
    }

    private async Task OpenModal(SettingsUI_Button.GenericModalLaunchers modal)
    {
        switch (modal)
        {
            case SettingsUI_Button.GenericModalLaunchers.SteamBridge:
                await MainWindow.instance!.DisplayModalAsync<Modal_SteamBridge>(m => m.Open());
                break;
        }
    }

    public Task LoadValue() => Task.CompletedTask;
}