using System;
using System.Collections.Generic;
using System.Linq;
using MissionPatcher.Data;

namespace MissionPatcher.Mission {
    public class Item {
        private static double _position = 10;
        private readonly List<string> _rawEntities = new List<string>();
        private readonly List<string> _rawItem = new List<string>();
        public readonly Entities Entities;
        public readonly bool IsPlayable;
        public readonly string ItemType;

        public Item(List<string> rawItem) {
            _rawItem = rawItem;
            ItemType = Utility.ReadSingleDataByKey(_rawItem, "dataType").ToString();
            if (ItemType.Equals("Group")) {
                _rawEntities = Utility.ReadDataByKey(_rawItem, "Entities");
                if (_rawEntities.Count > 0) {
                    Entities = new Entities(_rawEntities);
                }
            } else if (ItemType.Equals("Object")) {
                string isPlayable = Utility.ReadSingleDataByKey(_rawItem, "isPlayable").ToString();
                string isPlayer = Utility.ReadSingleDataByKey(_rawItem, "isPlayer").ToString();
                if (!string.IsNullOrEmpty(isPlayable)) {
                    IsPlayable = isPlayable == "1";
                } else if (!string.IsNullOrEmpty(isPlayer)) {
                    IsPlayable = isPlayer == "1";
                }
            }
        }

        public Item(Player player, int index) {
            _rawItem.Add($"class Item{index}");
            _rawItem.Add("{");
            _rawItem.Add("dataType=\"Object\";");
            _rawItem.Add($"flags={(index == 0 ? "7" : "5")};");
            _rawItem.Add($"id={Mission.NextId++};");
            _rawItem.Add("class PositionInfo");
            _rawItem.Add("{");
            _rawItem.Add("position[]={" + $"{_position += 1}" + ",0,0};");
            _rawItem.Add("};");
            _rawItem.Add("side=\"West\";");
            _rawItem.Add($"type=\"{player.ObjectClass}\";");
            _rawItem.Add("class Attributes");
            _rawItem.Add("{");
            _rawItem.Add("isPlayable=1;");
            _rawItem.Add($"description=\"{player.Name}{(string.IsNullOrEmpty(player.Role) ? "" : $" - {player.Role}")}@{Resolver.ResolveCallsign(player.Unit, player.Unit.Callsign)}\";");
            _rawItem.Add("};");
            _rawItem.Add("};");
        }

        public Item(Entities entities, string callsign) {
            Entities = entities;
            _rawItem.Add("class Item");
            _rawItem.Add("{");
            _rawItem.Add("dataType=\"Group\";");
            _rawItem.Add("side=\"West\";");
            _rawItem.Add($"id={Mission.NextId++};");
            _rawItem.Add("class Entities");
            _rawItem.Add("{");
            _rawItem.Add("};");
            _rawItem.Add("class Attributes");
            _rawItem.Add("{");
            _rawItem.Add("};");
            _rawItem.Add("class CustomAttributes");
            _rawItem.Add("{");
            _rawItem.Add("class Attribute0");
            _rawItem.Add("{");
            _rawItem.Add("property=\"groupID\";");
            _rawItem.Add("expression=\"[_this, _value] call CBA_fnc_setCallsign\";");
            _rawItem.Add("class Value");
            _rawItem.Add("{");
            _rawItem.Add("class data");
            _rawItem.Add("{");
            _rawItem.Add("class type");
            _rawItem.Add("{");
            _rawItem.Add("type[]={\"STRING\"};");
            _rawItem.Add("};");
            _rawItem.Add($"value=\"{callsign}\";");
            _rawItem.Add("};");
            _rawItem.Add("};");
            _rawItem.Add("};");
            _rawItem.Add("nAttributes=1;");
            _rawItem.Add("};");
            _rawItem.Add("};");
            _rawEntities = Utility.ReadDataByKey(_rawItem, "Entities");
        }

        public bool Ignore() {
            bool ignored = _rawItem.Any(x => x.ToLower().Contains("@ignore"));
            if (!ignored || !Program.ArgForce) return ignored;
            Console.WriteLine("Item tried to ignore, but patching is forced");
            return false;
        }

        public void Patch(int index) {
            _rawItem[0] = $"class Item{index}";
        }

        public IEnumerable<string> Serialize() {
            List<string> serialized = new List<string>();
            if (_rawEntities.Count > 0) {
                int start = Utility.GetIndexByKey(_rawItem, "Entities");
                int count = _rawEntities.Count;
                _rawItem.RemoveRange(start, count);
                _rawItem.InsertRange(start, Entities.Serialize());
            }

            foreach (string s in _rawItem) {
                serialized.Add(s);
            }

            return serialized;
        }
    }
}