using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic.Settings;
using GameLibrary.Logic.Settings.UI;

namespace RunixLauncher.Controls.Settings;

public partial class Control_Settings_Dropdown : UserControl, ISettingControl
{
    protected bool hasNullEntry;
    protected SettingBase? settings;

    public Control_Settings_Dropdown()
    {
        InitializeComponent();
    }

    public ISettingControl Draw(SettingBase settings, SettingsUI_Dropdown ui)
    {
        this.hasNullEntry = !string.IsNullOrEmpty(ui.nullEntry);
        this.settings = settings;

        title.Content = settings.getName;

        if (hasNullEntry)
        {
            btn.Setup((string[])[ui.nullEntry!, .. ui.options], 0, SaveValue);
        }
        else
        {
            btn.Setup(ui.options, 0, SaveValue);
        }

        return this;
    }

    public virtual async Task LoadValue()
    {
        int selected = await settings!.LoadSetting(-1);
        btn.SilentlyChangeValue(hasNullEntry ? selected + 1 : selected);
    }

    protected virtual async Task SaveValue()
    {
        int? selected = btn.selectedIndex;

        if (hasNullEntry)
        {
            selected--;
            selected = selected < 0 ? null : selected;
        }

        await settings!.SaveSetting(selected);
    }
}