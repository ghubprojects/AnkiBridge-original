FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["aspire/AnkiBridge.ServiceDefaults/AnkiBridge.ServiceDefaults.csproj", "aspire/AnkiBridge.ServiceDefaults/"]
COPY ["sources/AnkiBridge.Application/AnkiBridge.Application.csproj", "sources/AnkiBridge.Application/"]
COPY ["sources/AnkiBridge.Domain/AnkiBridge.Domain.csproj", "sources/AnkiBridge.Domain/"]
COPY ["sources/AnkiBridge.Infrastructure/AnkiBridge.Infrastructure.csproj", "sources/AnkiBridge.Infrastructure/"]
COPY ["sources/AnkiBridge.MigrationService/AnkiBridge.MigrationService.csproj", "sources/AnkiBridge.MigrationService/"]
COPY ["sources/AnkiBridge.Shared/AnkiBridge.Shared.csproj", "sources/AnkiBridge.Shared/"]
COPY ["sources/AnkiBridge.Web/AnkiBridge.Web.csproj", "sources/AnkiBridge.Web/"]

RUN dotnet restore "sources/AnkiBridge.Web/AnkiBridge.Web.csproj"
RUN dotnet restore "sources/AnkiBridge.MigrationService/AnkiBridge.MigrationService.csproj"

COPY aspire/AnkiBridge.ServiceDefaults/ aspire/AnkiBridge.ServiceDefaults/
COPY sources/ sources/

FROM build AS publish-web
RUN dotnet publish "sources/AnkiBridge.Web/AnkiBridge.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish/web \
    /p:UseAppHost=false

FROM build AS publish-migrations
RUN dotnet publish "sources/AnkiBridge.MigrationService/AnkiBridge.MigrationService.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish/migrations \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
RUN mkdir --parents /home/app/.aspnet/DataProtection-Keys \
    && chown --recursive $APP_UID:$APP_UID /home/app/.aspnet
USER $APP_UID

FROM runtime AS web
COPY --from=publish-web /app/publish/web/ ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "AnkiBridge.Web.dll"]

FROM runtime AS migrations
COPY --from=publish-migrations /app/publish/migrations/ ./
ENTRYPOINT ["dotnet", "AnkiBridge.MigrationService.dll"]
