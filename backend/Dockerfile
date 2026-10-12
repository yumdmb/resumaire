FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Resumaire.Api.csproj backend/
RUN dotnet restore backend/Resumaire.Api.csproj --runtime linux-x64
COPY backend/ backend/
RUN dotnet publish backend/Resumaire.Api.csproj -c Release -o /app/publish --no-restore
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.7
RUN /tools/dotnet-ef migrations bundle --project backend --startup-project backend --configuration Release --runtime linux-x64 --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG TYPST_VERSION=0.15.1
USER root
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl xz-utils \
    && curl -fSL "https://github.com/typst/typst/releases/download/v${TYPST_VERSION}/typst-x86_64-unknown-linux-musl.tar.xz" -o /tmp/typst.tar.xz \
    && tar -xJf /tmp/typst.tar.xz -C /tmp \
    && install /tmp/typst-x86_64-unknown-linux-musl/typst /usr/local/bin/typst \
    && rm -rf /tmp/typst* /var/lib/apt/lists/* \
    && mkdir /keys && chown app:app /keys
WORKDIR /app
COPY --from=build /app/publish/ ./
COPY --from=build --chmod=755 /app/efbundle ./efbundle
ENV ASPNETCORE_HTTP_PORTS=8080 ASPNETCORE_ENVIRONMENT=Production
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Resumaire.Api.dll"]
