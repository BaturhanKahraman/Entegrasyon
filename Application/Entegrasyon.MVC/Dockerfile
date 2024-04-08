
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y curl \
    && curl -sL https://deb.nodesource.com/setup_14.x | bash - \
    && apt-get install -y nodejs

COPY ["Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj", "Application/Entegrasyon.MVC/"]
COPY ["Application/Entegrasyon.Business/Entegrasyon.Business.csproj", "Application/Entegrasyon.Business/"]
COPY ["Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj", "Application/Entegrasyon.DataAccess/"]
COPY ["Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj", "Application/Entegrasyon.Entity/"]
COPY ["Shared/Shared/Shared.csproj", "Shared/Shared/"]
RUN dotnet restore "Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj"
COPY . .
WORKDIR "/src/Application/Entegrasyon.MVC"
RUN dotnet build "Entegrasyon.MVC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Entegrasyon.MVC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Entegrasyon.MVC.dll"]