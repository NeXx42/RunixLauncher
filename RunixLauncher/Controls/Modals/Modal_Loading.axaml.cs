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


    public async Task LoadTasks(bool showLoading, params LoadingTask[] tasks)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        if (showLoading)
        {
            inp_ProgressBar.Minimum = 0;
            inp_ProgressBar.Maximum = tasks.Length - 1;

            for (int i = 0; i < tasks.Length; i++)
            {
                lbl_Header.Content = tasks[i].header;

                try
                {
                    foreach (var subTask in tasks[i].task)
                    {
                        lbl_Description.Text = subTask.Item1;

                        await subTask.Item2();
                        await Dispatcher.UIThread.InvokeAsync(() => inp_ProgressBar.Value = i);
                    }
                }
                catch (Exception e)
                {
                    await DependencyManager.OpenExceptionDialog("", e);
                }
            }
        }
        else
        {
            await Task.WhenAll(tasks.SelectMany(x => x.task.Select(x => x.Item2())));
        }
    }
}