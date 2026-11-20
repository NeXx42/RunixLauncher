using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RunixLauncher.Controls;

public partial class Common_ItemList_Entry : UserControl
{
    public Control subControl { private set; get; }

    private Action? requestEditCallback;

    public Common_ItemList_Entry()
    {
        InitializeComponent();
        subControl = null!;
    }

    public Common_ItemList_Entry(Type obj)
    {
        InitializeComponent();

        subControl = (Control)Activator.CreateInstance(obj)!;
        cont.Child = subControl;

        btn_Edit.PointerPressed += (_, __) => requestEditCallback?.Invoke();
    }

    public void Draw(bool isOdd, Action? onRequestEdit)
    {
        requestEditCallback = null;
        btn_Edit.IsVisible = onRequestEdit != null;
        requestEditCallback = onRequestEdit;

        Classes.Clear();

        if (isOdd)
            Classes.Add("Odd");
    }
}