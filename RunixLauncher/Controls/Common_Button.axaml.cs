using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using GameLibrary.Controller;
using GameLibrary.Logic.Helpers;
using RunixLauncher.Utils;

namespace RunixLauncher.Controls
{
    public partial class Common_Button : UserControl, IControlChild
    {
        private Action? callback;
        private string? defaultMessage;

        public Common_Button()
        {
            InitializeComponent();
            DataContext = this;

            ctrl.PointerPressed += (_, __) => callback?.Invoke();
        }

        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<Common_Button, string>(nameof(Label), string.Empty);

        public string Label
        {
            get => GetValue(LabelProperty);
            set
            {
                defaultMessage = value.ToUpper();
                SetValue(LabelProperty, value.ToUpper());
            }
        }

        public void RegisterClick(Func<IProgress<int>, Task> callback)
        {
            this.callback += ExtensionMethods.WrapTaskInExceptionHandler(HandleUpdateAsync);

            async Task HandleUpdateAsync()
            {
                Dispatcher.UIThread.Post(() => SetValue(LabelProperty!, "0%"));

                await callback(new Progress<int>((int perc) =>
                {
                    Dispatcher.UIThread.Post(() => SetValue(LabelProperty!, $"{perc}%"));
                }));

                Dispatcher.UIThread.Post(() => SetValue(LabelProperty!, defaultMessage));
            }
        }

        public void RegisterClick(Func<Task> callback, string? asyncMessage = "")
        {
            this.callback += ExtensionMethods.WrapTaskInExceptionHandler(HandleUpdateAsync);

            async Task HandleUpdateAsync()
            {
                if (!string.IsNullOrEmpty(asyncMessage))
                    Dispatcher.UIThread.Post(() => SetValue(LabelProperty!, asyncMessage));

                await callback();

                if (!string.IsNullOrEmpty(asyncMessage))
                    Dispatcher.UIThread.Post(() => SetValue(LabelProperty!, defaultMessage));
            }
        }

        public void RegisterClick(Action? callback)
        {
            this.callback += () => callback?.Invoke();
        }

        public Task Enter() => Task.CompletedTask;
        public Task<bool> Move(int x, int y) => Task.FromResult(false);

        public Task<bool> PressButton(ControllerButton btn)
        {
            if (btn == ControllerButton.A)
            {
                callback?.Invoke();
            }

            return Task.FromResult(false);
        }
    }
}
