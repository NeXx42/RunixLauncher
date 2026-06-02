using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;
using GameLibrary.Logic.Settings;
using Runix.Logic.Settings.UI;
using Runix.Structure.Data;

namespace RunixLauncher.Controls.Settings;

public partial class Control_Settings_ExternalApplications : UserControl, ISettingControl
{
    private SettingBase setting;

    public Control_Settings_ExternalApplications()
    {
        InitializeComponent();
    }


    public ISettingControl Draw(SettingBase setting, SettingsUI_ExternalApplications ui)
    {
        this.setting = setting;
        inp_Control.Setup(
            () =>
            {
                Grid g = new Grid();
                g.ColumnDefinitions = [new ColumnDefinition(200, GridUnitType.Pixel), new ColumnDefinition(10, GridUnitType.Pixel), new ColumnDefinition(GridLength.Star)];

                TextBox l = new TextBox();
                l.TextChanged += (_, __) => _ = inp_Control.RequestUpdate();

                Common_Button b = new Common_Button();
                b.Label = "Browse";
                b.RegisterClick(async () =>
                {
                    string? path = await DependencyManager.OpenFileDialog("Tool", ["exe"]);

                    if (string.IsNullOrEmpty(path))
                        return;

                    b.Label = path;
                    await inp_Control.RequestUpdate();

                }, "Loading");

                Grid.SetColumn(b, 3);

                g.Children.Add(l);
                g.Children.Add(b);

                return g;
            },
            (Grid g, Data_ExternalApplication v) =>
            {
                (g.Children[0] as TextBox)!.Text = v.title;
                (g.Children[1] as Common_Button)!.Label = v.path ?? "Select";
            },
            () => new Data_ExternalApplication() { title = "New", path = null },
            (List<Grid> v) =>
            {
                List<Data_ExternalApplication> res = new List<Data_ExternalApplication>();

                foreach (Grid g in v)
                {
                    string? title = (g.Children[0] as TextBox)!.Text;
                    string? path = (g.Children[1] as Common_Button)!.Label;

                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(path) || path == "Select")
                        continue;

                    res.Add(new Data_ExternalApplication()
                    {
                        title = title,
                        path = path
                    });
                }

                return SaveValue(res);
            }
        );

        return this;
    }

    public async Task SaveValue(List<Data_ExternalApplication> res)
    {
        await setting.SaveSetting(res);
    }

    public async Task LoadValue()
    {
        await inp_Control.LoadAsync<Data_ExternalApplication>(async () => await setting.LoadSetting(new Data_ExternalApplication[0]));
    }
}