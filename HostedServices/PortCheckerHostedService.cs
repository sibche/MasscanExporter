using MasscanExporter.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NetTools;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MasscanExporter.HostedServices
{
    public class PortCheckerHostedService : BackgroundService
    {
        private static readonly TimeSpan _ipPortsCheckInterval = TimeSpan.FromDays(1);
        private readonly OpenPortService _openPortService;
        private readonly IOptionsMonitor<IpOptions> _ipOptions;

        public PortCheckerHostedService(OpenPortService openPortService, IOptionsMonitor<IpOptions> ipOptions)
        {
            _openPortService = openPortService;
            _ipOptions = ipOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ips = _ipOptions.CurrentValue.IPs.SelectMany(ip =>
                        IPAddressRange.Parse(ip).AsEnumerable().Select(x => x.ToString()))
                    .Distinct()
                    .ToList();

                var keysToRemove = OpenPortService.IpCheckStartTimeStats.GetAllLabelValues().Select(x => x[0]).Except(ips);

                foreach (var keyToRemove in keysToRemove)
                {
                    OpenPortService.IpCheckStartTimeStats.WithLabels(keyToRemove).Unpublish();
                    OpenPortService.IpCheckEndTimeStats.WithLabels(keyToRemove).Unpublish();
                }

                var lastPortsCheck = ips.ToDictionary(x => x, x => DateTimeOffset.FromUnixTimeSeconds((long)OpenPortService.IpCheckEndTimeStats.WithLabels(x).Value).UtcDateTime);

                var ipsToRefresh = lastPortsCheck.Where(x => DateTime.UtcNow - x.Value > _ipPortsCheckInterval)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var ip in ipsToRefresh)
                {
                    OpenPortService.IpCheckStartTimeStats.WithLabels(ip).SetToCurrentTimeUtc();
                }

                var checkResults = await _openPortService.CheckOpenPorts(ips);

                foreach (var ip in ipsToRefresh)
                {
                    OpenPortService.IpCheckEndTimeStats.WithLabels(ip).SetToCurrentTimeUtc();
                }

                foreach (var openPort in checkResults)
                {
                    if (_ipOptions.CurrentValue.WhitelistedPorts.ContainsKey(openPort.IP) &&
                        _ipOptions.CurrentValue.WhitelistedPorts[openPort.IP].Contains(openPort.Port))
                        continue;

                    OpenPortService.OpenPortsStats
                        .WithLabels(openPort.ToMetricLabelValues())
                        .Set(1);
                }
            }
        }
    }
}
