using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using GameLibrary.Logic;
using GameLibrary.Logic.Objects;
using RunixLauncher.Controls.Modals;

namespace RunixLauncher.Controls.Settings;

public class Control_Settings_LibraryList : Common_ItemList_Generic<Label, LibraryDto>, ISettingControl
{
    public Control_Settings_LibraryList()
    {
        Label = "Libraries";
        Register(() => OpenEditMenu());
    }

    public async Task LoadValue()
    {
        LibraryDto[] libs = LibraryManager.GetLibraries();
        DrawListContent(libs, DrawModal);
    }

    public override void OnDrawListEntry(Label ui, int id, LibraryDto dat)
    {
        ui.Content = dat.getFullName;
    }

    private async Task DrawModal(LibraryDto lib)
    {
        switch (await DependencyManager.OpenMultiModal(lib.getName, true, ["Edit", "Remove"]))
        {
            case 0:
                await OpenEditMenu(lib);
                break;

            case 1:
                await DeleteSelectedProfile();
                break;
        }
    }


    private async Task OpenEditMenu(LibraryDto? lib = null)
    {
        await MainWindow.instance!.DisplayModalAsync<Modal_Library>(HandleModal);

        async Task HandleModal(Modal_Library modal)
        {
            await modal.HandleOpen(lib);
            await LoadValue();
        }
    }

    private async Task DeleteSelectedProfile()
    {

    }


}
