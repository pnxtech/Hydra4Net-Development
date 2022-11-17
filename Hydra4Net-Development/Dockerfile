#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Hydra4Net-Development/Hydra4Net-Development.csproj", "Hydra4Net-Development/"]
RUN dotnet restore "Hydra4Net-Development/Hydra4Net-Development.csproj"
COPY . .
WORKDIR "/src/Hydra4Net-Development"
RUN dotnet build "Hydra4Net-Development.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hydra4Net-Development.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hydra4Net-Development.dll"]