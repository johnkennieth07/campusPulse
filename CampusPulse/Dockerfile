# ── Stage 1: Build ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["CampusPulse.csproj", "./"]
RUN dotnet restore "CampusPulse.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "CampusPulse.csproj" -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CampusPulse.dll"]