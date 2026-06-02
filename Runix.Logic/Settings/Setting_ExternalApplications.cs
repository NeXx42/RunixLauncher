using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;
using Runix.Logic.Settings.UI;
using Runix.Structure.Data;

namespace Runix.Logic.Settings;

public class Setting_ExternalApplications : SettingBase
{
    public override string getName => "Runners";
    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;

    public override ISettingsUI GetUI() => new SettingsUI_ExternalApplications();

    public override async Task<T> LoadSetting<T>(T fallback) where T : default
    {
        if (ConfigHandler.configProvider!.TryGetList(GameLibrary.Logic.Enums.ConfigKeys.ExternalApplicationList, out Data_ExternalApplication[] res))
            return (T)(object)res;

        return (T)(object)new List<Data_ExternalApplication>();
    }

    public override async Task<bool> SaveSetting<T>(T val)
    {
        if (val is not List<Data_ExternalApplication> applications)
            throw new Exception("Invalid type");

        if (!await ConfigHandler.configProvider!.SaveList(GameLibrary.Logic.Enums.ConfigKeys.ExternalApplicationList, applications))
            return false;

        return true;
    }
}
