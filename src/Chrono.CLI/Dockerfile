FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app
RUN dotnet tool install -g Chrono.CLI --version 1.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["chrono"]
