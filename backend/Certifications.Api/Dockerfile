# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETARCH

WORKDIR /src

COPY ["backend/Certifications.Api/Certifications.Api.csproj", "backend/Certifications.Api/"]
COPY ["backend/Certifications.Application/Certifications.Application.csproj", "backend/Certifications.Application/"]
COPY ["backend/Certifications.Domain/Certifications.Domain.csproj", "backend/Certifications.Domain/"]
COPY ["backend/Certifications.Infrastructure/Certifications.Infrastructure.csproj", "backend/Certifications.Infrastructure/"]

RUN dotnet restore "backend/Certifications.Api/Certifications.Api.csproj" -a "$TARGETARCH"

COPY backend/ backend/

RUN dotnet publish "backend/Certifications.Api/Certifications.Api.csproj" \
    -a "$TARGETARCH" \
    --no-restore \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migrations

COPY [".config/dotnet-tools.json", ".config/dotnet-tools.json"]

RUN dotnet tool restore

ENTRYPOINT ["dotnet", "tool", "run", "dotnet-ef"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R "$APP_UID:$APP_UID" /home/app/.aspnet

COPY --from=build /app/publish .

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "Certifications.Api.dll"]
