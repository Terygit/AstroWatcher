using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace AstroWatcher
{
    internal class Program
    {
        private static String AstroDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Astro", "Saved", "SaveGames");
        private static String AstroCopy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Astro", "Saved Backup");
        private const int KeepCount = 20;
        private static Boolean UseRecycleBin = true;

        private static void Main(string[] args)
        {
            Inform();
            Run();
        }

        private static void Inform()
        {
            Console.WriteLine($"AstroWatcher copies savegame file from .../SaveGames to .../SavedBackup.");
            Console.WriteLine($"It will keep the latest {KeepCount} saves of the current game.");
            Console.WriteLine("Press 'q' to quit the watcher.");
        }

        private static void Run()
        {
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
            var destination = AstroCopy + "/" + s;
            if (UseRecycleBin)
                Console.WriteLine($"Copying {e.Name}");
            if (!File.Exists(destination))
                File.Copy(e.FullPath, destination);
            KeepNewestFiles(prefix);
        }

        private static void KeepNewestFiles(string prefix)
        {
            var files = new DirectoryInfo(AstroCopy)
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