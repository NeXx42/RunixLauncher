using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using Runix.Structure.Data;

namespace RunixLauncher.Controls.Settings;

public class Control_Settings_ExternalApplicationList : Common_ItemList_Generic<Label, Data_ExternalApplication>, ISettingControl
{
    private Data_ExternalApplication[] existingApplication = [];

    public Control_Settings_ExternalApplicationList() : base()
    {
        Label = "External applications";

        Register(RequestApplication);
    }


    public async Task LoadValue()
    {
        existingApplication = ConfigHandler.configProvider!.GetList<Data_ExternalApplication>(ConfigKeys.ExternalApplicationList);
        DrawListContent(existingApplication, RequestModal);
    }

    public override void OnDrawListEntry(Label ui, int id, Data_ExternalApplication dat)
    {
        ui.Content = dat.title;
    }

    private async Task RequestApplication()
    {
        string? newApp = await DependencyManager.OpenFileDialog("exe");

        if (string.IsNullOrEmpty(newApp) || !File.Exists(newApp))
            return;

        await ConfigHandler.configProvider!.SaveList(ConfigKeys.ExternalApplicationList, [
            ..existingApplication,
            new Data_ExternalApplication{
                title = Path.GetFileName(newApp),
                path = newApp
            }
        ]);

        await LoadValue();
    }

    private async Task RequestModal(Data_ExternalApplication dat)
    {
        switch (await DependencyManager.OpenMultiModal(dat.title, true, ["Rename", "Relocate", "Remote"]))
        {
            case 0:
                string? newName = await DependencyManager.OpenStringInputModal("App name", dat.title);

                if (string.IsNullOrEmpty(newName))
                    return;

                dat.title = newName;
                await Save();
                break;

            case 1:
                string? newApp = await DependencyManager.OpenFileDialog("exe");

                if (string.IsNullOrEmpty(newApp) || !File.Exists(newApp))
                    return;

                dat.path = newApp;
                await Save();
                break;

            case 2:
                if (!await DependencyManager.OpenYesNoModal("Remote entry?", $"Are you sure you want to remove the external application entry (This will not delete the file)? {dat.path}"))
                    return;

                existingApplication = existingApplication.Where(e => e != dat).ToArray();
                await Save();
                break;
        }

        async Task Save()
        {
            await ConfigHandler.configProvider!.SaveList(ConfigKeys.ExternalApplicationList, existingApplication);
            await LoadValue();
        }
    }
}
