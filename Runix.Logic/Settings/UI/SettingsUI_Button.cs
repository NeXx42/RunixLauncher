namespace GameLibrary.Logic.Settings.UI;

public class SettingsUI_Button : ISettingsUI
{
    public readonly string title;
    public readonly string btnText;
    public readonly Func<Task> callback;

    public SettingsUI_Button(string title, string btnText, Func<Task> callback)
    {
        this.title = title;
        this.btnText = btnText;
        this.callback = callback;
    }
}