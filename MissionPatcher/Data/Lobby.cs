using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        private string _token;

        public List<Rank> Ranks;
        public List<Group> Groups;
        public List<Group> OrderedGroups;
        public List<Player> Players;

        public int Setup() {
            try {
                Login();
                GetLobbyData();
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

            Players = JArray.FromObject(data["accounts"]).Select(x => new Player {
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
                                    : JArray.Parse(group.MembersString).Select(x => Players.FirstOrDefault(p => p.Id == x.ToString())).ToList();
                if (string.IsNullOrEmpty(group.RolesString)) {
                    group.Roles = new Dictionary<string, Player>();
                } else {
                    Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(group.RolesString);
                    group.Roles = dictionary.ToDictionary(pair => pair.Key, pair => Players.FirstOrDefault(p => p.Id == pair.Value));
                }

                group.Callsign = Resolver.ResolveCallsign(group, group.Callsign);
            }

            foreach (Player player in Players) {
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
            Resolver.ResolveSpecialGroupOrder(ref OrderedGroups, "5a4284b155d6109bf0b081c1", "5ad748e0de5d414f4c4055e0"); // "UKSF Medical Group, RAMC" after "Guardian 1-R"
            Resolver.ResolveSpecialGroupOrder(ref OrderedGroups, "5a42845c55d6109bf0b081c0", "5a4284b155d6109bf0b081c1"); // "18th Signal Regiment" after "UKSF Medical Group, RAMC"
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

        private void GetLobbyData() {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URI + "accounts/serverlobby");
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(), _token);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            JObject data;
            using (HttpWebResponse response = (HttpWebResponse) request.GetResponse()) {
                using (Stream stream = response.GetResponseStream()) {
                    if (stream == null) return;
                    using (StreamReader reader = new StreamReader(stream)) {
                        data = JObject.Parse(reader.ReadToEnd());
                    }
                }
            }

            if (data == null) {
                throw new ArgumentNullException($"{nameof(data)}");
            }

            ParseLobbyData(data);
        }

        private void Login() {
            HttpClient httpClient = new HttpClient();
            string json = JsonConvert.SerializeObject(new {loginid = USERNAME, password = PASSWORD});
            StringContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = httpClient.PostAsync(URI + "authtoken", httpContent).Result;
            _token = "bearer " + JObject.Parse(httpResponse.Content.ReadAsStringAsync().Result)["token"];
        }
    }
}