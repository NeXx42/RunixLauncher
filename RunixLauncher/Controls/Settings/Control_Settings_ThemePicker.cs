using System;
using System.Threading.Tasks;
using GameLibrary.Logic.Settings;
using RunixLauncher.Helpers;
using SharpCompress.Compressors.PPMd;

namespace RunixLauncher.Controls.Settings;

public class Control_Settings_ThemePicker : Control_Settings_Dropdown
{
    public Control_Settings_ThemePicker Draw(SettingBase setting)
    {
        this.settings = setting;
        title.Content = settings.getName;

        btn.Setup(ThemeHelper.GetThemes(), 0, SaveValue);
        return this;
    }

    public override async Task LoadValue()
    {
        await base.LoadValue();

        string? val = (string?)btn.selectedValue;
        await ThemeHelper.ChangeTheme(val);
    }

    protected override async Task SaveValue()
    {
        await base.SaveValue();

        string? val = (string?)btn.selectedValue;
        await ThemeHelper.ChangeTheme(val);
    }
}
