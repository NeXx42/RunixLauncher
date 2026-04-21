using System.Text.Json;
using GameLibrary.Logic.Objects;

namespace Runix.Logic.Helpers;

public static class SteamHelper
{
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

    public struct SteamData
    {
        public long appId;
        public string name;
        public string iconUrl;
    }
}