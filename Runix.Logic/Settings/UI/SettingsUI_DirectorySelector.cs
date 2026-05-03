namespace GameLibrary.Logic.Settings.UI;

public struct SettingsUI_DirectorySelector : ISettingsUI
{
    public bool folder;
    public string defaultVal;

    public SettingsUI_DirectorySelector(string defaultVal, bool isFolder)
    {
        folder = isFolder;
        this.defaultVal = defaultVal;
    }
}