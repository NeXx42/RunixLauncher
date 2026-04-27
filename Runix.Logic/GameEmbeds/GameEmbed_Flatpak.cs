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
        // cant get to work
        return;
        string actualCommand = inp.command;
        inp.command = "flatpak";

        var flatpak = new LinkedList<string>();
        var node = flatpak.AddFirst("run");
        node = flatpak.AddAfter(node, "--command=flatpak-spawn");
        node = flatpak.AddAfter(node, "com.nexx.RunixLauncher");
        node = flatpak.AddAfter(node, "--host");

        inp.arguments[RunnerManager.ArgumentType.Flatpak] = flatpak;
        inp.arguments[RunnerManager.ArgumentType.Launcher].AddFirst(actualCommand);
    }
}