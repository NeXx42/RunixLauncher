using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace RunixLauncher.Helpers;

public static class AnimationHelper
{
    public static async Task MoveUpAndFade(Control control, CancellationToken cancellationToken)
    {
        if (control.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform();
            control.RenderTransform = translate;
        }

        translate.Y = 0;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(100),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(Control.OpacityProperty, .6),
                        new Setter(TranslateTransform.YProperty, translate.Y + 30)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(Control.OpacityProperty, 1),
                        new Setter(TranslateTransform.YProperty, translate.Y)
                    }
                }
            }
        };

        await animation.RunAsync(control, cancellationToken);
    }
}
