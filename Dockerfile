# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["EduHub.Core/EduHub.Core.csproj", "EduHub.Core/"]
COPY ["EduHub.Data/EduHub.Data.csproj", "EduHub.Data/"]
COPY ["EduHub.Services/EduHub.Services.csproj", "EduHub.Services/"]
COPY ["EduHub.Web/EduHub.Web.csproj", "EduHub.Web/"]

RUN dotnet restore "EduHub.Web/EduHub.Web.csproj"

# Copy all source files and publish
COPY . .
WORKDIR "/src/EduHub.Web"
RUN dotnet publish "EduHub.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Create directory for local uploads if Azure Blob Storage is not configured
RUN mkdir -p /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "EduHub.Web.dll"]
