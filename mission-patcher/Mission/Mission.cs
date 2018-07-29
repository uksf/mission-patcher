using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MissionPatcher.Data;

namespace MissionPatcher.Mission {
    public class Mission {
        private const string UNBIN = "C:\\Program Files (x86)\\Mikero\\DePboTools\\bin\\DeRapDos.exe";

        public static int NextId;
        private readonly string _descPath;
        private readonly string _sqmPath;

        private List<string> _descLines;
        private Entities _entities;

        private List<string> _rawEntities;
        private List<string> _sqmLines;

        public Mission(string path) {
            _descPath = $"{path}/description.ext";
            _sqmPath = $"{path}/mission.sqm";
        }

        public int Read() {
            bool ignored = Ignored();
            if (CheckBinned()) {
                Console.WriteLine($"Mission is binned, unbinning");
                UnBin();
            } else {
                Console.WriteLine("Mission is not binned");
            }

            _sqmLines = File.ReadAllLines(_sqmPath).Select(x => x.Trim()).ToList();
            _sqmLines.RemoveAll(string.IsNullOrEmpty);
            RemoveUnbinText();
            ReadAllData();
            return ignored ? PatchDescription() : 0;
        }

        public void Patch(Lobby lobby) {
            _entities.Patch(lobby);
        }

        public int Write() {
            int start = Utility.GetIndexByKey(_sqmLines, "Entities");
            int count = _rawEntities.Count;
            _sqmLines.RemoveRange(start, count);
            IEnumerable<string> newEntities = _entities.Serialize();
            _sqmLines.InsertRange(start, newEntities);
            File.WriteAllLines(_sqmPath, _sqmLines);
            return PatchDescription();
        }

        private bool Ignored() {
            if (!File.Exists(_descPath)) {
                if (Program.ArgForce) {
                    Console.WriteLine("No description.ext for mission, but patching is forced");
                    return false;
                }

                Console.WriteLine("No description.ext for mission, will not proceed");
                return true;
            }

            _descLines = File.ReadAllLines(_descPath).ToList();
            bool ignored = _descLines.Any(x => x.Contains("lobbyPatchingIgnore"));
            if (!ignored || !Program.ArgForce) return ignored;
            Console.WriteLine("Mission tried to ignore, but patching is forced");
            return false;
        }

        private bool CheckBinned() {
            try {
                Console.WriteLine($"Checking if mission is binned");

                Process process = new Process {StartInfo = {FileName = UNBIN, Arguments = $"-p -q \"{_sqmPath}\"", UseShellExecute = false, CreateNoWindow = true}};
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            } catch (Exception exception) {
                Console.WriteLine($"Failed to bin query '{_sqmPath}'. Will not proceed.\n{exception}");
                return false;
            }
        }

        private void UnBin() {
            try {
                Process process = new Process {StartInfo = {FileName = UNBIN, Arguments = $"-p \"{_sqmPath}\"", UseShellExecute = false, CreateNoWindow = true}};
                process.Start();
                process.WaitForExit();

                Console.WriteLine($"Unbinned '{_sqmPath}'");

                if (File.Exists($"{_sqmPath}.txt")) {
                    File.Delete(_sqmPath);
                    File.Move($"{_sqmPath}.txt", _sqmPath);
                } else {
                    throw new FileNotFoundException();
                }
            } catch (Exception exception) {
                Console.WriteLine($"Failed to unbin '{_sqmPath}'. Will not proceed.\n{exception}");
                throw;
            }
        }

        private void RemoveUnbinText() {
            if (_sqmLines.First() != "////////////////////////////////////////////////////////////////////") return;
            _sqmLines = _sqmLines.Skip(7).ToList();
            _sqmLines = _sqmLines.Take(_sqmLines.Count - 1).ToList();
        }

        private void ReadAllData() {
            NextId = Convert.ToInt32(Utility.ReadSingleDataByKey(Utility.ReadDataByKey(_sqmLines, "ItemIDProvider"), "nextID"));
            _rawEntities = Utility.ReadDataByKey(_sqmLines, "Entities");
            _entities = new Entities(_rawEntities);
        }

        private int PatchDescription() {
            int playable = _sqmLines.Count(x => x.Replace(" ", "").Contains("isPlayable=1") || x.Replace(" ", "").Contains("isPlayer=1"));
            if (!File.Exists(_descPath)) return playable;
            Console.WriteLine("Updating description max players");
            _descLines = File.ReadAllLines(_descPath).ToList();
            _descLines[_descLines.FindIndex(x => x.Contains("maxPlayers"))] = $"    maxPlayers = {playable};";
            int index = _descLines.FindIndex(x => x.Contains("respawnOnStart"));
            if (index != -1) {
                _descLines[index] = "respawnOnStart = 1;";
            } else {
                _descLines.Add("respawnOnStart = 1;");
            }
            index = _descLines.FindIndex(x => x.Contains("__EXEC"));
            if (index != -1) {
                _descLines.RemoveAt(index);
            }
            File.WriteAllLines(_descPath, _descLines);
            return playable;
        }
    }
}