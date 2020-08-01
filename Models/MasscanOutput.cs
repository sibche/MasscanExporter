using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MasscanExporter.Models
{
    public class MasscanOutput
    {
        [JsonPropertyName("ip")]
        public string IP { get; set; }
        [JsonPropertyName("ports")]
        public List<MasscanPortInfo> Ports { get; set; }
    }

    public class MasscanPortInfo
    {
        [JsonPropertyName("port")]
        public int Port { get; set; }
        [JsonPropertyName("proto")]
        public string Protocol { get; set; }
    }
}
