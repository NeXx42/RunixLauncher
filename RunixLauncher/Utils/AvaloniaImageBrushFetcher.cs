using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GameLibrary.DB.Tables;
using GameLibrary.Logic;
using GameLibrary.Logic.Interfaces;

namespace GameLibrary.AvaloniaUI.Utils;

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
}
