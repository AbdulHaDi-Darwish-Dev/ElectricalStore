# ASP.NET API — production multi-stage image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ElectricalStore.sln ./
COPY src/ElectricalStore.Domain/ src/ElectricalStore.Domain/
COPY src/ElectricalStore.Application/ src/ElectricalStore.Application/
COPY src/ElectricalStore.Infrastructure/ src/ElectricalStore.Infrastructure/
COPY src/ElectricalStore.Api/ src/ElectricalStore.Api/
RUN dotnet restore src/ElectricalStore.Api/ElectricalStore.Api.csproj
RUN dotnet publish src/ElectricalStore.Api/ElectricalStore.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /var/lib/electricalstore/dp-keys \
 && chown -R app:app /var/lib/electricalstore
USER app
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "ElectricalStore.Api.dll"]
