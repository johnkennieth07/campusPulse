# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file using correct path
COPY ["CampusPulse/CampusPulse.csproj", "CampusPulse/"]

RUN dotnet restore "CampusPulse/CampusPulse.csproj"

# Copy everything else
COPY . .

RUN dotnet publish "CampusPulse/CampusPulse.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CampusPulse.dll"]