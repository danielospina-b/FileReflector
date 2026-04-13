FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
COPY FileReflector /app/FileReflector/
WORKDIR /app/FileReflector/
RUN dotnet publish FileReflector.csproj --configuration Release --output /app/release

FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update && apt-get install -y --no-install-recommends \
    ssh \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/release /app/
WORKDIR /app/
CMD [ "/app/FileReflector" ]