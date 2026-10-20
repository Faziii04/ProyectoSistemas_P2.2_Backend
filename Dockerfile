# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copia sólo el .csproj para que la capa de restore quede en caché.
COPY practica2.2_backend.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish practica2.2_backend.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
USER $APP_UID

# Render inyecta la variable PORT; la aplicación debe escuchar en ella.
ENTRYPOINT ["sh", "-c", "exec dotnet practica2.2_backend.dll --urls http://0.0.0.0:${PORT:-8080}"]
