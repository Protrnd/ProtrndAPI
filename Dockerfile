# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /source
COPY . .
RUN dotnet restore "./ProtrndWebAPI/ProtrndWebAPI.csproj" --disable-parallel
RUN dotnet publish "./ProtrndWebAPI/ProtrndWebAPI.csproj" -c release -o /app --no-restore

# Server Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0-focal
WORKDIR /app
COPY --from=build /app ./

EXPOSE 5000

ENTRYPOINT ["dotnet", "PreotrndWebAPI.API.dll"]