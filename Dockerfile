# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DotNetActorFramework.sln", "./"]
COPY ["src/DotNetActorFramework/DotNetActorFramework.csproj", "src/DotNetActorFramework/"]
RUN dotnet restore "DotNetActorFramework.sln"
COPY . .
# Build and publish the project
RUN dotnet publish "src/DotNetActorFramework/DotNetActorFramework.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
# Application entry point
ENTRYPOINT ["dotnet", "DotNetActorFramework.dll"]
