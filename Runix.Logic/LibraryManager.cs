using System.Data.SQLite;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;

namespace GameLibrary.Logic
{
    public static class LibraryManager
    {
        public enum ExternalLibraryTypes
        {
            Steam
        }
        public static Action? onGameDeletion;
        public static Action<int>? onGameDetailsUpdate;

        private static int filteredGameCount;

        private static Dictionary<int, Game?> cachedGames = new Dictionary<int, Game?>();
        private static Dictionary<int, LibraryDto> cachedLibraries = new Dictionary<int, LibraryDto>();


        public static async Task Setup()
        {
            await FindLibraries();
        }

        public static async Task FindLibraries()
        {
            dbo_Libraries[] libraries = await Database_Manager.GetItems<dbo_Libraries>();
            cachedLibraries = libraries.ToDictionary(x => x.libaryId, x => new LibraryDto(x));
        }


        public static async Task<List<string>> ImportGames(Dictionary<string, FileManager.IImportEntry?> availableImports, int? libraryId)
        {
            bool useGuidFolderNames = ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Import_GUIDFolderNames, true);
            List<string> successfulGames = new List<string>();

            await DependencyManager.OpenLoadingModal(true, GetTasks());

            onGameDeletion?.Invoke();
            return successfulGames;

            Func<Task>[] GetTasks()
            {
                List<Func<Task>> updates = new List<Func<Task>>();

                foreach (KeyValuePair<string, FileManager.IImportEntry?> importEntry in availableImports)
                {
                    if (importEntry.Value == null)
                        continue;

                    FileManager.IImportEntry folder = importEntry.Value;

                    if (string.IsNullOrEmpty(folder.getBinaryPath))
                        continue;

                    updates.Add(async () =>
                    {
                        string gameName = folder.getPotentialName;
                        string absoluteFolder;

                        if (folder is FileManager.ImportEntry_Binary binaryImport)
                        {
                            absoluteFolder = FileManager.CreateEmptyGameFolder(binaryImport.binaryLocation);
                            binaryImport.binaryLocation = Path.Combine(absoluteFolder, Path.GetFileName(binaryImport.binaryLocation));
                        }
                        else
                        {
                            absoluteFolder = folder.getBinaryFolder!;
                        }

                        dbo_Game newGame = new dbo_Game
                        {
                            gameName = gameName,
                            gameFolder = absoluteFolder,
                            executablePath = Path.GetFileName(folder.getBinaryPath),
                            libraryId = libraryId,
                            status = (int)Game_Status.Active
                        };


                        if (libraryId.HasValue)
                        {
                            newGame.gameFolder = useGuidFolderNames ? Guid.NewGuid().ToString() : gameName;
                            dbo_Libraries library = (await Database_Manager.GetItem<dbo_Libraries>(SQLFilter.Equal(nameof(dbo_Libraries.libaryId), libraryId.Value)))!;

                            if (!await FileManager.MoveGameToItsLibrary(newGame, folder.getBinaryPath, library.rootPath))
                                return;
                        }

                        await Database_Manager.InsertItem(newGame);
                        successfulGames.Add(importEntry.Key);
                    });
                }

                return updates.ToArray();
            }
        }

        public static int GetMaxPages(int limit) => (int)Math.Ceiling(filteredGameCount / (float)limit) - 1;

        public static async Task<int[]> GetGameList(GameFilterRequest filterRequest, CancellationToken cancellationToken)
        {
            string sql = filterRequest.ConstructSQL();
            sql += $" LIMIT {filterRequest.take} OFFSET {filterRequest.take * filterRequest.page};";

            int? totalCount = null;
            int[] res = await Database_Manager.GetItemsGeneric(sql, DeserializeDatabaseRequest, cancellationToken);

            filteredGameCount = totalCount ?? 0;
            return res;

            async Task<int> DeserializeDatabaseRequest(SQLiteDataReader reader)
            {
                totalCount ??= Convert.ToInt32(reader["total_count"]);
                return Convert.ToInt32(reader["id"]);
            }
        }

        public static async Task<Game?> GetGame(int? gameId, CancellationToken cancellationToken)
        {
            if (gameId.HasValue)
                return await GetGame(gameId.Value, cancellationToken);

            return null;
        }

        public static async Task<Game?> GetGame(int gameId, CancellationToken cancellationToken)
        {
            if (cachedGames.TryGetValue(gameId, out Game? game))
                return game;

            try
            {

                dbo_Game? dboGame = await Database_Manager.GetItem<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), gameId), cancellationToken);
                if (dboGame == null || cancellationToken.IsCancellationRequested) return null;

                dbo_GameTag[] dboTags = await Database_Manager.GetItems<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), gameId), cancellationToken);
                if (cancellationToken.IsCancellationRequested) return null;

                dbo_GameConfig[] dboConfig = await Database_Manager.GetItems<dbo_GameConfig>(SQLFilter.Equal(nameof(dbo_GameConfig.gameId), gameId), cancellationToken);
                if (cancellationToken.IsCancellationRequested) return null;

                if (dboGame.libraryId.HasValue && cachedLibraries.TryGetValue(dboGame.libraryId.Value, out LibraryDto? lib) && lib != null)
                {
                    switch (lib.externalType)
                    {
                        case Library_ExternalProviders.Steam:
                            game = new Game_Steam(dboGame, dboTags, dboConfig);
                            break;
                    }
                }

                game ??= new Game_Custom(dboGame, dboTags, dboConfig);
                cachedGames[gameId] = game;

                return game;
            }
            catch (Exception e)
            {
                await DependencyManager.OpenExceptionDialog("Failed to load game", e);
                return null;
            }
        }

        public static async Task DeleteGame(Game game, bool removeFiles)
        {
            if (removeFiles)
            {
                try
                {
                    await FileManager.DeleteGameFiles(game);
                }
                catch (Exception e)
                {
                    string paragraph = $"Failed to delete games files!\n\n{e.Message}\n\nDo you want to delete the record anyway?";

                    if (!await DependencyManager.OpenYesNoModal("Delete record?", paragraph))
                        return;
                }
            }

            await Database_Manager.Delete<dbo_GameConfig>(SQLFilter.Equal(nameof(dbo_GameConfig.gameId), game.gameId));
            await Database_Manager.Delete<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), game.gameId));
            await Database_Manager.Delete<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), game.gameId));
            cachedGames.Remove(game.gameId);

            onGameDeletion?.Invoke();
        }


        public static async Task GenerateLibrary(string path)
        {
            await Database_Manager.InsertItem(new dbo_Libraries()
            {
                rootPath = path
            });

            await FindLibraries();
        }

        public static async Task CreateTag(string tagName)
        {
            await Database_Manager.InsertItem(new dbo_Tag()
            {
                TagName = tagName,
            });
        }

        public static string GetLibraryRoute(int? libraryId) => libraryId == null ? string.Empty : cachedLibraries[libraryId.Value].root;
        public static LibraryDto[] GetLibraries() => cachedLibraries.Values.Where(x => !x.externalType.HasValue).ToArray();

        public static void InvokeGameDetailsUpdate(int gameId)
        {
            DependencyManager.InvokeOnUIThread(() => onGameDetailsUpdate?.Invoke(gameId));
        }

        public static async Task InsertGames(dbo_Game[] games)
        {
            await Database_Manager.InsertItem(games);
            onGameDeletion?.Invoke();
        }

        public static async Task ClearRunner(int runnerId)
        {
            // really should do in a single db call, but its not too big of an issue

            foreach (Game? game in cachedGames.Values)
            {
                if (game?.runnerId == runnerId)
                {
                    await game.ChangeRunnerId(null);
                }
            }
        }

        public static async Task<Game[]> GetGamesPerLibrary(int libraryId, CancellationToken token)
        {
            string paramName = Database_Manager.GetGenericParameterName();
            string sql = $"SELECT {nameof(dbo_Game.id)} FROM {dbo_Game.tableName} WHERE {nameof(dbo_Game.libraryId)} = @{paramName}";

            int[] games = await Database_Manager.ExecuteSQLQuery(sql, Deserializer, token, new SQLiteParameter(paramName, libraryId));
            List<Game> relevantGames = new List<Game>();

            foreach (int gameId in games)
            {
                Game? game = await GetGame(gameId, token);

                if (game == null)
                    continue;

                relevantGames.Add(game);
            }

            return relevantGames.ToArray();

            async Task<int> Deserializer(SQLiteDataReader reader)
            {
                return (int)(long)reader[nameof(dbo_Game.id)];
            }
        }
    }
}
