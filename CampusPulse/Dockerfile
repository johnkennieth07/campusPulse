# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CampusPulse.csproj", "./"]
RUN dotnet restore "CampusPulse.csproj"

COPY . .
RUN dotnet publish "CampusPulse.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CampusPulse.dll"]