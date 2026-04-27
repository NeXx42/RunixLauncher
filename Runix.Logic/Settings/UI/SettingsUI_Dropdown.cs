namespace GameLibrary.Logic.Settings.UI;

public struct SettingsUI_Dropdown : ISettingsUI
{
    public string[] options;
    public string? nullEntry;

    public SettingsUI_Dropdown(string[] options, string? nullEntry = null)
    {
        this.options = options;
        this.nullEntry = nullEntry;
    }
}
