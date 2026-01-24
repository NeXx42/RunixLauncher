using System.Diagnostics;
using System.Text.Json;

namespace Runix.Logic.Helpers;

public static class GithubVersionHelper
{
    public static async Task<string[]?> GetRunnerVersions(string githubName)
    {
        return (await GetVersionData(githubName)).OrderByDescending(x => x.id).Select(x => x.json.GetProperty("tag_name").GetString()).ToArray()!;
    }

    public static async Task<(long id, JsonElement json)[]> GetVersionData(string githubName)
    {
        const int pageSize = 25;
        const int maxPageSearches = 1;

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0");
            client.DefaultRequestHeaders.Add("Accept", "text/json");

            List<(long id, JsonElement)> versions = new List<(long id, JsonElement)>();

            for (int currentPage = 0; currentPage < maxPageSearches; currentPage++)
            {
                var res = await SearchPage(client, currentPage);
                currentPage++;

                if (res.Count == 0 || res.Count < pageSize)
                    break;

                versions.AddRange(res);
            }

            return versions.ToArray();
        }

        async Task<List<(long id, JsonElement)>> SearchPage(HttpClient client, int page)
        {
            string url = $"https://api.github.com/repos/{githubName}/releases?per_page={pageSize}&page={page}";
            HttpResponseMessage res = await client.GetAsync(url);

            var json = await res.Content.ReadAsStringAsync();
            JsonDocument doc = JsonDocument.Parse(json);

            List<(long id, JsonElement)> versions = new List<(long id, JsonElement)>();

            foreach (JsonElement el in doc.RootElement.EnumerateArray())
            {
                long version = el.GetProperty("id").GetInt64();
                versions.Add((version, el));
            }

            return versions;
        }
    }

    public static async Task<JsonElement[]> TryFindAssetsFromTag(string githubName, string tagName)
    {
        int? selectedVersion = null;
        (long, JsonElement json)[] releases = await GetVersionData(githubName);

        for (int i = 0; i < releases.Length; i++)
        {
            if (releases[i].json.GetProperty("tag_name").GetString() == tagName)
            {
                selectedVersion = i;
                break;
            }
        }

        if (selectedVersion == null)
            throw new Exception("Couldn't match version with tag");


        return releases[selectedVersion.Value].json.GetProperty("assets").EnumerateArray().ToArray();
    }

    public static async Task InstallWine(string binaryFolder, string githubName, string version)
    {
        JsonElement[] assets = await GithubVersionHelper.TryFindAssetsFromTag(githubName, version);

        if (assets.Length == 0)
        {
            throw new Exception("Failed to find install for tag");
        }

        string url = GetUrlForWine(assets);

        await DownloadFile(url, $"{binaryFolder}.tar.xz");
        await ExtractFile($"{binaryFolder}.tar.xz", binaryFolder);

        File.Delete($"{binaryFolder}.tar.xz");
    }

    public static string GetUrlForWine(JsonElement[] assets)
    {
        foreach (JsonElement asset in assets)
        {
            if (asset.GetProperty("content_type").GetString() == "application/x-xz")
            {
                return asset.GetProperty("browser_download_url").GetString()!;
            }
        }

        throw new Exception("Failed to find asset to download");
    }

    public static async Task DownloadFile(string url, string outputFile)
    {
        if (!File.Exists(outputFile)) // maybe clear
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            res.EnsureSuccessStatusCode();


            using (Stream contentStream = await res.Content.ReadAsStreamAsync())
            using (FileStream filestream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                await contentStream.CopyToAsync(filestream);
            }
        }
    }

    public static async Task ExtractFile(string extract, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-xf {extract} -C {outputFolder}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        Process p = new Process();
        p.StartInfo = startInfo;

        p.Start();
        await p.WaitForExitAsync();
    }
}
