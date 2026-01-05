# Etapa base: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Etapa de build: restore y publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el csproj y restaura dependencias (mejora cache en builds)
COPY ["InvoiceManagementAPI/InvoiceManagementAPI.csproj", "InvoiceManagementAPI/"]
RUN dotnet restore "InvoiceManagementAPI/InvoiceManagementAPI.csproj"

# Copia todo el código y publica en Release
COPY . .
WORKDIR /src/InvoiceManagementAPI
RUN dotnet publish -c Release -o /app/publish --no-restore /p:PublishTrimmed=false

# Etapa final: copiar artefactos publicados
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Arranque: permite usar la variable de entorno PORT que Render asigna.
ENTRYPOINT ["sh", "-c", "dotnet InvoiceManagementAPI.dll --urls http://0.0.0.0:${PORT:-5000}"]