FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj", "Application/Entegrasyon.Blazor/"]
COPY ["Application/Entegrasyon.Business/Entegrasyon.Business.csproj", "Application/Entegrasyon.Business/"]
COPY ["Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj", "Application/Entegrasyon.DataAccess/"]
COPY ["Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj", "Application/Entegrasyon.Entity/"]
COPY ["Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj", "Application/Entegrasyon.ApplicationBootstrap/"]
COPY ["Application/Entegrasyon.MessageQueue/Entegrasyon.MessageQueue.Handler.csproj", "Application/Entegrasyon.MessageQueue/"]
COPY ["Application/Entegrasyon.MessageQueue.Commands/Entegrasyon.MessageQueue.Commands.csproj", "Application/Entegrasyon.MessageQueue.Commands/"]
COPY ["Shared/Shared/Shared.csproj", "Shared/Shared/"]
COPY ["Shared/MarketPlace/MarketPlace.csproj", "Shared/MarketPlace/"]

# Restore dependencies
RUN dotnet restore "Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/Application/Entegrasyon.Blazor"
RUN dotnet build "Entegrasyon.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Entegrasyon.Blazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Entegrasyon.Blazor.dll"]
