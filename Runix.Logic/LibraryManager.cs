using System.Data.SQLite;
using System.Net.NetworkInformation;
using System.Text;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Objects;
using Logic.db;
using Runix.Structure.DTOs;
using Runix.Structure.Interfaces.Repositories;

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

            foreach (KeyValuePair<string, FileManager.IImportEntry?> importEntry in availableImports)
            {
                if (importEntry.Value == null)
                    continue;

                FileManager.IImportEntry folder = importEntry.Value;

                if (string.IsNullOrEmpty(folder.getBinaryPath))
                    continue;

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
                        continue;
                }

                await Database_Manager.InsertItem(newGame);
                successfulGames.Add(importEntry.Key);
            }

            onGameDeletion?.Invoke();
            return successfulGames;
        }

        public static int GetMaxPages(int limit) => (int)Math.Ceiling(filteredGameCount / (float)limit) - 1;

        public static async Task<int[]> GetGameList(GameFilterRequest filterRequest, CancellationToken cancellationToken)
        {
            (int[] res, filteredGameCount) = await DependencyManager.gameRepo!.GameGameList(filterRequest.ConstructSQL(), filterRequest.page, filterRequest.take, cancellationToken);
            return res;
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
                GameDTO? obj = await DependencyManager.gameRepo!.GetGame(gameId, cancellationToken);

                if (obj == null)
                {
                    cachedGames[gameId] = null;
                    return null;
                }

                if (obj.libraryId.HasValue && cachedLibraries.TryGetValue(obj.libraryId.Value, out LibraryDto? lib) && lib != null)
                {
                    switch (lib.externalType)
                    {
                        case Library_ExternalProviders.Steam:
                            game = new Game_Steam(obj);
                            break;
                    }
                }

                game ??= new Game_Custom(obj);
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

            await DependencyManager.gameRepo!.DeleteGame(game.gameId, CancellationToken.None);
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

            foreach (Game game in cachedGames.Values)
            {
                if (game.runnerId == runnerId)
                {
                    await game.ChangeRunnerId(null);
                }
            }
        }

        public static async Task UpdateGame_Name(Game game, string to)
        {
            game.gameName = to;
            await DependencyManager.gameRepo!.SaveGame(game.GetDTO(), CancellationToken.None);

            onGameDetailsUpdate?.Invoke(game.gameId);
        }

        public static async Task<Game[]> GetGamesPerLibrary(int libraryId, CancellationToken token)
        {
            int[] games = await DependencyManager.libraryRepo!.GetLinkedGameNames(libraryId, token);
            List<Game> relevantGames = new List<Game>();

            foreach (int gameId in games)
            {
                Game? game = await GetGame(gameId, token);

                if (game == null)
                    continue;

                relevantGames.Add(game);
            }

            return relevantGames.ToArray();
        }
    }
}
