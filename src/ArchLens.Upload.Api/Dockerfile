FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/SharedKernel/ArchLens.SharedKernel/ArchLens.SharedKernel.csproj", "src/SharedKernel/ArchLens.SharedKernel/"]
COPY ["src/Contracts/ArchLens.Contracts/ArchLens.Contracts.csproj", "src/Contracts/ArchLens.Contracts/"]
COPY ["src/Services/Upload/ArchLens.Upload.Domain/ArchLens.Upload.Domain.csproj", "src/Services/Upload/ArchLens.Upload.Domain/"]
COPY ["src/Services/Upload/ArchLens.Upload.Application.Contracts/ArchLens.Upload.Application.Contracts.csproj", "src/Services/Upload/ArchLens.Upload.Application.Contracts/"]
COPY ["src/Services/Upload/ArchLens.Upload.Application/ArchLens.Upload.Application.csproj", "src/Services/Upload/ArchLens.Upload.Application/"]
COPY ["src/Services/Upload/ArchLens.Upload.Infrastructure/ArchLens.Upload.Infrastructure.csproj", "src/Services/Upload/ArchLens.Upload.Infrastructure/"]
COPY ["src/Services/Upload/ArchLens.Upload.Api/ArchLens.Upload.Api.csproj", "src/Services/Upload/ArchLens.Upload.Api/"]
COPY ["Directory.Build.props", "."]

RUN dotnet restore "src/Services/Upload/ArchLens.Upload.Api/ArchLens.Upload.Api.csproj"

COPY src/SharedKernel/ src/SharedKernel/
COPY src/Contracts/ src/Contracts/
COPY src/Services/Upload/ src/Services/Upload/

RUN dotnet build "src/Services/Upload/ArchLens.Upload.Api/ArchLens.Upload.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Services/Upload/ArchLens.Upload.Api/ArchLens.Upload.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ArchLens.Upload.Api.dll"]
