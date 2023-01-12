#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["HostingMessageDemo/HostingMessageDemo.csproj", "HostingMessageDemo/"]
COPY ["Hydra4Net.HostingExtensions/Hydra4Net.HostingExtensions.csproj", "Hydra4Net.HostingExtensions/"]
COPY ["Hydra4NET/Hydra4NET.csproj", "Hydra4NET/"]
RUN dotnet restore "HostingMessageDemo/HostingMessageDemo.csproj"
COPY . .
WORKDIR "/src/HostingMessageDemo"
RUN dotnet build "HostingMessageDemo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HostingMessageDemo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HostingMessageDemo.dll"]