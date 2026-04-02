FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first (layer caching)
COPY ["Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj", "Application/Entegrasyon.MVC/"]
COPY ["Application/Entegrasyon.Business/Entegrasyon.Business.csproj", "Application/Entegrasyon.Business/"]
COPY ["Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj", "Application/Entegrasyon.DataAccess/"]
COPY ["Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj", "Application/Entegrasyon.Entity/"]
COPY ["Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj", "Application/Entegrasyon.ApplicationBootstrap/"]
COPY ["Application/Entegrasyon.AdminPanel/Entegrasyon.AdminPanel.csproj", "Application/Entegrasyon.AdminPanel/"]
COPY ["Application/Entegrasyon.MessageQueue/Entegrasyon.MessageQueue.Handler.csproj", "Application/Entegrasyon.MessageQueue/"]
COPY ["Application/Entegrasyon.MessageQueue.Commands/Entegrasyon.MessageQueue.Commands.csproj", "Application/Entegrasyon.MessageQueue.Commands/"]
COPY ["Shared/Shared/Shared.csproj", "Shared/Shared/"]
COPY ["Shared/MarketPlace/MarketPlace.csproj", "Shared/MarketPlace/"]

RUN dotnet restore "Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj"

COPY . .

WORKDIR "/src/Application/Entegrasyon.MVC"
RUN dotnet publish "Entegrasyon.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=3s \
  CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Entegrasyon.MVC.dll"]
