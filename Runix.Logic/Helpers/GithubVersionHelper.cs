using System.Diagnostics;
using System.Text.Json;
using GameLibrary.Logic;
using GameLibrary.Logic.Helpers;

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
        const int maxPageSearches = 10;

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

    public static LoadingTask InstallWine(string binaryFolder, string githubName, string version, Func<JsonElement, bool> AssesSelector)
    {
        binaryFolder.CreateDirectoryIfNotExists();

        return new LoadingTask()
        {
            task = new List<(string, Func<Task>)>()
            {
                ( "Downloading", Install ),
                ( "Extracting", Extract ),
            }
        };

        async Task Install()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0");
                client.DefaultRequestHeaders.Add("Accept", "text/json");

                string url = $"https://api.github.com/repos/{githubName}/releases/tags/{version}";
                HttpResponseMessage res = await client.GetAsync(url);

                var json = await res.Content.ReadAsStringAsync();
                JsonDocument doc = JsonDocument.Parse(json);

                JsonElement? asset = doc.RootElement.GetProperty("assets").EnumerateArray().FirstOrDefault(AssesSelector);

                if (asset == null)
                    throw new Exception("Failed to find asset");

                await DownloadFile(asset.Value.GetProperty("browser_download_url").GetString()!, $"{binaryFolder}.tar.xz");
            }
        }

        async Task Extract()
        {
            await DependencyManager.OpenLoadingModal(false, async () => await ExtractFile($"{binaryFolder}.tar.xz", binaryFolder));
            File.Delete($"{binaryFolder}.tar.xz");
        }
    }

    public static async Task DownloadFile(string url, string outputFile)
    {
        try
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
        catch (Exception e)
        {
            throw new Exception($"Failed to download file, {e.Message}");
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
