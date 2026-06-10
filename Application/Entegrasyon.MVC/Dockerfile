FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
# Alpine runtime ICU icermez → globalization-invariant mode'da tr-TR gibi kulturler
# patlar (CultureNotFoundException). ICU ekle + invariant mode'u kapat.
# tzdata: Alpine zaman dilimi DB icermez → TimeZoneInfo "Europe/Istanbul" patlar
# (TurkeyTime static initializer → onu kullanan TUM sayfalar 500). tzdata ekle.
RUN apk add --no-cache icu-libs icu-data-full tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=Europe/Istanbul

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Central Package Management — versiyonlar Directory.Packages.props'ta; restore'dan
# ONCE kopyalanmali yoksa NU1015 (PackageReference version yok).
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]

# Copy project files first (layer caching)
COPY ["Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj", "Application/Entegrasyon.MVC/"]
COPY ["Application/Entegrasyon.Business/Entegrasyon.Business.csproj", "Application/Entegrasyon.Business/"]
COPY ["Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj", "Application/Entegrasyon.DataAccess/"]
COPY ["Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj", "Application/Entegrasyon.Entity/"]
COPY ["Application/Entegrasyon.ApplicationBootstrap/Entegrasyon.ApplicationBootstrap.csproj", "Application/Entegrasyon.ApplicationBootstrap/"]
COPY ["Application/Entegrasyon.AdminPanel/Entegrasyon.AdminPanel.csproj", "Application/Entegrasyon.AdminPanel/"]
COPY ["Agent/Entegrasyon.PrintAgent.Contracts/Entegrasyon.PrintAgent.Contracts.csproj", "Agent/Entegrasyon.PrintAgent.Contracts/"]

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
