FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY ./src ./src
COPY ./data-pumper-web.sln .
RUN dotnet restore data-pumper-web.sln

FROM build AS publish
RUN dotnet publish src/DataPumper.Web/DataPumper.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
COPY --from=publish /app .
CMD ["dotnet", "DataPumper.Web.dll"]