using System.Collections.Generic;

namespace MissionPatcher.Data {
    public class Unit {
        public string Callsign;
        public string Id;
        public List<Player> Members;
        public string MembersString;
        public string Name;
        public Unit Parent;
        public string ParentId;
        public Dictionary<string, Player> Roles;
        public string RolesString;
        public int Order;
    }
}