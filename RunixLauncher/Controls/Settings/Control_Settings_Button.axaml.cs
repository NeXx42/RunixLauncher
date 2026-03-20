using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.Settings;

public partial class Control_Settings_Button : UserControl, ISettingControl
{
    public Control_Settings_Button(SettingsUI_Button info)
    {
        InitializeComponent();

        title.Content = info.title;

        btn.Label = info.btnText;
        btn.RegisterClick(info.callback);
    }

    public Task LoadValue() => Task.CompletedTask;
}