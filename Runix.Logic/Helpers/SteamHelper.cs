using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using CSharpSqliteORM;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using Logic.db;

namespace Runix.Logic.Helpers;

public static class SteamHelper
{
    public static string GetDefaultSteamLocation() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam");

    public static string GetSteamLocation()
    {
        if (ConfigHandler.configProvider!.TryGetValue(ConfigKeys.Steam_Location, out string val))
        {
            return val;
        }

        return GetDefaultSteamLocation();
    }

    private static HttpClient httpClient
    {
        get
        {
            if (m_httpClient == null)
                m_httpClient = new HttpClient();

            return m_httpClient;
        }
    }
    private static HttpClient? m_httpClient = null;

    public static async Task<SteamData?> GetSteamDataForGame(long id)
    {
        HttpResponseMessage res = await httpClient.GetAsync($"https://store.steampowered.com/api/appdetails?appids={id}");

        if (!res.IsSuccessStatusCode)
            return null;

        Stream json = await res.Content.ReadAsStreamAsync();
        JsonElement doc = (await JsonDocument.ParseAsync(json)).RootElement.EnumerateObject().ElementAt(0).Value;

        return await ParseSteamData(id, doc);
    }

    public static async Task<SteamData> ParseSteamData(long appId, JsonElement doc)
    {
        if (!doc.GetProperty("success").GetBoolean())
            throw new Exception("Request was not successful");

        JsonElement data = doc.GetProperty("data");

        if (data.GetProperty("type").GetString() != "game")
            throw new Exception("Could not find game");

        return new SteamData()
        {
            appId = appId,
            name = data.GetProperty("name").GetString()!,
            iconUrl = data.GetProperty("header_image").GetString()!,
        };
    }

    public static async Task UpdateExistingGame(long appId, Game game)
    {
        SteamData? data = await GetSteamDataForGame(appId);

        if (!data.HasValue)
            return;

        await game.UpdateFromSteamGame(data.Value);
    }

    public static string CreateSteamBridge()
    {
        string path = Path.Combine(DependencyManager.GetUserStorageFolder(), "steambridge.sh");

        if (!File.Exists(path))
            File.Create(path).Dispose();

        if (ConfigHandler.isOnLinux)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupWrite |
                UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherWrite |
                UnixFileMode.OtherExecute
            );
#pragma warning restore CA1416 // Validate platform compatibility
        }

        return path;
    }

    public static async Task<bool> ValidateBridge()
    {
        string path = CreateSteamBridge();
        string[] users = Directory.GetDirectories(Path.Combine(GetSteamLocation(), "userdata"));

        byte terminator = (byte)'\0';
        byte[] exeKey = Encoding.UTF8.GetBytes("Exe\0");
        byte[] idKey = Encoding.UTF8.GetBytes("appid\0");

        foreach (string usr in users)
        {
            Span<byte> data = (await File.ReadAllBytesAsync(Path.Combine(usr, "config", "shortcuts.vdf"))).AsSpan();

            List<string> paths = new List<string>();
            List<ulong> ids = new List<ulong>();

            for (int i = 0; i < data.Length - idKey.Length; i++)
            {
                if (data.Slice(i, exeKey.Length).SequenceEqual(exeKey))
                {
                    int valueStart = i + exeKey.Length;

                    for (int x = valueStart; x < data.Length; x++)
                    {
                        if (data[x] == terminator)
                        {
                            paths.Add(Encoding.UTF8.GetString(data.Slice(valueStart, x - valueStart)));
                            i = x;
                            break;
                        }
                    }
                }
                else if (data.Slice(i, idKey.Length).SequenceEqual(idKey))
                {
                    int valueStart = i + idKey.Length;

                    for (int x = valueStart; x < data.Length; x++)
                    {
                        if (data[x] == terminator)
                        {
                            ulong appId = ((ulong)BitConverter.ToUInt32(data.Slice(valueStart, 4)) << 32) | 0x02000000UL;
                            ids.Add(appId);
                            i = x;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].Equals(path, StringComparison.CurrentCultureIgnoreCase))
                {
                    // not sure if this is actually true. Its assuming that each entry has a single appname and apppath
                    ulong id = ids[i];
                    await ConfigHandler.configProvider!.SaveValue(ConfigKeys.Steam_BridgeShortcutId, id.ToString());

                    return true;
                }
            }
        }

        return false;
    }

    public struct SteamData
    {
        public long appId;
        public string name;
        public string iconUrl;
    }
}