using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameEmbeds;

public interface IGameEmbed
{
    public void Embed(RunnerManager.LaunchArguments inp, ConfigProvider<RunnerDto.RunnerConfigValues> args);
}
