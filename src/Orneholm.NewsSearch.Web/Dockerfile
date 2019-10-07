FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["src/Orneholm.NewsSearch.Web/Orneholm.NewsSearch.Web.csproj", "src/Orneholm.NewsSearch.Web/"]
RUN dotnet restore "src/Orneholm.NewsSearch.Web/Orneholm.NewsSearch.Web.csproj"
COPY . .
WORKDIR "/src/src/Orneholm.NewsSearch.Web"
RUN dotnet build "Orneholm.NewsSearch.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Orneholm.NewsSearch.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orneholm.NewsSearch.Web.dll"]