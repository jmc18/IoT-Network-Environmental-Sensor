# Azure Functions .NET isolated worker - based on A_PG structure
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IoTBackend/Functions/Functions.csproj", "IoTBackend/Functions/"]
COPY ["IoTBackend/Shared/IoTBackend.Shared.csproj", "IoTBackend/Shared/"]
RUN dotnet restore "IoTBackend/Functions/Functions.csproj"
COPY . .
WORKDIR "/src/IoTBackend/Functions"
RUN dotnet build "Functions.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Functions.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
