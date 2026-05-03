using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runix.Logic.Helpers;

namespace RunixLauncher.Controls.Modals;

public partial class Modal_SteamBridge : UserControl
{
    private TaskCompletionSource? openTask;

    public Modal_SteamBridge()
    {
        InitializeComponent();
        btn_Validate.RegisterClick(ValidateBridge, "Validating");
        btn_Close.RegisterClick(() => openTask?.SetResult());
    }

    public Task Open()
    {
        string dir = SteamHelper.CreateSteamBridge();
        inp_Dir.Text = $"{dir}";

        openTask = new TaskCompletionSource();
        return openTask.Task;
    }

    private async Task ValidateBridge()
    {
        btn_Validate.Label = await SteamHelper.ValidateBridge() ? "Valid" : "Invalid";
    }
}