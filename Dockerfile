# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src
COPY ["DotNetActorFramework.sln", "./"]
COPY ["src/DotNetActorFramework/DotNetActorFramework.csproj", "src/DotNetActorFramework/"]

RUN dotnet restore "DotNetActorFramework.sln"

COPY . .

RUN dotnet build "DotNetActorFramework.sln" -c Release --no-restore

RUN dotnet pack "src/DotNetActorFramework/DotNetActorFramework.csproj" \
    -c Release \
    -o /app/packages \
    --no-build

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /app

ENV DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Copy built artifacts
COPY --from=builder /app/packages /app/packages

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

CMD ["dotnet", "--version"]
