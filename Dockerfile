FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
EXPOSE 8080

COPY ["src/Rinha.Api.csproj", "./"]

RUN dotnet restore "Rinha.Api.csproj"
COPY . ../
WORKDIR /src
RUN dotnet build "Rinha.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [ "dotnet", "Rinha.Api.dll"]