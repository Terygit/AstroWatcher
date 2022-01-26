using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
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

        private static String AppName = Assembly.GetEntryAssembly().GetName().Name;
        private static String AstroDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Astro", "Saved", "SaveGames");
        private static String BackupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Astro", "Saved", Configuration.Options.BackupDirectory);

        private static void Main(string[] args)
        {

            using var mutex = new Mutex(true, AppName + "Singleton", out bool notAlreadyRunning);
            if (notAlreadyRunning)
            {
                ProgramHelpers.HideConsoleAfter(2000);
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
            Console.WriteLine($"AstroWatcher copies savegame file from {AstroDirectory} to {Configuration.Options.BackupDirectory}.");
            Console.WriteLine($"It will keep the latest {Configuration.Options.KeepCount} saves of the current game.");
            Console.WriteLine($"Press 'q' to quit the watcher.");
            Console.WriteLine($"This window will minimize now");
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


                while (Console.ReadKey().KeyChar != 'q') ;
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
            if (Configuration.Options.UseRecycleBin)
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
            for (int i = 0; i < files.Count - Configuration.Options.KeepCount; i++)
            {
                if (Configuration.Options.UseRecycleBin)
                    FileSystem.DeleteFile(files[i].FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                else
                    File.Delete(files[i].FullName);
                Console.WriteLine($"Deleted {files[i].Name}");
            }
        }
    }

    public class Parameters
    {
        public int KeepCount { get; set; }
        public bool UseRecycleBin { get; set; }
        public string BackupDirectory { get; set; }
    }


    public class Configuration
    {
        private IConfigurationRoot configuration;
        public static Parameters Options { get => _options; }
        private static Parameters _options = new Parameters();

        private static Configuration Instance { get; set; }
        static Configuration()
        {
            Instance = new Configuration();
        }
        private Configuration()
        {
            configuration = new ConfigurationBuilder()
               .AddJsonFile("config.json", optional: false, reloadOnChange: true)
               .Build();

            configuration
                .GetSection(nameof(Options))
                .Bind(_options);

            ChangeToken.OnChange(() => configuration.GetReloadToken(), onChangeCallback);
        }
        private void onChangeCallback()
        {
            _options = configuration.GetSection(nameof(Options)).Get<Parameters>();
        }

    }
}