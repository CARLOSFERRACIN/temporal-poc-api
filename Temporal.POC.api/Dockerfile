# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Temporal.POC.api.sln .
COPY Temporal.POC.api/Temporal.POC.api.csproj Temporal.POC.api/
RUN dotnet restore Temporal.POC.api/Temporal.POC.api.csproj

# Copy everything else and build
COPY Temporal.POC.api/ Temporal.POC.api/
RUN dotnet build Temporal.POC.api/Temporal.POC.api.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish Temporal.POC.api/Temporal.POC.api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Temporal.POC.api.dll"]

