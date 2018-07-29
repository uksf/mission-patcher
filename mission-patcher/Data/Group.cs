using System.Collections.Generic;

namespace MissionPatcher.Data {
    public class Group {
        public string Callsign;
        public string Id;
        public List<Player> Members;
        public string MembersString;
        public string Name;
        public Group Parent;
        public string ParentId;
        public Dictionary<string, Player> Roles;
        public string RolesString;
    }
}