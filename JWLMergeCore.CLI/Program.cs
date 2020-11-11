namespace JWLMergeCore.CLI
{
    using System.IO;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public static class Program
    {
        /// <summary>
        /// The main entry point.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static async Task Main(string[]? args)
        {
            try
            {
                //Log.Logger.Information("Started");

                if (args is null || args.Length < 2)
                {
                    ShowUsage();
                    args = GetLocations();
                }

                var app = new MainApp();
                app.ProgressEvent += AppProgress;
                await app.Run(args);


                Environment.ExitCode = 0;
                //Log.Logger.Information("Finished");
            }
            catch (Exception ex)
            {
                //Log.Logger.Error(ex, "Error");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Environment.ExitCode = 1;
            }
        }

        private static string GetVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private static void ShowUsage()
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine($" JWLMerge version {GetVersion()} ");
            Console.WriteLine();
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("   Description:");
            Console.ResetColor();
            Console.WriteLine("    JWLMergeCLI is used to merge the contents of 2 or more jwlibrary backup");
            Console.WriteLine("    files. These files are produced by the JW Library backup command and");
            Console.WriteLine("    contain your personal study notes and highlighting.");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("   Usage:");
            Console.ResetColor();
            Console.WriteLine("    JWLMergeCLI <jwlibrary file 1> <jwlibrary file 2>...");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("   An example:");
            Console.ResetColor();
            Console.WriteLine("    JWLMergeCLI \"C:\\Backup_PC16.jwlibrary\" \"C:\\Backup_iPad.jwlibrary\"");
            Console.WriteLine();
        }

        private static string[] GetLocations()
        {
            var locations = new string[2];
            for (var i = 0; i < locations.Length; i++)
            {
                do
                {
                    Console.WriteLine($"Specify path to file number {i + 1}");
                    locations[i] = Console.ReadLine();
                    if (!File.Exists(locations[i]))
                    {
                        Console.WriteLine("Could not find file! Try again!");
                    }
                    else if (!locations[i].EndsWith(".jwlibrary"))
                    {
                        Console.WriteLine("Invalid file format");
                    }
                } while (!File.Exists(locations[i]) || !locations[i].EndsWith(".jwlibrary"));
            }

            return locations;
        }

        private static void AppProgress(object? sender, BackupFileServices.Events.ProgressEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
    }
}