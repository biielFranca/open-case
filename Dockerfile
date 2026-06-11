# ─── Build ───
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpenCase.slnx .
COPY src/ src/
COPY tests/ tests/

RUN dotnet restore
RUN dotnet publish src/OpenCase.Web -c Release -o /app/publish

# ─── Runtime ───
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "OpenCase.Web.dll"]
