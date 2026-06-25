FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY schedule-sync-function.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4 AS runtime
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    DOTNET_VERSION=10.0