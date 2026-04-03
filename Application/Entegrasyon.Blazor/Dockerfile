FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first (layer caching)
COPY ["Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj", "Application/Entegrasyon.Blazor/"]
COPY ["Application/Entegrasyon.Business/Entegrasyon.Business.csproj", "Application/Entegrasyon.Business/"]
COPY ["Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj", "Application/Entegrasyon.DataAccess/"]
COPY ["Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj", "Application/Entegrasyon.Entity/"]
COPY ["Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj", "Application/Entegrasyon.ApplicationBootstrap/"]
COPY ["Application/Entegrasyon.MessageQueue/Entegrasyon.MessageQueue.Handler.csproj", "Application/Entegrasyon.MessageQueue/"]
COPY ["Application/Entegrasyon.MessageQueue.Commands/Entegrasyon.MessageQueue.Commands.csproj", "Application/Entegrasyon.MessageQueue.Commands/"]
COPY ["Shared/Shared/Shared.csproj", "Shared/Shared/"]
COPY ["Shared/MarketPlace/MarketPlace.csproj", "Shared/MarketPlace/"]

RUN dotnet restore "Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj"

COPY . .

WORKDIR "/src/Application/Entegrasyon.Blazor"
RUN dotnet publish "Entegrasyon.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=3s \
  CMD wget -qO- http://localhost:8080/ || exit 1
ENTRYPOINT ["dotnet", "Entegrasyon.Blazor.dll"]
