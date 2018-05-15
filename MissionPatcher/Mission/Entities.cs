using System;
using System.Collections.Generic;
using System.Linq;
using MissionPatcher.Data;

namespace MissionPatcher.Mission {
    public class Entities {
        private readonly List<Item> _items = new List<Item>();
        private int _itemsCount;

        public Entities(List<string> rawEntities) {
            _itemsCount = Convert.ToInt32(Utility.ReadSingleDataByKey(rawEntities, "items"));
            ParseItems(rawEntities);
        }

        private Entities(Lobby lobby, Group group) {
            List<Player> slots = Resolver.ResolveGroupSlots(lobby, group);
            for (int i = 0; i < slots.Count; i++) {
                _items.Add(new Item(slots[i], i));
            }
        }

        private void ParseItems(List<string> rawEntities) {
            int index = rawEntities.FindIndex(x => x.Contains("class Item"));
            while (_items.Count != _itemsCount) {
                _items.Add(new Item(Utility.ReadDataFromIndex(rawEntities, ref index)));
            }
        }

        public void Patch(Lobby lobby) {
            _items.RemoveAll(x => x.ItemType.Equals("Group") && x.Entities != null && x.Entities._items.All(y => y.IsPlayable && !y.Ignore()));
            foreach (Group lobbyGroup in lobby.OrderedGroups) {
                Entities entities = new Entities(lobby, lobbyGroup);
                _items.Add(new Item(entities, lobbyGroup.Callsign));
            }

            _itemsCount = _items.Count;
            for (int index = 0; index < _items.Count; index++) {
                Item item = _items[index];
                item.Patch(index);
            }
        }

        public IEnumerable<string> Serialize() {
            _itemsCount = _items.Count;
            List<string> serialized = new List<string> {"class Entities", "{", $"items = {_itemsCount};"};
            foreach (Item item in _items) {
                serialized.AddRange(item.Serialize());
            }

            serialized.Add("};");
            return serialized;
        }
    }
}