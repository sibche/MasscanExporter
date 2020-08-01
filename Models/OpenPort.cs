namespace MasscanExporter.Models
{
    public class OpenPort
    {
        public string IP { get; }
        public int Port { get; }
        public string Protocol { get; }

        public OpenPort(string ip, int port, string protocol)
        {
            IP = ip;
            Port = port;
            Protocol = protocol;
        }

        public string[] ToMetricLabelValues() => new[] { IP, Port.ToString(), Protocol };


        public override int GetHashCode()
        {
            return $"{IP}:{Port}/{Protocol}".GetHashCode();
        }

        public override string ToString()
        {
            return $"{IP}:{Port}/{Protocol}";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is OpenPort)) return false;
            return ((OpenPort)obj).GetHashCode() == GetHashCode();
        }
    }
}