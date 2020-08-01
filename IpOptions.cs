using System.Collections.Generic;

namespace MasscanExporter
{
    public class IpOptions
    {
        public List<string> IPs { get; set; }
        public Dictionary<string, int[]> WhitelistedPorts { get; set; } = new Dictionary<string, int[]>();
        public List<int> GlobalWhitelistedPorts { get; set; } = new List<int>();
    }
}
