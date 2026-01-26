using GameLibrary.Logic;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;
using static GameLibrary.Logic.Objects.RunnerDto;

namespace Runix.Logic.Helpers;

public static class WineHelper
{
    public const string SHARED_PREFIX_NAME = "shared";

    public static void GetPrefixName(string root, RunnerManager.LaunchRequest req, out string path)
    {
        path = Path.Combine(root, SHARED_PREFIX_NAME);

        if (req.gameId.HasValue && (req.gameConfig?.GetBoolean(GameLibrary.Logic.Enums.Game_Config.Wine_IsolatedPrefix, false) ?? false))
        {
            path = Path.Combine(root, req.gameId.Value.ToString());
        }
    }

    public static async Task SharePrefixDataFolders(string prefixFolder, string sharedLocation, RunnerDto runner)
    {
        if (!Directory.Exists(prefixFolder))
            throw new Exception("Profile hasnt been ran yet");

        string[] users = Directory.GetDirectories(Path.Combine(prefixFolder, "drive_c", "users"));

        foreach (string usr in users)
            HandleUser(usr);

        await runner.globalRunnerValues.SaveValue(RunnerConfigValues.Wine_SharedDocuments, sharedLocation);

        void HandleUser(string usrPath)
        {
            string usrName = Path.GetFileName(usrPath)!;
            usrName = usrName.Equals("Public") ? usrName : "user";

            // ensure share directory is correct
            string shareRoot = Path.Combine(sharedLocation, usrName);

            string shareDocuments = Path.Combine(shareRoot, "Documents");
            string shareLocal = Path.Combine(shareRoot, "AppData", "Local");
            string shareRoaming = Path.Combine(shareRoot, "AppData", "Roaming");
            string shareLocalLow = Path.Combine(shareRoot, "AppData", "LocalLow");

            if (!Directory.Exists(shareRoot))
            {
                shareRoot.CreateDirectoryIfNotExists();
                shareDocuments.CreateDirectoryIfNotExists();

                Path.Combine(shareRoot, "AppData").CreateDirectoryIfNotExists();
                shareLocal.CreateDirectoryIfNotExists();
                shareRoaming.CreateDirectoryIfNotExists();
                shareLocalLow.CreateDirectoryIfNotExists();
            }

            HandleSymlink(Path.Combine(usrPath, "Documents"), shareDocuments);
            HandleSymlink(Path.Combine(usrPath, "AppData", "Local"), shareLocal);
            HandleSymlink(Path.Combine(usrPath, "AppData", "Roaming"), shareRoaming);
            HandleSymlink(Path.Combine(usrPath, "AppData", "LocalLow"), shareLocalLow);

            void HandleSymlink(string prefixLoc, string sharedLoc)
            {
                if (File.Exists(prefixLoc))
                    File.Delete(prefixLoc);

                // want to make sure that the parent folders exist
                prefixLoc.CreateDirectoryIfNotExists();
                Directory.Delete(prefixLoc, true);

                Directory.CreateSymbolicLink(prefixLoc, sharedLoc);
            }
        }
    }
}
