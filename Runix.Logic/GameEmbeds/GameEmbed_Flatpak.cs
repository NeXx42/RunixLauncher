using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameEmbeds;

/// <summary>
/// To stop the game launched from having the same restrictions as the sandbox
/// </summary>
public class GameEmbed_Flatpak : IGameEmbed
{
    public int getOrder => (int)RunnerManager.ArgumentType.Flatpak;

    public void Embed(RunnerManager.LaunchArguments inp, ConfigProvider<RunnerDto.RunnerConfigValues> args)
    {
        string actualCommand = inp.command;
        inp.command = "flatpak-spawn";

        var flatpak = new LinkedList<string>();
        flatpak.AddFirst("--host");

        inp.arguments[RunnerManager.ArgumentType.Flatpak] = flatpak;
        inp.arguments[RunnerManager.ArgumentType.Launcher].AddFirst(actualCommand);
    }
}