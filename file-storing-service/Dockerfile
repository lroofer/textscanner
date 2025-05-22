FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/*.csproj", "./"]
RUN dotnet restore

COPY src/ .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /app/storage
VOLUME /app/storage

ENTRYPOINT ["dotnet", "FileStoringService.dll"]
