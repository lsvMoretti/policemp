using System.Collections.Generic;

namespace DiscordBot.Models
{
    public class MasterListPlayer
    {
        public string endpoint { get; set; }
        public int id { get; set; }
        public List<string> identifiers { get; set; }
        public string name { get; set; }
        public int ping { get; set; }
    }
}