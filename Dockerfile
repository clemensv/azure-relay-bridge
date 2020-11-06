FROM mcr.microsoft.com/dotnet/core/sdk:3.0 as publish

COPY . /azure-relay-bridge/
WORKDIR /azure-relay-bridge/src/azbridge
RUN dotnet publish azbridge.csproj -c Release -f netcoreapp3.1 -p:SelfContained=false -p:PublishTrimmed=false -o /app

FROM mcr.microsoft.com/dotnet/core/runtime:3.0
RUN apt-get update
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT [ "/app/azbridge" ]
