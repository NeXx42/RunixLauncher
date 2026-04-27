using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RunixLauncher.Controls.Modals;

public partial class Modal_MultiSelect : UserControl
{
    private TaskCompletionSource<int?>? completeResponse;

    public Modal_MultiSelect()
    {
        InitializeComponent();
    }

    public Task<int?> RequestModal(string title, string[] btns)
    {
        completeResponse = new TaskCompletionSource<int?>();
        lbl_Title.Content = title;

        this.btns.Children.Clear();

        for (int i = 0; i < btns.Length; i++)
        {
            int temp = i;
            Common_Button btnUI = new Common_Button();

            btnUI.Label = btns[i];

            btnUI.Height = 30;
            btnUI.RegisterClick(() => completeResponse?.SetResult(temp));

            this.btns.Children.Add(btnUI);
        }

        return completeResponse.Task;
    }
}