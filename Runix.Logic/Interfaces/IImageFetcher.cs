namespace GameLibrary.Logic.Interfaces;

public interface IImageFetcher
{
    public Task<object?> GetIcon(string absolutePath, int? quality, int? interpolation);
    public Task<string?> GetImageFromClipboard(float delayS);
}
