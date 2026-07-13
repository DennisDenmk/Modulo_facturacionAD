# Etapa base: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Etapa de build: restore y publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el .csproj desde el contexto raíz (optimiza cache)
COPY ["InvoiceManagementAPI/InvoiceManagementAPI.csproj", "InvoiceManagementAPI/"]
RUN dotnet restore "InvoiceManagementAPI/InvoiceManagementAPI.csproj"

# Copia todo el repositorio y publica la app
COPY . .
WORKDIR /src/InvoiceManagementAPI
RUN dotnet publish -c Release -o /app/publish /p:PublishTrimmed=false

# Etapa final: runtime ligero
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Usar la variable PORT que Render proporciona; fallback a 5000 para pruebas locales
ENTRYPOINT ["sh", "-c", "dotnet InvoiceManagementAPI.dll --urls http://0.0.0.0:${PORT:-5000}"]
