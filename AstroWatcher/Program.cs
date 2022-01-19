using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AstroWatcher
{
    internal static class Program
    {
        private static String AstroDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Astro", "Saved", "SaveGames");
        private static String BackupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Astro", "Saved", "SaveGamesBackup");
        private const int KeepCount = 20;
        private static bool UseRecycleBin = true;
        private static String AppName = Assembly.GetEntryAssembly().GetName().Name;
        private static void Main(string[] args)
        {
            using var mutex = new Mutex(true, AppName + "Singleton", out bool notAlreadyRunning);
            if (notAlreadyRunning)
            {
                Inform();
                Run();
            }
            else
            {
                Environment.Exit(-1);
            }
        }

        private static void Inform()
        {
            Console.WriteLine($"AstroWatcher copies savegame file from {AstroDirectory} to SaveGamesBackup.");
            Console.WriteLine($"It will keep the latest {KeepCount} saves of the current game.");
            Console.WriteLine("Press 'q' to quit the watcher.");
        }

        private static void Run()
        {
            Directory.CreateDirectory(BackupDirectory);

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = AstroDirectory;

                watcher.NotifyFilter = NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;

                watcher.Filter = "*.savegame";

                watcher.Created += OnChanged;


                watcher.EnableRaisingEvents = true;


                while (Console.Read() != 'q') ;
            }
        }

        private static (string, string) Split(string fileName)
        {

            var a = fileName.Split('$');
            return (a[0], a[1]);
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Thread.Sleep(500);
            (var prefix, var datetime) = Split(Path.GetFileName(e.FullPath));
            var s = Path.GetFileName(e.FullPath);
            var destination = BackupDirectory + "/" + s;
            if (UseRecycleBin)
                Console.WriteLine($"Copying {e.Name}");
            if (!File.Exists(destination))
                File.Copy(e.FullPath, destination);
            KeepNewestFiles(prefix);
        }

        private static void KeepNewestFiles(string prefix)
        {
            var files = new DirectoryInfo(BackupDirectory)
                .GetFiles($"{prefix}*.savegame")
                .OrderBy(f => f.LastWriteTime)
                .ToList();
            for (int i = 0; i < files.Count - KeepCount; i++)
            {
                if (UseRecycleBin)
                    FileSystem.DeleteFile(files[i].FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                else
                    File.Delete(files[i].FullName);
                Console.WriteLine($"Deleted {files[i].Name}");
            }
        }
    }
}