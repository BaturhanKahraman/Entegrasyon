#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Application/Entegrasyon.API/Entegrasyon.API.csproj", "Application/Entegrasyon.API/"]
COPY ["Application/Entegrasyon.Business/Entegrasyon.Business.csproj", "Application/Entegrasyon.Business/"]
COPY ["Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj", "Application/Entegrasyon.DataAccess/"]
COPY ["Application/Entegrasyon.Entity/Entegrasyon.Entity.csproj", "Application/Entegrasyon.Entity/"]
COPY ["Shared/Shared/Shared.csproj", "Shared/Shared/"]
RUN dotnet restore "Application/Entegrasyon.API/Entegrasyon.API.csproj"
COPY . .
WORKDIR "/src/Application/Entegrasyon.API"
RUN dotnet build "Entegrasyon.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Entegrasyon.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Entegrasyon.API.dll"]