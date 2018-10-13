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
        //private const string URI = "http://localhost:5000/api/";
        private const string URI = "http://api.uk-sf.com/api/";

        private const string USERNAME = "server";
        private const string PASSWORD = "DernaldIVesTRyleWoonESeisHFA";
        private const string CACHE = "cache";

        private string _token;

        public List<Rank> Ranks;
        public List<Unit> Units;
        public List<Unit> OrderedUnits;
        private List<Player> _players;

        public int Setup() {
            try {
                GetLobbyData(Login());
                OrderUnits();
            } catch (Exception exception) {
                Console.WriteLine($"\t{exception.Message}");
                return 1;
            }

            return 0;
        }

        private void ParseLobbyData(JObject data) {
            Ranks = JArray.FromObject(data["ranks"]).Select(x => new Rank {Name = x["name"].ToString()}).ToList();

            Units = JArray.FromObject(data["units"]).Select(x => new Unit {
                Id = x["id"].ToString(),
                Name = x["name"].ToString(),
                ParentId = x["parent"].ToString(),
                MembersString = x["members"].ToString(),
                RolesString = x["roles"].ToString(),
                Callsign = x["callsign"].ToString(),
                Order = int.Parse(x["order"].ToString())
            }).ToList();

            _players = JArray.FromObject(data["accounts"]).Select(x => new Player {
                Id = x["id"].ToString(),
                Rank = Ranks.FirstOrDefault(r => r.Name == x["rank"].ToString()),
                Name = x["displayName"].ToString(),
                Role = x["roleAssignment"].ToString(),
                UnitName = x["unitAssignment"].ToString()
            }).ToList();

            foreach (Unit unit in Units) {
                unit.Parent = Units.FirstOrDefault(g => g.Id == unit.ParentId);
                unit.Members = string.IsNullOrEmpty(unit.MembersString)
                                    ? new List<Player>()
                                    : JArray.Parse(unit.MembersString).Select(x => _players.FirstOrDefault(p => p.Id == x.ToString())).ToList();
                if (string.IsNullOrEmpty(unit.RolesString)) {
                    unit.Roles = new Dictionary<string, Player>();
                } else {
                    Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(unit.RolesString);
                    unit.Roles = dictionary.ToDictionary(pair => pair.Key, pair => _players.FirstOrDefault(p => p.Id == pair.Value));
                }

                unit.Callsign = Resolver.ResolveCallsign(unit, unit.Callsign);
            }

            foreach (Player player in _players) {
                player.Unit = Units.FirstOrDefault(g => g.Name == player.UnitName);
                player.ObjectClass = Resolver.ResolveObjectClass(player);
            }
        }

        private void OrderUnits() {
            OrderedUnits = new List<Unit>();
            Unit parent = Units.First(x => x.Parent == null);
            OrderedUnits.Add(parent);
            InsertUnitChildren(OrderedUnits, parent);
            OrderedUnits.RemoveAll(x => !Resolver.IsUnitPermanent(x) && x.Members.Count == 0 || string.IsNullOrEmpty(x.Callsign));
            Resolver.ResolveSpecialUnits(ref OrderedUnits);
        }

        private void InsertUnitChildren(List<Unit> newUnits, Unit parent) {
            List<Unit> children = Units.Where(x => x.Parent == parent).OrderBy(x => x.Order).ToList();
            if (children.Count == 0) return;
            int index = newUnits.IndexOf(parent);
            newUnits.InsertRange(index + 1, children);
            foreach (Unit child in children) {
                InsertUnitChildren(newUnits, child);
            }
        }

        private void GetLobbyData(bool useCache) {
            JObject data;
            if (useCache) {
                Console.WriteLine("Failed to login, using cache");
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