using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Windows;
using CSharpSqliteORM;
using GameLibrary.DB;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace GameLibrary.Logic
{
    public static class FileManager
    {
        private static SemaphoreSlim _concurrentUnzipSlim = new SemaphoreSlim(3);

        public static async Task DeleteGameFiles(GameDto game)
        {
            if (Directory.Exists(game.getAbsoluteFolderLocation))
                Directory.Delete(game.getAbsoluteFolderLocation, true);
        }

        public static async Task UpdateGameIcon(int gameId, Uri newIconPath)
        {
            GameDto? game = LibraryManager.TryGetCachedGame(gameId);

            if (!File.Exists(newIconPath.LocalPath) || game == null)
            {
                return;
            }

            string localPath = $"{Guid.NewGuid()}.png";
            File.Copy(newIconPath.LocalPath, Path.Combine(game.getAbsoluteFolderLocation, localPath));

            await game.UpdateGameIcon(localPath);
        }

        public static string CreateEmptyGameFolder(string absoluteBinaryFile)
        {
            string folderName = Path.Combine(Path.GetDirectoryName(absoluteBinaryFile)!, Path.GetFileNameWithoutExtension(absoluteBinaryFile).Replace(" ", string.Empty));

            Directory.CreateDirectory(folderName);
            Directory.Move(absoluteBinaryFile, Path.Combine(folderName, Path.GetFileName(absoluteBinaryFile)));

            return folderName;
        }

        public static async Task<bool> MoveGameToItsLibrary(dbo_Game game, string binaryAbsolutePath, string libraryRootLocation)
        {
            string destination = Path.Combine();
            string existingFolderPath = Path.GetDirectoryName(binaryAbsolutePath)!;

            if (Directory.Exists(destination))
            {
                if (await DependencyManager.OpenYesNoModal("Delete?", $"A directory already exists at\n'{destination}'\n\nDo you want to delete it?"))
                {
                    Directory.Delete(destination, true);
                }
                else
                {
                    return false;
                }
            }

            return await MoveFolder(existingFolderPath, libraryRootLocation, game.gameFolder);
        }

        public static async Task<bool> MoveFolder(string absoluteFolder, string newAbsoluteParent, string? newFolderName = "")
        {
            newFolderName = string.IsNullOrEmpty(newFolderName) ? Path.GetFileName(absoluteFolder)! : newFolderName;
            string fullTarget = Path.Combine(newAbsoluteParent, newFolderName);

            try
            {
                Directory.Move(absoluteFolder, fullTarget);
            }
            catch (IOException ex) when (ex.Message.Contains("Invalid cross-device link"))
            {
                await CopyFiles(absoluteFolder, fullTarget);
            }

            return true;
        }

        private static Task CopyFiles(string existing, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(existing))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(existing))
            {
                var destSubDir = Path.Combine(destination, Path.GetFileName(directory));
                CopyFiles(directory, destSubDir);
            }

            Directory.Delete(existing, recursive: true);
            return Task.CompletedTask;
        }


        public static bool IsZip(string path)
        {
            string extension = Path.GetExtension(path);

            return extension.Equals(".7z", StringComparison.InvariantCultureIgnoreCase) ||
                extension.Equals(".rar", StringComparison.InvariantCultureIgnoreCase) ||
                extension.Equals(".zip", StringComparison.InvariantCultureIgnoreCase) ||
                extension.Equals(".bin", StringComparison.InvariantCultureIgnoreCase) ||
                extension.Equals(".iso", StringComparison.InvariantCultureIgnoreCase);
        }


        public static async Task<string?> RequestExtract(string archivePath, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (!File.Exists(archivePath) || !IsZip(archivePath))
            {
                throw new Exception($"Invalid archive - {archivePath}");
            }

            ReaderOptions readerOptions = new ReaderOptions();

            if (IsArchiveEncrypted(archivePath))
            {
                string? lastPassword = ConfigHandler.configProvider?.GetValue(Enums.ConfigKeys.Misc_LastArchivePassword);
                readerOptions.Password = await DependencyManager.OpenStringInputModal("Archive Password", lastPassword) ?? string.Empty;

                if (lastPassword != readerOptions.Password)
                    await (ConfigHandler.configProvider?.SaveValue(Enums.ConfigKeys.Misc_LastArchivePassword, readerOptions.Password) ?? Task.CompletedTask);
            }

            await _concurrentUnzipSlim.WaitAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return null;

            try
            {
                string extractName = Path.GetFileNameWithoutExtension(archivePath)!;
                string dir = Path.GetDirectoryName(archivePath)!;

                string extractedFolderName = Path.Combine(dir, extractName).CreateDirectoryIfNotExists();
                string extension = Path.GetExtension(archivePath);

                using var archive = ArchiveFactory.Open(archivePath, readerOptions);

                switch (extension)
                {
                    case ".rar": return await ExtractFile_Rar(archivePath, readerOptions.Password, extractedFolderName, progress, cancellationToken);
                    default: return await ExtractFile_Generic(archivePath, readerOptions.Password, extractedFolderName, progress, cancellationToken);
                }
            }
            catch (CryptographicException)
            {
                throw new Exception("Invalid Password");
            }
            catch (InvalidDataException)
            {
                throw new Exception("Invalid Password");
            }
            finally
            {
                _concurrentUnzipSlim.Release();
            }
        }

        private static bool IsArchiveEncrypted(string path)
        {
            try
            {
                using (var archive = ArchiveFactory.Open(path))
                {
                    return archive.Entries.Any(x => !x.IsDirectory && x.IsEncrypted);
                }
            }
            catch (CryptographicException)
            {
                return true;
            }
        }

        private static async Task<string?> ExtractFile_Rar(string archivePath, string? password, string extractFolderName, IProgress<int> progress, CancellationToken cancellationToken)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "unrar";

            info.RedirectStandardError = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            info.ArgumentList.Add("x");
            info.ArgumentList.Add("-o+");

            if (!string.IsNullOrEmpty(password))
                info.ArgumentList.Add($"-p{password}");

            info.ArgumentList.Add(archivePath);
            info.ArgumentList.Add(extractFolderName);

            Process p = new Process();
            p.StartInfo = info;

            p.Start();
            await ReadProgressOfExtraction(p, progress, cancellationToken);

            if (p.ExitCode != 0)
            {
                throw new Exception(await p.StandardError.ReadToEndAsync());
            }

            return extractFolderName;
        }

        private static async Task<string?> ExtractFile_Generic(string archivePath, string? password, string extractFolderName, IProgress<int> progress, CancellationToken cancellationToken)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "7z";

            info.RedirectStandardError = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            info.ArgumentList.Add("x");
            info.ArgumentList.Add(archivePath);

            info.ArgumentList.Add($"-o{extractFolderName}");

            if (!string.IsNullOrEmpty(password))
                info.ArgumentList.Add($"-p{password}");

            info.ArgumentList.Add("-y");
            info.ArgumentList.Add("-bsp1"); // for when i can figure out capturing percentages

            Process p = new Process();
            p.StartInfo = info;

            p.Start();
            await ReadProgressOfExtraction(p, progress, cancellationToken);

            if (p.ExitCode != 0)
            {
                throw new Exception(await p.StandardError.ReadToEndAsync());
            }

            return extractFolderName;
        }

        private static Task ReadProgressOfExtraction(Process p, IProgress<int> progress, CancellationToken cancellationToken)
        {
            int charNumber;
            const int newLineCharNumber = '\b';

            string line = string.Empty;

            TaskCompletionSource task = new TaskCompletionSource();

            Task.Run(() =>
            {
                while (!p.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        p.Kill();
                        return;
                    }

                    while ((charNumber = p.StandardOutput.Read()) != -1)
                    {
                        if (charNumber == newLineCharNumber)
                        {
                            string percentageText = line.Replace(" ", "");
                            Match match = Regex.Match(percentageText, @"^(\d+)%");

                            if (match.Success)
                            {
                                int percentage = int.Parse(match.Groups[1].Value);
                                progress.Report(percentage);
                            }

                            line = string.Empty;
                        }
                        else
                        {
                            line += (char)charNumber;
                        }
                    }
                }

                task.SetResult();
            });

            return task.Task;
        }

        public interface IImportEntry
        {
            public string getPotentialName { get; }
            public string getBinaryPath { get; }

            public string? getBinaryFolder { get; }
        }


        public class ImportEntry_Archive : IImportEntry
        {
            public readonly string archivePath;

            public string? extractedFolder;
            public string? selectedBinary;

            public string getPotentialName => Path.GetFileName(getBinaryFolder)!;
            public string getBinaryPath => selectedBinary!;

            public string? getBinaryFolder => Path.GetDirectoryName(getBinaryPath);

            public ImportEntry_Archive(string archive)
            {
                archivePath = archive;
                string potentialExtracted = archivePath.Substring(0, archivePath.Length - Path.GetExtension(archive).Length);

                if (Directory.Exists(potentialExtracted))
                {
                    extractedFolder = potentialExtracted;
                }
            }
        }


        public class ImportEntry_Binary : IImportEntry
        {
            public string binaryLocation;

            public string getPotentialName => Path.GetFileNameWithoutExtension(binaryLocation).Replace(" ", string.Empty);
            public string getBinaryPath => binaryLocation;
            public string? getBinaryFolder => null;

            public ImportEntry_Binary(string loc) => binaryLocation = loc;

        }

        public class ImportEntry_Folder : IImportEntry
        {
            public string folderPath;

            public int selectedBinary;
            public string[] binaries;

            public string getPotentialName => Path.GetFileName(folderPath);

            public string getBinaryPath => Path.Combine(folderPath, binaries[selectedBinary]);
            public string? getBinaryFolder => folderPath;

            public ImportEntry_Folder(string folder)
            {
                folderPath = folder.EndsWith("/") ? folder.Substring(0, folder.Length - 1) : folder;
                binaries = Directory.GetFiles(folder).Where(RunnerManager.IsUniversallyAcceptedExecutableFormat).Select(x => Path.GetFileName(x)).ToArray();
            }
        }
    }
}
