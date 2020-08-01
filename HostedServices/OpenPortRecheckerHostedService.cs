using MasscanExporter.Models;
using MasscanExporter.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace MasscanExporter.HostedServices
{
    internal class OpenPortRecheckerHostedService : BackgroundService
    {
        private readonly OpenPortService _openPortService;
        private readonly IOptionsMonitor<IpOptions> _ipOptions;

        public OpenPortRecheckerHostedService(OpenPortService openPortService, IOptionsMonitor<IpOptions> ipOptions)
        {
            _openPortService = openPortService;
            _ipOptions = ipOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var openPorts = OpenPortService.OpenPortsStats.GetAllLabelValues()
                                .Where(x => OpenPortService.OpenPortsStats.WithLabels(x).Value > 0)
                                .Select(x => new OpenPort(x[0], int.Parse(x[1]), x[2]))
                                .ToList();

                foreach (var openPort in openPorts)
                {
                    if (_ipOptions.CurrentValue.GlobalWhitelistedPorts.Contains(openPort.Port) || (_ipOptions.CurrentValue.WhitelistedPorts.ContainsKey(openPort.IP) &&
                            _ipOptions.CurrentValue.WhitelistedPorts[openPort.IP].Contains(openPort.Port)))
                    {
                        OpenPortService.RemoveOpenPortFromStats(openPort);
                        openPorts.Remove(openPort);
                    }
                }

                var ips = openPorts.Select(x => x.IP).ToList();
                if (ips.Any())
                {
                    var ports = openPorts.Select(x => x.Port).ToList();

                    var checkResults = await _openPortService.CheckOpenPorts(ips, ports);

                    var resolvedPorts = openPorts.Except(checkResults).ToList();

                    foreach (var openPort in resolvedPorts)
                    {
                        OpenPortService.RemoveOpenPortFromStats(openPort);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}