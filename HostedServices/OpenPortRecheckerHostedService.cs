using MasscanExporter.Models;
using MasscanExporter.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<OpenPortRecheckerHostedService> _logger;

        public OpenPortRecheckerHostedService(OpenPortService openPortService, IOptionsMonitor<IpOptions> ipOptions, ILogger<OpenPortRecheckerHostedService> logger)
        {
            _openPortService = openPortService;
            _ipOptions = ipOptions;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var _ = _logger.BeginScope(("job", "open port rechecker hosted service"));
            while (!stoppingToken.IsCancellationRequested)
            {
                try
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "rechecking the open ports failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}