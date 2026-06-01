using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Integration;
using GameLibrary.Logic.Objects;
using GameLibrary.Logic.Settings.UI;

namespace GameLibrary.Logic.Settings;

public class Setting_EpicGamesIntegration : SettingBase
{
    public override string getName => "Epic Games Integration";

    public override SettingOSCompatibility getCompatibility => SettingOSCompatibility.Universal;
    public override ISettingsUI GetUI() => new SettingsUI_Toggle("Sync", "Enable");

    public override async Task<T> LoadSetting<T>(T fallback) where T : default
    {
        return (T)(object)await Database_Manager.Exists<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.status), (int)Game_Status.EpicGamesIntegrator));
    }

    public override async Task<bool> SaveSetting<T>(T val)
    {
        await DependencyManager.OpenConfirmationAsync("Integrate?", "Select a folder to store the install",
            ("Install", Integration_EpicGames.Install, "Select "));

        // ui flips this if a success
        return false;
    }
}
