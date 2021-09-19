#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Unistad Document Manager/Unistad Document Manager.csproj", "Unistad Document Manager/"]
COPY ["StorageLibrary/StorageLibrary.csproj", "StorageLibrary/"]
COPY ["ConfigurationLibrary/ConfigurationLibrary.csproj", "ConfigurationLibrary/"]
RUN dotnet restore "Unistad Document Manager/Unistad Document Manager.csproj"
COPY . .
WORKDIR "/src/Unistad Document Manager"
RUN dotnet build "Unistad Document Manager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Unistad Document Manager.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Unistad Document Manager.dll"]