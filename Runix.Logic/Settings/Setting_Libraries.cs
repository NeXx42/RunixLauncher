using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using Runix.Logic.Settings.UI;

namespace Runix.Logic.Settings;

public class Setting_Libraries : SettingBase
{
    public override string getName => "Libraries";
    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;

    public override ISettingsUI GetUI() => new SettingsUI_Libraries();

    public override Task<T> LoadSetting<T>(T fallback)
    {
        return Task.FromResult((T)(object)LibraryManager.GetLibraries());
    }

    public override Task<bool> SaveSetting<T>(T val) => Task.FromResult(false);
}
