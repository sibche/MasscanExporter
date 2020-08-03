using MasscanExporter.Models;
using Microsoft.Extensions.Logging;
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
                IPs = ips.Distinct().ToList(),
                Ports = ports?.Distinct().ToList(),
            };

            new DirectoryInfo(Path.GetDirectoryName(masscanConf.ConfigPath)).Create();
            new DirectoryInfo(Path.GetDirectoryName(masscanConf.OutputPath)).Create();

            var configs = await razorLightEngine.CompileRenderAsync("masscan-conf.cshtml", masscanConf);
            await File.WriteAllTextAsync(masscanConf.ConfigPath, configs);

            using var process = new Process
            {
                StartInfo = {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"masscan -c '{masscanConf.ConfigPath}'\"",
                },
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<bool>();

            process.Exited += (sender, args) =>
            {
                var result = process.ExitCode == 0;
                tcs.TrySetResult(result);
            };

            if (process.Start())
            {
                if (await tcs.Task)
                {
                    var result = JsonSerializer.Deserialize<List<MasscanOutput>>(await File.ReadAllTextAsync(masscanConf.OutputPath));
                    return result.SelectMany(x => x.Ports.Select(y => new OpenPort(x.IP, y.Port, y.Protocol))).ToList();
                }
            }
            return new List<OpenPort>();
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
