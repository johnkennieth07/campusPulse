FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# FIXED PATH HERE
COPY ["CampusPulse/CampusPulse.csproj", "CampusPulse/"]

RUN dotnet restore "CampusPulse/CampusPulse.csproj"

COPY . .

RUN dotnet publish "CampusPulse/CampusPulse.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CampusPulse.dll"]