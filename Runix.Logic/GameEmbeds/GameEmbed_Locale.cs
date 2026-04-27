using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameEmbeds;

public class GameEmbed_Locale : IGameEmbed
{
    public int getOrder => (int)RunnerManager.ArgumentType.LocaleEmulator;

    public const string EMULATOR_PROGRAM_NAME = "LEProc.exe";

    public void Embed(RunnerManager.LaunchArguments inp, ConfigProvider<RunnerDto.RunnerConfigValues> args)
    {
        if (!ConfigHandler.isOnLinux && (ConfigHandler.configProvider?.TryGetValue(Enums.ConfigKeys.LocaleEmulator_Location, out string localeEmulatorPath) ?? false))
        {
            LinkedList<string> commands = new LinkedList<string>();
            commands.AddFirst(Path.Combine(localeEmulatorPath, EMULATOR_PROGRAM_NAME));

            inp.arguments[RunnerManager.ArgumentType.LocaleEmulator] = commands;
        }

        inp.environmentArguments.Add("LANG", "ja_JP.UTF-8");

        inp.environmentArguments.Add("LC_ALL", "ja_JP.UTF-8");
        inp.environmentArguments.Add("LC_CTYPE", "ja_JP.UTF-8");
    }
}
