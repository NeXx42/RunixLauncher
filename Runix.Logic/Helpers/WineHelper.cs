using GameLibrary.Logic;

namespace Runix.Logic.Helpers;

public static class WineHelper
{
    public static void GetPrefixName(string root, RunnerManager.LaunchRequest req, out string path)
    {
        path = Path.Combine(root, "shared");

        if (req.gameId.HasValue && (req.gameConfig?.GetBoolean(GameLibrary.Logic.Enums.Game_Config.Wine_IsolatedPrefix, false) ?? false))
        {
            path = Path.Combine(root, req.gameId.Value.ToString());
        }
    }
}
