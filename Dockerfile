#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM debian:buster-slim as masscan
WORKDIR /src
RUN apt-get update
RUN apt-get install -y git gcc make libpcap-dev
RUN git clone https://github.com/robertdavidgraham/masscan .
RUN make -j

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
COPY --from=masscan /src/bin/masscan /usr/local/bin/masscan

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["MasscanExporter.csproj", ""]
RUN dotnet restore "./MasscanExporter.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MasscanExporter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MasscanExporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MasscanExporter.dll"]