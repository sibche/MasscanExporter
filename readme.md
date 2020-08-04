# Masscan Exporter

This project users [masscan](https://github.com/robertdavidgraham/masscan) project to periodically check your public IPs and export metrics on IPs with open port. You can add the metrics to your prometheus and setup proper alerts to get notified on unwanted open ports on your infrastructure before other people find them out.

# Installation

there's two way to install masscan exporter. docker and systemd. I experienced weird behavior on docker installation (it couldn't find open ports after a while) so I moved to systemd.

> note that on both installation options, `appsettings.json` file should be present on the installation directory

## Systemd

1. make sure masscan is installed and exists on `PATH`. you can compile it and move it to `/usr/local/bin`.
1. install .net core 3.1 SDK on target server
1. upload installation script from `deploy/systemd/install.sh` to the target server
1. upload your `appsettings.json` (take a look at `appsettings.example.json`) to `/opt/masscan-exporter`
1. run the installation script. it'd clone the project on the server, build and installs it on `/opt/masscan-exporter` and sets up the systemd unit.

### Updating

you can run `install.sh` again in order to update the project to the latest version (a.k.a. master branch)

## Docker

take a look at `deploy/docker/docker-compose.yaml`.

# Development

## Docker Debug

Visual Studio docker debug always expects the first image on `Dockerfile` to be the base image for debugging. Since we've using multistage build to build masscan we can't give our first image to base. So in order to debug the project with VS docker debugger you need to do these changes

```patch
diff --git Dockerfile Dockerfile
--- Dockerfile
+++ Dockerfile
@@ -1,16 +1,14 @@
 #See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
 
-FROM debian:buster-slim as masscan
+FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
 WORKDIR /src
 RUN apt-get update
 RUN apt-get install -y git gcc make libpcap-dev
 RUN git clone https://github.com/robertdavidgraham/masscan .
 RUN make -j
-
-FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
+RUN mv bin/masscan /usr/local/bin/masscan
 WORKDIR /app
 EXPOSE 80
-COPY --from=masscan /src/bin/masscan /usr/local/bin/masscan
 
 FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
 WORKDIR /src

```