using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MissionPatcher.Data;

namespace MissionPatcher.Mission {
    public class Mission {
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

        public bool Read() {
            if (Ignored()) return false;
            RemoveUnbinText();
            ReadAllData();
            return true;
        }

        public void Patch(Lobby lobby) {
            _entities.Patch(lobby);
        }

        public void Write() {
            int start = Utility.GetIndexByKey(_sqmLines, "Entities");
            int count = _rawEntities.Count;
            _sqmLines.RemoveRange(start, count);
            IEnumerable<string> newEntities = _entities.Serialize();
            _sqmLines.InsertRange(start, newEntities);
            File.WriteAllLines(_sqmPath, _sqmLines);
        }

        private bool Ignored() {
            if (!File.Exists(_sqmPath)) {
                Console.WriteLine("No description.ext for mission, will not proceed.");
                return false;
            }

            _descLines = File.ReadAllLines(_descPath).ToList();
            return _descLines.Any(x => x.Contains("ignored = 1;"));
        }

        private void RemoveUnbinText() {
            _sqmLines = File.ReadAllLines(_sqmPath).Select(x => x.Trim()).ToList();
            if (_sqmLines.First() != "////////////////////////////////////////////////////////////////////") return;
            _sqmLines = _sqmLines.Skip(9).ToList();
            _sqmLines = _sqmLines.Take(_sqmLines.Count - 1).ToList();
        }

        private void ReadAllData() {
            NextId = Convert.ToInt32(Utility.ReadSingleDataByKey(Utility.ReadDataByKey(_sqmLines, "ItemIDProvider"), "nextID"));
            _rawEntities = Utility.ReadDataByKey(_sqmLines, "Entities");
            _entities = new Entities(_rawEntities);
        }
    }
}