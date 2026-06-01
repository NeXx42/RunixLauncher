namespace Runix.Logic.Objects.Runners;

public interface IWineRunner
{
    public Task<(string title, string path)[]> GetPrefixes();
}
