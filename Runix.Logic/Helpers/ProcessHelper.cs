using System.Collections;
using System.Diagnostics;

namespace Runix.Logic.Helpers;

public static class ProcessHelper
{
    public static Process Create(ProcessStartInfo info, params string[] args)
    {
        foreach (string arg in args)
            info.ArgumentList.Add(arg);

        return new Process()
        {
            StartInfo = info.UpdateProcessStartInfoForEnv()
        };
    }

    public static ProcessStartInfo UpdateProcessStartInfoForEnv(this ProcessStartInfo info)
    {
        if (File.Exists("/.flatpak-info"))
        {
            string[] args = [info.FileName, .. info.ArgumentList];

            info.FileName = "flatpak-spawn";

            info.ArgumentList.Clear();
            info.ArgumentList.Add("--host");

            if (info.EnvironmentVariables.Count > 0)
            {
                info.ArgumentList.Add("env");

                foreach (var ev in info.Environment)
                    info.ArgumentList.Add($"{ev.Key}={ev.Value}");
            }


            foreach (string arg in args)
                info.ArgumentList.Add(arg);
        }

        return info;
    }

}
