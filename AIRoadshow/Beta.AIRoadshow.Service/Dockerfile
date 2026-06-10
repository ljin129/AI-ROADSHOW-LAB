FROM harbor.betawm.com/mcr/aspnet-ffmpeg:v8-0 AS base
ENV _AppId_ svc-airoadshow-service
WORKDIR /beta
EXPOSE 80

FROM harbor.betawm.com/mcr/sdk:v8-0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]
COPY ["Beta.AIRoadshow.Service/Beta.AIRoadshow.Service.csproj", "Beta.AIRoadshow.Service/"]
COPY ["Beta.AIRoadshow.Application/Beta.AIRoadshow.Application.csproj", "Beta.AIRoadshow.Application/"]
COPY ["Beta.AIRoadshow.Common/Beta.AIRoadshow.Common.csproj", "Beta.AIRoadshow.Common/"]
COPY ["Beta.AIRoadshow.DataAccess/Beta.AIRoadshow.DataAccess.csproj", "Beta.AIRoadshow.DataAccess/"]
COPY ["Beta.AIRoadshow.Entity/Beta.AIRoadshow.Entity.csproj", "Beta.AIRoadshow.Entity/"]
RUN dotnet restore "./Beta.AIRoadshow.Service/Beta.AIRoadshow.Service.csproj"
COPY . .
WORKDIR "/src/Beta.AIRoadshow.Service"
RUN dotnet build "./Beta.AIRoadshow.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Beta.AIRoadshow.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/beta/Beta.AIRoadshow.Service.dll"]