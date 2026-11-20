using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.Modals;

public partial class Modal_MultiSelect : UserControl
{
    private TaskCompletionSource<int?>? completeResponse;
    private ReusableList<Common_Button> btnContainer;

    public Modal_MultiSelect()
    {
        InitializeComponent();

        btnContainer = new ReusableList<Common_Button>(btns);
        btn_Close.RegisterClick(() => completeResponse?.SetResult(null));
    }

    public Task<int?> RequestModal(string title, bool allowClosing, params string[] btns)
    {
        completeResponse = new TaskCompletionSource<int?>();
        lbl_Title.Content = title;

        btn_Close.IsVisible = allowClosing;
        btnContainer.Draw(btns, (ui, i, dat) =>
        {
            ui.Label = dat;
            ui.Height = 30;
            ui.RegisterClick(() => completeResponse?.SetResult(i));
        });

        return completeResponse.Task;
    }
}