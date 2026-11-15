# Builds one of the two web apps. The build context is the lab folder (02-identitate-federata-keycloak).
#   docker build -f deploy/azure/Dockerfile --build-arg APP=Secretariat.Web .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP
WORKDIR /lab
COPY src/ ./src/
RUN dotnet publish "src/${APP}/${APP}.csproj" -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG APP
WORKDIR /app
COPY --from=build /out ./
# /data holds the SQLite database; the second folder keeps the Data Protection keys,
# so login cookies stay valid when the container restarts.
RUN mkdir -p /data /home/app/.aspnet/DataProtection-Keys && chown -R app:app /data /home/app/.aspnet
USER app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    APP_DLL=${APP}.dll
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\""]
