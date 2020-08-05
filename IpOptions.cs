using System.Collections.Generic;
using System.Linq;

namespace MasscanExporter
{
    public class IpOptions
    {
        public List<string> IPs { get; set; }
        public Dictionary<string, string[]> IPGroups { get; set; } = new Dictionary<string, string[]>();
        public Dictionary<string, int[]> WhitelistedPorts { get; set; } = new Dictionary<string, int[]>();
        public List<int> GlobalWhitelistedPorts { get; set; } = new List<int>();

        private Dictionary<string, string[]> _groupsOfIP;
        public Dictionary<string, string[]> GroupsOfIP
        {
            get
            {
                if (_groupsOfIP == null)
                    _groupsOfIP = IPGroups
                    .SelectMany(x =>
                        x.Value.Select(y => new KeyValuePair<string, string>(y, x.Key)))
                    .GroupBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.Value).Distinct().ToArray());
                return _groupsOfIP;
            }
        }
    }
}
