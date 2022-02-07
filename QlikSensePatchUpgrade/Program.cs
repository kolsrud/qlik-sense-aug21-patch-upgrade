using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace QlikSensePatchUpgrade
{
    class Program
    {
        static void Main(string[] args)
        {
            var printingFolder = ProcessArguments(args);

            var restartRequired = new[]
            {
                Update(printingFolder, "Printing.exe.config"),
                Update(printingFolder, "Qlik.Sense.Printing.dll.config")
            }.Any(x => x);

            if (!restartRequired)
            {
                Console.WriteLine("No config file changes required.");
                return;
            }

            Console.WriteLine("Config file change has been performed. Service restart required.");
            var service = new ServiceController("Qlik Sense Printing Service");
            if (service.CanShutdown)
            {
                Write("Stopping printing service... ");
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
                WriteLine("Done");
            }

            Write("Restarting printing service... ");
            service.Start();
            try
            {
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                WriteLine("Done");
            }
            catch
            {
                WriteLine("Error");
                WriteLine("Service failed to restart after after 10s. Manual interaction required.");
            }
        }

        private static string ProcessArguments(string[] args)
        {
            var programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (args.Any())
            {
                if (args.Length > 2)
                    PrintUsage();

                switch (args[0])
                {
                    case "-h":
                        PrintUsage();
                        break;
                    case "-f":
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Error: No folder path provided.");
                            PrintUsage();
                        }

                        programFilesFolder = args[1];
                        break;
                    default:
                        Console.WriteLine($"Error: Unknown argument \"{args[0]}\".");
                        PrintUsage();
                        break;
                }
            }

            var printingFolder = Path.Combine(programFilesFolder, @"Qlik\Sense\Printing");
            if (!Directory.Exists(printingFolder))
            {
                Console.WriteLine($"Error: Printing folder not found at {printingFolder}. Installation folder must be provided as argument.");
                PrintUsage();
            }

            return printingFolder;
        }

        private static void PrintUsage()
        {
            var exeName = AppDomain.CurrentDomain.FriendlyName;
            Console.WriteLine($"Usage: {exeName} [-h] [-f <folder>]");
            Console.WriteLine("  -h: Print this message and exit.");
            Console.WriteLine("  -f: Path to folder where Qlik Sense is installed. Default is %ProgramFiles%.");
            Environment.Exit(0);
        }

        private static void WriteLine(string msg)
        {
            Write(msg + Environment.NewLine);
        }

        private static void Write(string msg)
        {
            Console.Write(msg);
        }

        private static bool Update(string folder, string file)
        {
            WriteLine("Updating file: " + file);
            var filePath = Path.Combine(folder, file);
            if (!File.Exists(filePath))
            {
                WriteLine("  *** Error - File not found: " + filePath);
                Environment.Exit(1);
            }

            var fileContents = File.ReadAllText(filePath);
            var replacements = new[]
            {
                Tuple.Create(
                    "<bindingRedirect oldVersion=\"0.0.0.0-11.0.0.0\" newVersion=\"11.0.0.0\" />",
                    "<bindingRedirect oldVersion=\"0.0.0.0-12.0.0.0\" newVersion=\"12.0.0.0\" />"
                ),
                Tuple.Create(
                    "<bindingRedirect oldVersion=\"0.0.0.0-34.7.0.0\" newVersion=\"34.7.0.0\" />",
                    "<bindingRedirect oldVersion=\"0.0.0.0-36.6.0.0\" newVersion=\"36.6.0.0\" />"
                ),
                Tuple.Create(
                    "<bindingRedirect oldVersion=\"0.0.0.0-2.0.8.0\" newVersion=\"2.0.8.0\" />",
                    "<bindingRedirect oldVersion=\"0.0.0.0-2.0.12.0\" newVersion=\"2.0.12.0\" />"
                )
            };

            var result = RunReplacements(replacements, fileContents);
            if (result != fileContents)
            {
                MakeBackup(filePath);
                File.WriteAllText(filePath, result);
                WriteLine("File updated: " + filePath);
                return true;
            }
            else
            {
                WriteLine("No file update required: " + filePath);
                return false;
            }
        }

        private static void MakeBackup(string filePath)
        {
            var backupName = GetBackupFileName(filePath);
            File.Copy(filePath, backupName);
            WriteLine("File backup created: " + backupName);
        }

        private static string GetBackupFileName(string filePath)
        {
            var baseBackupFileName = filePath + ".bak";
            if (!File.Exists(baseBackupFileName))
                return baseBackupFileName;

            var n = 1;
            while (true)
            {
                var backupFileName = baseBackupFileName + "." + n;
                if (!File.Exists(backupFileName))
                    return backupFileName;
                n++;
            }
        }

        private static string RunReplacements(Tuple<string, string>[] replacements, string fileContents)
        {
            var result = fileContents;
            foreach (var replacement in replacements)
            {
                var newResult = result.Replace(replacement.Item1, replacement.Item2);
                if (newResult != result)
                {
                    WriteLine("  Replacement performed:");
                    WriteLine("    From: " + replacement.Item1);
                    WriteLine("    To:   " + replacement.Item2);
                    result = newResult;
                }
                else
                {
                    WriteLine("  Already up to date: " + replacement.Item2);
                }
            }

            return result;
        }
    }
}
