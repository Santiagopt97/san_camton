# Imagen común de las 4 APIs. Construir desde la raíz del repositorio:
#   docker build -f docker/api.Dockerfile --build-arg API_DIR=auth-api --build-arg ASSEMBLY=AuthApi -t hotel-auth-api:dev .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG API_DIR
ARG ASSEMBLY
WORKDIR /src
COPY hotel-security/HotelSecurity.csproj hotel-security/
COPY ${API_DIR}/${ASSEMBLY}.csproj ${API_DIR}/
RUN dotnet restore ${API_DIR}/${ASSEMBLY}.csproj
COPY hotel-security/ hotel-security/
COPY ${API_DIR}/ ${API_DIR}/
RUN dotnet publish ${API_DIR}/${ASSEMBLY}.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG ASSEMBLY
ENV ASSEMBLY=${ASSEMBLY} \
    ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet ${ASSEMBLY}.dll"]
