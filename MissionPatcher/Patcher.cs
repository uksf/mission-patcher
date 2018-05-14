using System;
using System.Diagnostics;
using System.IO;
using MissionPatcher.Data;

namespace MissionPatcher {
    public class Patcher {
        private const string EXTRACT_PBO = "C:\\Program Files (x86)\\Mikero\\DePboTools\\bin\\ExtractPboDos.exe";
        private const string MAKE_PBO = "C:\\Program Files (x86)\\Mikero\\DePboTools\\bin\\MakePbo.exe";

        private string _filePath;
        private string _folderPath;
        private bool _isDirectory;

        public Patcher(string filePath) => _filePath = filePath;

        public int Patch() {
            if (Directory.Exists(_filePath)) {
                _folderPath = _filePath;
                _isDirectory = true;
            } else if (!File.Exists(_filePath)) {
                Console.WriteLine($"Path '{_filePath}' does not exist");
                return 1;
            }

            if (!CreateBackup()) return 1;
            if (!_isDirectory && Path.GetExtension(_filePath) == ".pbo") {
                if (!UnpackPbo()) return 1;
            }

            Mission.Mission mission = new Mission.Mission(_folderPath);
            if (!mission.Read()) {
                return 2;
            }

            Lobby lobby = new Lobby();
            int exitCode = lobby.Setup();
            switch (exitCode) {
                case 1:
                    Console.WriteLine("Failed to retrieve data from api");
                    break;
                default:
                    Console.WriteLine("Retrieved data from api");
                    break;
            }

            mission.Patch(lobby);
            mission.Write();

            if (!PackPbo()) return 1;
            Cleanup();

            return 0;
        }

        private bool UnpackPbo() {
            try {
                _folderPath = Path.Combine(Path.GetDirectoryName(_filePath) ?? throw new DirectoryNotFoundException(),
                                           Path.GetFileNameWithoutExtension(_filePath) ?? throw new FileNotFoundException());
                Process process = new Process {StartInfo = {FileName = EXTRACT_PBO, Arguments = $"-D -P {_filePath}", UseShellExecute = false, CreateNoWindow = true}};
                process.Start();
                process.WaitForExit();

                Console.WriteLine($"Unpacked to '{_folderPath}'");

                if (Directory.Exists(_folderPath)) return true;
                Console.WriteLine("Could not find unpacked folder");
                return false;
            } catch (Exception exception) {
                Console.WriteLine($"Failed to unpack '{_filePath}'. Will not proceed.\n\t{exception.Message}");
                return false;
            }
        }

        private bool PackPbo() {
            try {
                if (Directory.Exists(_filePath)) {
                    _filePath += ".pbo";
                }

                Process process = new Process {StartInfo = {FileName = MAKE_PBO, Arguments = $"-Z -BD -P {_folderPath}", UseShellExecute = false, CreateNoWindow = true}};
                process.Start();
                process.WaitForExit();

                Console.WriteLine($"Packed to '{_filePath}'");

                if (File.Exists(_filePath)) return true;
                Console.WriteLine("Could not find packed file");
                return false;
            } catch (Exception exception) {
                Console.WriteLine($"Failed to pack '{_filePath}'. Will not proceed.\n\t{exception.Message}");
                return false;
            }
        }

        private bool CreateBackup() {
            try {
                string backupPath = Path.Combine(Path.GetDirectoryName(_filePath) ?? throw new DirectoryNotFoundException(), "BACKUPS", $"{Path.GetFileName(_filePath)}.backup");
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
                Console.WriteLine($"Failed to create backup for '{_filePath}'. Will not proceed.\n\t{exception.Message}");
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