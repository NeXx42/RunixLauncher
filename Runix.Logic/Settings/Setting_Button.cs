using System.Text;
using GameLibrary.Logic.Settings.UI;
using static GameLibrary.Logic.Settings.UI.SettingsUI_Button;

namespace GameLibrary.Logic.Settings;

public class Setting_Button : SettingBase
{
    public readonly SettingsUI_Button uiSettings;
    public readonly SettingOSCompatibility compatibility;

    public Setting_Button(string label, string buttonTxt, Func<Task> callback, SettingOSCompatibility compatibility)
    {
        this.uiSettings = new SettingsUI_Button(label, buttonTxt, callback);
        this.compatibility = compatibility;
    }

    public Setting_Button(string label, string buttonTxt, GenericModalLaunchers callback, SettingOSCompatibility compatibility)
    {
        this.uiSettings = new SettingsUI_Button(label, buttonTxt, callback);
        this.compatibility = compatibility;
    }

    public override string getName => uiSettings.title;
    public override SettingOSCompatibility getCompatibility => compatibility;

    public override ISettingsUI GetUI() => uiSettings;

    public override Task<T> LoadSetting<T>(T fallback) where T : default => Task.FromResult(fallback);
    public override Task<bool> SaveSetting<T>(T val) => Task.FromResult(true);
}
