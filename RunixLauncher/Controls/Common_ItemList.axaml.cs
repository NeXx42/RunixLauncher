using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RunixLauncher.Helpers;

namespace RunixLauncher.Controls;

public partial class Common_ItemList : UserControl, INotifyPropertyChanged
{
    public string Label { get; set; } = "";
}

public class Common_ItemList_Generic<T, TDATA> : Common_ItemList
    where T : Control
{
    private ReusableList<Common_ItemList_Entry>? uiList;

    public Common_ItemList_Generic()
    {
        InitializeComponent();
    }

    protected void Register(Func<Task>? onAddCallback)
    {
        uiList = new ReusableList<Common_ItemList_Entry>(cont, () => new Common_ItemList_Entry(typeof(T)));

        if (onAddCallback != null)
        {
            btn_Add.IsVisible = true;
            btn_Add.PointerPressed += (_, __) => _ = onAddCallback();
        }
        else
        {
            btn_Add.IsVisible = false;
        }
    }

    public virtual void DrawListContent(IEnumerable<TDATA> data, Func<TDATA, Task>? requestEdit)
        => uiList!.Draw(data, (ui, i, dat) =>
        {
            ui.Draw(i % 2 != 0, requestEdit != null ? () => _ = requestEdit(dat) : null);
            OnDrawListEntry((T)ui.subControl, i, dat);
        });

    public virtual void OnDrawListEntry(T ui, int id, TDATA dat)
    {

    }
}