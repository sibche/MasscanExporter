using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasscanExporter.Models
{
    public class MasscanConf
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public List<string> IPs { get; set; } = new List<string>();
        public List<int> Ports { get; set; }

        public string ConfigPath => $"/opt/{Id}.conf";
        public string OutputPath => $"/opt/{Id}.out.json";
        public string PortsString => Ports != null ? string.Join(",", Ports) : "0-65535";
    }
}
