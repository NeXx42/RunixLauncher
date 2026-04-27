using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RunixLauncher.Controls;

public partial class Common_ContextMenu : UserControl
{
    private (string, MenuItem)[]? activeItems;
    private Action<int>? callback;

    public Common_ContextMenu()
    {
        InitializeComponent();
        ctrl.Click += (_, __) => menu.Open(ctrl);
    }

    public void SetupString(IEnumerable<string> options, Action<string> onCallback)
        => SetupInternal(options, (int id) => onCallback?.Invoke(activeItems![id].Item1));

    public void Setup(IEnumerable<string> options, Func<int, Task> onCallback)
        => SetupInternal(options, (i) => _ = onCallback(i));

    private void Setup(IEnumerable<string> options, Action<int> onCallback)
        => SetupInternal(options, onCallback);


    private void SetupInternal(IEnumerable<string> options, Action<int> onCallback)
    {
        callback = onCallback;
        activeItems = new (string, MenuItem)[options.Count()];

        for (int i = 0; i < activeItems.Length; i++)
        {
            int temp = i;

            MenuItem item = new MenuItem();
            item.Header = options.ElementAt(i);
            item.Click += (_, __) => callback?.Invoke(temp);

            menu.Items.Add(item);
            activeItems[i] = (options.ElementAt(i), item);
        }
    }
}