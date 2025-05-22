FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/*.csproj", "./"]

RUN dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0-preview.2.24128.4
RUN dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0-preview.2.24128.4
RUN dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0-preview.1

RUN dotnet restore

COPY src/ .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /app/storage
VOLUME /app/storage

ENTRYPOINT ["dotnet", "FileStoringService.dll"]
