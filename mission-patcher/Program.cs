using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MissionPatcher {
    internal class Program {
        private static bool _argAll;
        private static bool _argConfig;
        private static string _argConfigPath;
        public static bool ArgDelete;
        public static bool ArgForce;

        public static void Main(string[] args) {
            int exitCode = Start(args);
            if (exitCode == 1) {
                Console.Read();
            }
        }

        private static int Start(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("No arguments given");
                Console.WriteLine("MissionPatcher.exe <mission pbo/folder/root path> [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("\t-C=<config path> Get mission file from config. Use missions root folder for main path");
                Console.WriteLine("\t-A Patch all missions in given folder");
                Console.WriteLine("\t-F Force patching. Ignores any ignore flags");
                Console.WriteLine("\t-D Delete mission source if folder");
                return 1;
            }

            foreach (string arg in args.Skip(1)) {
                switch (arg) {
                    case "-A":
                        _argAll = true;
                        break;
                    case "-D":
                        ArgDelete = true;
                        break;
                    case "-F":
                        ArgForce = true;
                        break;
                    default:
                        if (arg.Contains("-C=")) {
                            _argConfig = true;
                            _argConfigPath = arg.Split('=')[1];
                            break;
                        }

                        Console.WriteLine($"Option '{arg}' is not recognized");
                        break;
                }
            }

            Console.WriteLine($"Patching with args '{string.Join(" ", args)}'\n");

            if (_argConfig) {
                if (Directory.Exists(args[0]) && File.Exists(_argConfigPath)) {
                    string path = GetMissionFromConfig();
                    if (!string.IsNullOrEmpty(path)) {
                        Patch(Path.Combine(args[0], $"{path}.pbo"));
                        return 0;
                    }

                    Console.WriteLine($"Could not find mission path in '{_argConfigPath}'");
                    return 1;
                }

                Console.WriteLine($"Either '{args[0]}' is an invalid folder, or '{_argConfigPath}' is an invalid path");
                return 1;
            }

            if (_argAll) {
                int successful = 0;
                List<string> unsuccessful = new List<string>();
                if (Directory.Exists(args[0])) {
                    List<string> files = Directory.EnumerateFiles(args[0], "*", SearchOption.TopDirectoryOnly).Concat(Directory.EnumerateDirectories(args[0], "*", SearchOption.TopDirectoryOnly))
                                                  .ToList();
                    foreach (string path in files) {
                        bool isDirectory = Directory.Exists(path);
                        if (isDirectory) {
                            if (Directory.EnumerateFiles(path, "mission.sqm", SearchOption.TopDirectoryOnly).Any()) {
                                if (Patch(path) == 0) {
                                    successful++;
                                } else {
                                    unsuccessful.Add(path);
                                }
                            }

                            continue;
                        }

                        if (Path.GetExtension(path) != ".pbo") continue;
                        if (Patch(path) == 0) {
                            successful++;
                        } else {
                            unsuccessful.Add(path);
                        }
                    }
                } else {
                    Console.WriteLine($"'{args[0]}' is not a valid directory");
                }

                Console.WriteLine($"\nSuccessfully processed {successful} missions");
                if (!unsuccessful.Any()) return 0;
                Console.WriteLine($"\nThere were {unsuccessful.Count} failed missions");
                unsuccessful.ForEach(Console.WriteLine);
            } else {
                Patch(args[0]);
            }

            return 0;
        }

        private static string GetMissionFromConfig() {
            string line = File.ReadLines(_argConfigPath).FirstOrDefault(x => x.ToLower().Contains("template"));
            return !string.IsNullOrEmpty(line) ? line.Split('=')[1].Trim().Replace("\"", "").Replace(";", "") : string.Empty;
        }

        private static int Patch(string path) {
            Console.WriteLine($"Patching '{path}'");
            Patcher patcher = new Patcher(path);
            try {
                int playable = patcher.Patch();
                if (_argConfig) PatchConfig(playable);
                Console.WriteLine("Patching successful\n");
                return 0;
            } catch (Exception exception) {
                Console.WriteLine("Patching failed");
                Console.WriteLine($"{exception}\n");
                return 1;
            }
        }

        private static void PatchConfig(int playable) {
            if (!File.Exists(_argConfigPath)) return;
            Console.WriteLine("Updating config max players");
            List<string> lines = File.ReadAllLines(_argConfigPath).ToList();
            lines[lines.FindIndex(x => x.Contains("maxPlayers"))] = $"maxPlayers = {playable};";
            File.WriteAllLines(_argConfigPath, lines);
        }
    }
}