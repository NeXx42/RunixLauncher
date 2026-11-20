using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace RunixLauncher.Controls;

public partial class Common_ButtonToggle : UserControl
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<Common_ButtonToggle, string>(nameof(Label), string.Empty);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<string> InternalPaddingProperty = AvaloniaProperty.Register<Common_ButtonToggle, string>(nameof(InternalPadding), "10 2");

    public string InternalPadding
    {
        get => GetValue(InternalPaddingProperty);
        set => SetValue(InternalPaddingProperty, value);
    }


    public bool isSelected
    {
        set
        {
            if (value)
                ctrl.Classes.Add("Selected");
            else
                ctrl.Classes.Clear();

            m_isSelected = value;
        }
        get => m_isSelected;
    }
    private bool m_isSelected;

    private Action<bool>? callback;

    public bool autoToggle = true;

    public Common_ButtonToggle()
    {
        InitializeComponent();
        DataContext = this;

        isSelected = false;
        ctrl.PointerPressed += (_, __) => HandleToggle();
    }

    public void Register(Action<bool> onToggle)
    {
        callback = onToggle;
    }

    public void Register(Func<bool, Task> onToggle, string? asyncMessage)
    {
        callback = (b) => _ = onToggle(b);
    }

    private void HandleToggle()
    {
        if (autoToggle)
        {
            isSelected = !isSelected;
        }

        callback?.Invoke(isSelected);
    }
}