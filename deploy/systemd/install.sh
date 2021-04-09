#!/bin/bash
set -e

git clone https://github.com/sibche/MasscanExporter /tmp/MasscanExporter
pushd /tmp/MasscanExporter
systemctl stop masscan-exporter.service || true
dotnet publish --configuration Release MasscanExporter.csproj -o /opt/masscan-exporter
cat << EOF > /etc/systemd/system/masscan-exporter.service
[Unit]
Description=masscan-exporter

[Service]
WorkingDirectory=/opt/masscan-exporter
ExecStart=`which dotnet` /opt/masscan-exporter/MasscanExporter.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=masscan-exporter
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target

EOF
systemctl daemon-reload
systemctl enable masscan-exporter.service
systemctl restart masscan-exporter.service
popd
rm -rf /tmp/MasscanExporter
