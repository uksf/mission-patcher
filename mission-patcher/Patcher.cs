using System;
using System.Diagnostics;
using System.IO;
using MissionPatcher.Data;

namespace MissionPatcher {
    public class Patcher {
        private const string BACKUPS = "_BACKUPS";
        private const string EXTRACT_PBO = "C:\\Program Files (x86)\\Mikero\\DePboTools\\bin\\ExtractPboDos.exe";
        private const string MAKE_PBO = "C:\\Program Files (x86)\\Mikero\\DePboTools\\bin\\MakePbo.exe";

        private static Lobby _lobby;

        private string _filePath;
        private string _folderPath;
        private bool _isDirectory;

        public Patcher(string path) => _filePath = path;

        public int Patch() {
            if (Directory.Exists(_filePath)) {
                _folderPath = _filePath;
                _isDirectory = true;
            } else if (!File.Exists(_filePath)) {
                throw new Exception($"Path '{_filePath}' is not valid");
            }

            if (!CreateBackup()) throw new Exception();
            if (!_isDirectory && Path.GetExtension(_filePath) == ".pbo") {
                Console.WriteLine($"Unpacking '{_filePath}'");
                if (!UnpackPbo()) throw new Exception();
            }

            Mission.Mission mission = new Mission.Mission(_folderPath);
            int playable = mission.Read();
            if (playable != 0) {
                Console.WriteLine("Patching stopped as mission is ignored\n");
                return playable;
            }

            if (_lobby == null) {
                Console.WriteLine("Mision read, fetching data from api");
                _lobby = new Lobby();
                int exitCode = _lobby.Setup();
                switch (exitCode) {
                    case 1:
                        _lobby = null;
                        Console.WriteLine("Failed to fetch data from api");
                        throw new Exception();
                    default:
                        Console.WriteLine("Fetched data from api");
                        break;
                }
            } else {
                Console.WriteLine("Mision read, using cached api data");
            }

            Console.WriteLine("Patching mission");
            mission.Patch(_lobby);

            Console.WriteLine("Writing mission");
            playable = mission.Write();

            Console.WriteLine("Packing new pbo");
            if (!PackPbo()) throw new Exception();

            if (!Program.ArgDelete) return playable;
            Console.WriteLine("Deleting source mission");
            Cleanup();

            return playable;
        }

        private bool UnpackPbo() {
            try {
                _folderPath = Path.Combine(Path.GetDirectoryName(_filePath) ?? throw new DirectoryNotFoundException(),
                                           Path.GetFileNameWithoutExtension(_filePath) ?? throw new FileNotFoundException());
                Process process = new Process {StartInfo = {FileName = EXTRACT_PBO, Arguments = $"-D -P \"{_filePath}\"", UseShellExecute = false, CreateNoWindow = true}};
                process.Start();
                process.WaitForExit();

                Console.WriteLine($"Unpacked to '{_folderPath}'");

                if (Directory.Exists(_folderPath)) return true;
                Console.WriteLine("Could not find unpacked folder");
                return false;
            } catch (Exception exception) {
                Console.WriteLine($"Failed to unpack '{_filePath}'. Will not proceed.\n{exception.Message}");
                return false;
            }
        }

        private bool PackPbo() {
            try {
                if (Directory.Exists(_filePath)) {
                    _filePath += ".pbo";
                }

                Process process = new Process {
                    StartInfo = {
                        FileName = MAKE_PBO,//-P 
                        Arguments = $"-Z -BD -X=thumbs.db,*.txt,*.h,*.dep,*.cpp,*.bak,*.png,*.log,*.pew \"{_folderPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = false
                    }
                };
                process.Start();
                process.WaitForExit();

                Console.WriteLine($"Packed to '{_filePath}'");

                if (File.Exists(_filePath)) return true;
                Console.WriteLine("Could not find packed file");
                return false;
            } catch (Exception exception) {
                Console.WriteLine($"Failed to pack '{_filePath}'. Will not proceed.\n{exception}");
                return false;
            }
        }

        private bool CreateBackup() {
            try {
                string backupPath = Path.Combine(Path.GetDirectoryName(_filePath) ?? throw new DirectoryNotFoundException(), BACKUPS, Path.GetFileName(_filePath) ?? throw new FileNotFoundException());
                if (_isDirectory) {
                    if (Directory.Exists(backupPath)) {
                        return true;
                    }

                    Directory.CreateDirectory(backupPath);
                    CopyDirectory(_filePath, backupPath);
                    if (!Directory.Exists(backupPath)) {
                        throw new DirectoryNotFoundException();
                    }
                } else {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? throw new DirectoryNotFoundException());
                    if (File.Exists(backupPath)) {
                        return true;
                    }

                    File.Copy(_filePath, backupPath, true);
                    if (!File.Exists(backupPath)) {
                        throw new FileNotFoundException();
                    }
                }

                return true;
            } catch (Exception exception) {
                Console.WriteLine($"Failed to create backup for '{_filePath}'. Will not proceed.\n{exception}");
                return false;
            }
        }

        private void Cleanup() {
            Directory.Delete(_folderPath, true);
        }

        private static void CopyDirectory(string sourcePath, string destinationPath) {
            foreach (string directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)) {
                Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));
            }

            foreach (string file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)) {
                File.Copy(file, file.Replace(sourcePath, destinationPath), true);
            }
        }
    }
}