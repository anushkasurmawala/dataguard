FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY DataGuard.Api.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DatasetStorage__KeepFiles=false
EXPOSE 8080
ENTRYPOINT ["dotnet", "DataGuard.Api.dll"]
