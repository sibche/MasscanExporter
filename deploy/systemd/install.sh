#!/bin/bash
set -e

git clone https://github.com/sibche/MasscanExporter /tmp/MasscanExporter
pushd /tmp/MasscanExporter
dotnet publish --configuration Release MasscanExporter.csproj -o /opt/masscan-exporter
cp -f deploy/systemd/masscan-exporter.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable masscan-exporter.service
systemctl restart masscan-exporter.service
popd
rm -rf /tmp/MasscanExporter
