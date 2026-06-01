namespace GameLibrary.Logic.Integration;

public static class Integration_EpicGames
{
    public static async Task Install()
    {
        string? launcherStorage = await DependencyManager.OpenFolderDialog("Install location");

        if (string.IsNullOrEmpty(launcherStorage))
            return;

        using (HttpClient client = new HttpClient())
        {

        }


    }
}