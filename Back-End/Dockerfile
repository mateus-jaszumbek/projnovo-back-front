FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ServicosApp.slnx ./
COPY src/ServicosApp.API/ServicosApp.API.csproj src/ServicosApp.API/
COPY src/ServicosApp.Application/ServicosApp.Application.csproj src/ServicosApp.Application/
COPY src/ServicosApp.Domain/ServicosApp.Domain.csproj src/ServicosApp.Domain/
COPY src/ServicosApp.Infrastructure/ServicosApp.Infrastructure.csproj src/ServicosApp.Infrastructure/
COPY src/ServicosApp.Infrastructure.PostgresMigrations/ServicosApp.Infrastructure.PostgresMigrations.csproj src/ServicosApp.Infrastructure.PostgresMigrations/

RUN dotnet restore src/ServicosApp.API/ServicosApp.API.csproj

COPY . .
RUN dotnet publish src/ServicosApp.API/ServicosApp.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ServicosApp.API.dll"]
