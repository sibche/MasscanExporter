using MasscanExporter.Models;
using Prometheus;
using RazorLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MasscanExporter.Services
{
    public class OpenPortService
    {
        public static readonly Gauge OpenPortsStats = Metrics.CreateGauge("netstat_open_ports", "indicator of open ports",
            new GaugeConfiguration
            {
                LabelNames = new[] { "ip", "port", "protocol" }
            });

        public static readonly Gauge IpCheckStartTimeStats = Metrics.CreateGauge("netstat_ip_check_start_time", "start time of the time ip was checked",
            new GaugeConfiguration
            {
                LabelNames = new[] { "ip" }
            });
        public static readonly Gauge IpCheckEndTimeStats = Metrics.CreateGauge("netstat_ip_check_end_time", "end time of the time ip was checked",
            new GaugeConfiguration
            {
                LabelNames = new[] { "ip" }
            });

        private readonly RazorLightEngine razorLightEngine;

        public OpenPortService(RazorLightEngine razorLightEngine)
        {
            this.razorLightEngine = razorLightEngine;
        }

        public async Task<List<OpenPort>> CheckOpenPorts(List<string> ips, List<int> ports = null)
        {
            var masscanConf = new MasscanConf
            {
                IPs = ips,
                Ports = ports,
            };

            await File.WriteAllTextAsync(masscanConf.ConfigPath,
                await razorLightEngine.CompileRenderAsync("masscan-conf.cshtml", masscanConf));

            using var process = new Process
            {
                StartInfo = { FileName = "/usr/local/bin/masscan", Arguments = $"-c {masscanConf.ConfigPath}" },
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<bool>();

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode == 0);
            };

            process.Start();
            await tcs.Task;

            var result = JsonSerializer.Deserialize<List<MasscanOutput>>(await File.ReadAllTextAsync(masscanConf.OutputPath));

            return result.SelectMany(x => x.Ports.Select(y => new OpenPort(x.IP, y.Port, y.Protocol))).ToList();
        }

        public static void RemoveOpenPortFromStats(OpenPort openPort)
        {
            OpenPortsStats
                .WithLabels(openPort.ToMetricLabelValues())
                .Set(0);
            OpenPortsStats
                .WithLabels(openPort.ToMetricLabelValues())
                .Unpublish();
        }
    }
}
