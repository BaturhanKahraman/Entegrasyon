FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first (layer caching)
COPY ["Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj", "Application/Entegrasyon.Blazor/"]
COPY ["Application/Entegrasyon.Business/Entegrasyon.Business.csproj", "Application/Entegrasyon.Business/"]
COPY ["Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj", "Application/Entegrasyon.DataAccess/"]
COPY ["Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj", "Application/Entegrasyon.Entity/"]
COPY ["Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj", "Application/Entegrasyon.ApplicationBootstrap/"]
COPY ["Application/Entegrasyon.AdminPanel/Entegrasyon.AdminPanel.csproj", "Application/Entegrasyon.AdminPanel/"]
COPY ["Application/Entegrasyon.Storefront/Entegrasyon.Storefront.csproj", "Application/Entegrasyon.Storefront/"]
COPY ["Agent/Entegrasyon.PrintAgent.Contracts/Entegrasyon.PrintAgent.Contracts.csproj", "Agent/Entegrasyon.PrintAgent.Contracts/"]

RUN dotnet restore "Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj"

COPY . .

WORKDIR "/src/Application/Entegrasyon.Blazor"
RUN dotnet publish "Entegrasyon.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=5 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Entegrasyon.Blazor.dll"]
