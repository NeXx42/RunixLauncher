using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic.GameEmbeds;

public class GameEmbed_Firejail : IGameEmbed
{
    public void Embed(RunnerManager.LaunchArguments inp, ConfigProvider<RunnerDto.RunnerConfigValues> args)
    {
        string actualCommand = inp.command;
        inp.command = "firejail";

        LinkedList<string> firejail = new LinkedList<string>();
        LinkedListNode<string> argumentsEnd = firejail.AddFirst("--noprofile");

        if (args.GetBoolean(RunnerDto.RunnerConfigValues.Generic_Sandbox_BlockNetwork, true))
        {
            argumentsEnd = firejail.AddAfter(argumentsEnd, "--net=none");
            //inp.environmentArguments.Add("WINEDLLOVERRIDES", "wininet=n;dnsapi=n;ws2_32=n");
        }

        if (args.GetBoolean(RunnerDto.RunnerConfigValues.Generic_Sandbox_IsolateFilesystem, true))
        {
            if (args.TryGetValue(RunnerDto.RunnerConfigValues.Wine_SharedDocuments, out string? documentsStorage) && !string.IsNullOrEmpty(documentsStorage))
            {
                argumentsEnd = firejail.AddAfter(argumentsEnd, $"--whitelist={documentsStorage}");
            }

            // hide user folder by default
            argumentsEnd = firejail.AddAfter(argumentsEnd, $"--whitelist={DependencyManager.GetUserStorageFolder()}");
            //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--read-only=~");

            foreach (string whitelist in inp.whiteListedDirs)
            {
                argumentsEnd = firejail.AddAfter(argumentsEnd, $"--whitelist={whitelist}");
            }

            argumentsEnd = firejail.AddAfter(argumentsEnd, "--blacklist=~/.ssh");
            argumentsEnd = firejail.AddAfter(argumentsEnd, "--blacklist=~/.gnupg");
            argumentsEnd = firejail.AddAfter(argumentsEnd, "--blacklist=~/.aws");
            argumentsEnd = firejail.AddAfter(argumentsEnd, "--blacklist=~/.config/browser");

            argumentsEnd = firejail.AddAfter(argumentsEnd, "--dbus-user=none");
            argumentsEnd = firejail.AddAfter(argumentsEnd, "--private-tmp");
        }

        argumentsEnd = firejail.AddAfter(argumentsEnd, "--caps.drop=all");
        argumentsEnd = firejail.AddAfter(argumentsEnd, "--nodvd");
        //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--seccomp"); // breaks slave process

        //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--no-ptrace");
        //argumentsEnd = inp.arguments.AddAfter(argumentsEnd, "--device=/dev/dri");

        inp.arguments[RunnerManager.ArgumentType.Launcher].AddFirst(actualCommand);
        inp.arguments[RunnerManager.ArgumentType.FireJail] = firejail;
    }
}
