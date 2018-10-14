using System;
using System.Collections.Generic;

namespace MissionPatcher.Data {
    public static class Resolver {
        public static string ResolveObjectClass(Player player) {
            switch (player.Unit.Id) {
                case "5a435eea905d47336442c75a": // "Joint Special Forces Aviation Wing"
                case "5a848590eab14d12cc7fa618": // "JSFAW Training Unit"
                    return "UKSF_B_Pilot";
                case "5a441619730e9d162834500b": // "7 Squadron"
                    return "UKSF_B_Pilot_7";
                case "5a441602730e9d162834500a": // "656 Squadron"
                    return "UKSF_B_Pilot_656";
                case "5a4415d8730e9d1628345007": // "617 Squadron"
                    return "UKSF_B_Pilot_617";
                case "5a68b28e196530164c9b4fed": // "Sniper Platoon"
                    return "UKSF_B_Sniper";
                case "5a68c047196530164c9b4fee": // "The Pathfinder Platoon"
                    return "UKSF_B_Pathfinder";
                case "5bbbb8875eb3a4170c488b24": // "Air Troop"
                case "5bbbb89e5eb3a4170c488b26": // "Mobility Troop"
                    return "UKSF_B_SAS";
                case "5ba8983ee12a331f94cb02d4": // "SAS"
                    return "UKSF_B_Officer";
                case "5b9123ca7a6c1f0e9875601c": // "3 Medical Regiment"
                    return "UKSF_B_Medic";
                case "5a42835b55d6109bf0b081bd": // "UKSF"
                    if (ResolvePlayerUnitRole(player) == 2) {
                        return "UKSF_B_Officer";
                    }

                    return "UKSF_B_Rifleman";
                default:
                    if (ResolvePlayerUnitRole(player) != -1) {
                        return "UKSF_B_SectionLeader";
                    }

                    return "UKSF_B_Rifleman";
            }
        }

        private static int ResolvePlayerUnitRole(Player player) {
            if (player.Unit.Roles.ContainsKey("1iC") && player.Unit.Roles["1iC"] == player) return 2;
            if (player.Unit.Roles.ContainsKey("2iC") && player.Unit.Roles["2iC"] == player) return 1;
            if (player.Unit.Roles.ContainsKey("NCOiC") && player.Unit.Roles["NCOiC"] == player) return 0;
            return -1;
        }

        public static string ResolveCallsign(Unit unit, string defaultCallsign) {
            switch (unit.Id) {
                case "5a435eea905d47336442c75a": // "Joint Special Forces Aviation Wing"
                case "5a441619730e9d162834500b": // "7 Squadron"
                case "5a441602730e9d162834500a": // "656 Squadron"
                case "5a4415d8730e9d1628345007": // "617 Squadron"
                case "5a848590eab14d12cc7fa618": // "JSFAW Training Unit"
                    return "JSFAW";
                default: return defaultCallsign;
            }
        }

        public static void ResolveSpecialUnits(ref List<Unit> orderedUnits) {
            List<Unit> newOrderedUnits = new List<Unit>();
            foreach (Unit unit in orderedUnits) {
                switch (unit.Id) {
                    case "5a441619730e9d162834500b": // "7 Squadron"
                    case "5a441602730e9d162834500a": // "656 Squadron"
                    case "5a4415d8730e9d1628345007": // "617 Squadron"
                    case "5a848590eab14d12cc7fa618": // "JSFAW Training Unit"
                        continue;
                    default:
                        newOrderedUnits.Add(unit);
                        break;
                }
            }

            orderedUnits = newOrderedUnits;
        }

        public static void ResolveSpecialUnitOrder(ref List<Unit> orderedUnits, string unitId, string unitBeforeId) {
            Unit unit = orderedUnits.Find(x => x.Id == unitId);
            orderedUnits.Remove(unit);
            orderedUnits.Insert(orderedUnits.FindIndex(x => x.Id == unitBeforeId) + 1, unit);
        }

        public static List<Player> ResolveUnitSlots(Lobby lobby, Unit unit) {
            List<Player> slots = new List<Player>();
            int max = 8;
            int fillerCount;
            switch (unit.Id) {
                case "5a435eea905d47336442c75a": // "Joint Special Forces Aviation Wing"
                    slots.AddRange(lobby.Units.Find(x => x.Id == "5a435eea905d47336442c75a").Members);
                    slots.AddRange(lobby.Units.Find(x => x.Id == "5a441619730e9d162834500b").Members);
                    slots.AddRange(lobby.Units.Find(x => x.Id == "5a441602730e9d162834500a").Members);
                    slots.AddRange(lobby.Units.Find(x => x.Id == "5a4415d8730e9d1628345007").Members);
                    slots.AddRange(lobby.Units.Find(x => x.Id == "5a848590eab14d12cc7fa618").Members);
                    break;
                case "5a68b28e196530164c9b4fed": // "Sniper Platoon"
                    max = 3;
                    slots.AddRange(unit.Members);
                    fillerCount = max - slots.Count;
                    for (int i = 0; i < fillerCount; i++) {
                        Player player = new Player {Name = "Sniper", Unit = unit, Rank = lobby.Ranks.Find(x => x.Name == "Private")};
                        player.ObjectClass = ResolveObjectClass(player);
                        slots.Add(player);
                    }

                    break;
                case "5bbbb9645eb3a4170c488b36": // "Guardian 1-1"
                case "5bbbbdab5eb3a4170c488f2e": // "Guardian 1-2"
                case "5bbbbe365eb3a4170c488f30": // "Guardian 1-3"
                    slots.AddRange(unit.Members);
                    fillerCount = max - slots.Count;
                    for (int i = 0; i < fillerCount; i++) {
                        Player player = new Player {Name = "Reserve", Unit = unit, Rank = lobby.Ranks.Find(x => x.Name == "Recruit")};
                        player.ObjectClass = ResolveObjectClass(player);
                        slots.Add(player);
                    }

                    break;
                case "5ad748e0de5d414f4c4055e0": // "Guardian 1-R"
                    for (int i = 0; i < 6; i++) {
                        Player player = new Player {Name = "Reserve", Unit = unit, Rank = lobby.Ranks.Find(x => x.Name == "Recruit")};
                        player.ObjectClass = ResolveObjectClass(player);
                        slots.Add(player);
                    }

                    break;
                default:
                    slots = unit.Members;
                    break;
            }

            slots.Sort((a, b) => {
                int roleA = ResolvePlayerUnitRole(a);
                int roleB = ResolvePlayerUnitRole(b);
                int rankA = lobby.Ranks.IndexOf(a.Rank);
                int rankB = lobby.Ranks.IndexOf(b.Rank);
                return roleA < roleB ? 1 : (roleA > roleB ? -1 : rankA < rankB ? -1 : (rankA > rankB ? 1 : string.CompareOrdinal(a.Name, b.Name)));
            });
            return slots;
        }

        public static bool IsUnitPermanent(Unit unit) {
            switch (unit.Id) {
                case "5bbbb9645eb3a4170c488b36": // "Guardian 1-1"
                case "5bbbbdab5eb3a4170c488f2e": // "Guardian 1-2"
                case "5bbbbe365eb3a4170c488f30": // "Guardian 1-3"
                case "5ad748e0de5d414f4c4055e0": // "Guardian 1-R"
                    return true;
                default: return false;
            }
        }
    }
}