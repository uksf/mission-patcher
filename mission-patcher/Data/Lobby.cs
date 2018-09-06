using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MissionPatcher.Data {
    public class Lobby {
#if DEBUG
        private const string URI = "http://localhost:5000/api/";
#else
        private const string URI = "http://api.uk-sf.com/api/";
#endif

        private const string USERNAME = "server";
        private const string PASSWORD = "DernaldIVesTRyleWoonESeisHFA";
        private const string CACHE = "cache";

        private string _token;

        public List<Rank> Ranks;
        public List<Group> Groups;
        public List<Group> OrderedGroups;
        private List<Player> _players;

        public int Setup() {
            try {
                GetLobbyData(Login());
                OrderGroups();
            } catch (Exception exception) {
                Console.WriteLine($"\t{exception.Message}");
                return 1;
            }

            return 0;
        }

        private void ParseLobbyData(JObject data) {
            Ranks = JArray.FromObject(data["ranks"]).Select(x => new Rank {Name = x["name"].ToString()}).ToList();

            Groups = JArray.FromObject(data["groups"]).Select(x => new Group {
                Id = x["id"].ToString(),
                Name = x["name"].ToString(),
                ParentId = x["parent"].ToString(),
                MembersString = x["members"].ToString(),
                RolesString = x["roles"].ToString(),
                Callsign = x["callsign"].ToString()
            }).ToList();

            _players = JArray.FromObject(data["accounts"]).Select(x => new Player {
                Id = x["id"].ToString(),
                Rank = Ranks.FirstOrDefault(r => r.Name == x["rank"].ToString()),
                Name = x["displayName"].ToString(),
                Role = x["roleAssignment"].ToString(),
                GroupName = x["unitAssignment"].ToString()
            }).ToList();

            foreach (Group group in Groups) {
                group.Parent = Groups.FirstOrDefault(g => g.Id == group.ParentId);
                group.Members = string.IsNullOrEmpty(group.MembersString)
                                    ? new List<Player>()
                                    : JArray.Parse(group.MembersString).Select(x => _players.FirstOrDefault(p => p.Id == x.ToString())).ToList();
                if (string.IsNullOrEmpty(group.RolesString)) {
                    group.Roles = new Dictionary<string, Player>();
                } else {
                    Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(group.RolesString);
                    group.Roles = dictionary.ToDictionary(pair => pair.Key, pair => _players.FirstOrDefault(p => p.Id == pair.Value));
                }

                group.Callsign = Resolver.ResolveCallsign(group, group.Callsign);
            }

            foreach (Player player in _players) {
                player.Group = Groups.FirstOrDefault(g => g.Name == player.GroupName);
                player.ObjectClass = Resolver.ResolveObjectClass(player);
            }
        }

        private void OrderGroups() {
            OrderedGroups = new List<Group>();
            Group parent = Groups.First(x => x.Parent == null);
            OrderedGroups.Add(parent);
            InsertGroupChildren(OrderedGroups, parent);
            OrderedGroups.RemoveAll(x => !Resolver.IsGroupPermanent(x) && x.Members.Count == 0 || string.IsNullOrEmpty(x.Callsign));
            Resolver.ResolveSpecialGroups(ref OrderedGroups);
            Resolver.ResolveSpecialGroupOrder(ref OrderedGroups, "5b9123ca7a6c1f0e9875601c", "5ad748e0de5d414f4c4055e0"); // "3 Medical Regiment" after "Guardian 1-R"
            Resolver.ResolveSpecialGroupOrder(ref OrderedGroups, "5a42845c55d6109bf0b081c0", "5b9123ca7a6c1f0e9875601c"); // "18th Signal Regiment" after "3 Medical Regiment"
            Resolver.ResolveSpecialGroupOrder(ref OrderedGroups, "5a68b28e196530164c9b4fed", "5a42845c55d6109bf0b081c0"); // "Sniper Platoon" after "18th Signal Regiment"
            Resolver.ResolveSpecialGroupOrder(ref OrderedGroups, "5a68c047196530164c9b4fee", "5a68b28e196530164c9b4fed"); // "The Pathfinder Platoon" after "Sniper Platoon"
        }

        private void InsertGroupChildren(List<Group> newGroups, Group parent) {
            List<Group> children = Groups.Where(x => x.Parent == parent).ToList();
            if (children.Count == 0) return;
            int index = newGroups.IndexOf(parent);
            newGroups.InsertRange(index + 1, children);
            foreach (Group child in children) {
                InsertGroupChildren(newGroups, child);
            }
        }

        private void GetLobbyData(bool useCache) {
            JObject data;
            if (useCache) {
                Console.WriteLine($"Failed to login, using cache");
                if (File.Exists(CACHE)) {
                    data = JObject.Parse(File.ReadAllText(CACHE));
                } else {
                    throw new FileNotFoundException();
                }
            } else {
                try {
                    using (HttpClient client = new HttpClient()) {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                        string response = client.GetStringAsync($"{URI}accounts/serverlobby").Result;
                        data = JObject.Parse(response);
                        if (data == null) {
                            throw new ArgumentNullException(nameof(data));
                        }

                        File.WriteAllText(CACHE, data.ToString());
                    }
                } catch (Exception) {
                    Console.WriteLine($"Failed to retrieve data, using cache");
                    if (File.Exists(CACHE)) {
                        data = JObject.Parse(File.ReadAllText(CACHE));
                    } else {
                        throw new FileNotFoundException();
                    }
                }
            }

            ParseLobbyData(data);
        }

        private bool Login() {
            try {
                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(new {
                        username = USERNAME,
                        password = PASSWORD
                    }), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync($"{URI}authtoken/server", content).Result;
                    if (!response.IsSuccessStatusCode) {
                        return true;
                    }
                    _token = response.Content.ReadAsStringAsync().Result;
                    return false;
                }
            } catch (Exception) {
                return true;
            }
        }
    }
}