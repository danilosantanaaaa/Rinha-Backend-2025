FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
EXPOSE 8080

# Dependencia necessaria para rodar o código nativo
RUN apk update \
    && apk add build-base zlib-dev

COPY ["src/Rinha.Api.csproj", "./"]

RUN dotnet restore "Rinha.Api.csproj"
COPY . ../
WORKDIR /src

FROM build AS publish
RUN dotnet publish --no-restore -c Release --property PublishDir=out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /src
COPY --from=publish /src/out .

ENTRYPOINT ["./Rinha.Api"]