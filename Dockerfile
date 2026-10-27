# ============================================================
# Condominio360 - Imagen Docker para despliegue (Render, etc.)
# ============================================================

# ---------- Etapa 1: compilar y publicar ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar dependencias primero (aprovecha la caché de Docker)
COPY Condominio360/Condominio360.csproj Condominio360/
RUN dotnet restore Condominio360/Condominio360.csproj

# Copiar el resto del código y publicar en modo Release
COPY Condominio360/ Condominio360/
RUN dotnet publish Condominio360/Condominio360.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Etapa 2: imagen final (solo runtime) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Carpeta para comprobantes de pago (el usuario 'app' debe poder escribir)
RUN mkdir -p /app/App_Data/comprobantes && chown -R $APP_UID /app/App_Data

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Condominio360.dll"]
