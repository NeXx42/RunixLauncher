using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using Runix.Logic.Settings.UI;

namespace Runix.Logic.Settings;

public class Setting_RuntimeDerived : SettingBase
{
    private ConfigHandler.RuntimeConfigs name;
    private SettingOSCompatibility compatibility;

    public override string getName => name.ToString();
    public override SettingOSCompatibility getCompatibility => compatibility;

    public override ISettingsUI GetUI() => new SettingsUI_RuntimeDerived();

    public Setting_RuntimeDerived(ConfigHandler.RuntimeConfigs name, SettingOSCompatibility compatibility)
    {
        this.name = name;
        this.compatibility = compatibility;
    }

    public override Task<T> LoadSetting<T>(T fallback)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> SaveSetting<T>(T val)
    {
        throw new NotImplementedException();
    }
}
