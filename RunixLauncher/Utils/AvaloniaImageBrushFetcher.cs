using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Interfaces;

namespace RunixLauncher.Utils;

public class AvaloniaImageBrushFetcher : IImageFetcher
{
    public async Task<object?> GetIcon(string absolutePath, int? resolution, int? interpolation)
    {
        Bitmap bitmap;

        if (resolution.HasValue)
        {
            using FileStream stream = new FileStream(absolutePath, new FileStreamOptions() { Access = FileAccess.Read, Share = FileShare.ReadWrite });
            bitmap = Bitmap.DecodeToWidth(stream, resolution.Value, (BitmapInterpolationMode)((interpolation ?? 1) + 2)); // default medium, low is 2 hence the offset
        }
        else
        {
            bitmap = new Bitmap(absolutePath);
        }

        ImageBrush? brush = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            brush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill
            };
        });

        return brush;
    }

    public async Task<string?> GetImageFromClipboard(float delayS)
    {
        if (delayS > 0)
        {
            await Task.Delay((int)Math.Round(delayS * 1000));
        }

        if (MainWindow.instance?.Clipboard == null)
            return string.Empty;

        Bitmap? img = await MainWindow.instance.Clipboard.TryGetBitmapAsync();

        if (img == null)
            return string.Empty;

        string tempPath = $"/tmp/{Guid.NewGuid()}";

        await using FileStream fs = File.OpenWrite(tempPath);
        img.Save(fs);

        return tempPath;
    }
}
