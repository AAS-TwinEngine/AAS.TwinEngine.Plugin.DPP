#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Aas.TwinEngine.Plugin.RelationalDatabase/Aas.TwinEngine.Plugin.RelationalDatabase.csproj", "Aas.TwinEngine.Plugin.RelationalDatabase/"]
RUN dotnet restore "Aas.TwinEngine.Plugin.RelationalDatabase/Aas.TwinEngine.Plugin.RelationalDatabase.csproj"
COPY Aas.TwinEngine.Plugin.RelationalDatabase/ Aas.TwinEngine.Plugin.RelationalDatabase/
WORKDIR "/src/Aas.TwinEngine.Plugin.RelationalDatabase"

RUN dotnet build "Aas.TwinEngine.Plugin.RelationalDatabase.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Aas.TwinEngine.Plugin.RelationalDatabase.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

RUN useradd -m appuser \
 && mkdir /app \
 && chown appuser:appuser /app

USER appuser
WORKDIR /app

ENV DATA_DIR=/app/Data
RUN mkdir -p $DATA_DIR

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aas.TwinEngine.Plugin.RelationalDatabase.dll"]
