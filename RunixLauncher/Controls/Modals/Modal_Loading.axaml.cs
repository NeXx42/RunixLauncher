using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GameLibrary.Logic;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls.Modals;

public partial class Modal_Loading : UserControl
{
    public Modal_Loading()
    {
        InitializeComponent();
    }


    public async Task LoadTasks(bool showLoading, params Func<Task>[] tasks)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        if (showLoading)
        {
            lbl_Header.Content = "Loading";
            lbl_Description.Text = "Loading...";

            inp_ProgressBar.Minimum = 0;
            inp_ProgressBar.Maximum = tasks.Length - 1;

            for (int i = 0; i < tasks.Length; i++)
            {
                try
                {
                    await tasks[i]();
                    await Dispatcher.UIThread.InvokeAsync(() => inp_ProgressBar.Value = i);
                }
                catch (Exception e)
                {
                    await DependencyManager.OpenExceptionDialog("", e);
                }
            }
        }
        else
        {
            await Task.WhenAll(tasks.Select(x => x()));
        }
    }
}