FROM harbor.betawm.com/mcr/aspnet-ffmpeg:v8-0 AS base
ENV _AppId_ web-airoadshow-h5api
WORKDIR /beta
EXPOSE 80

FROM harbor.betawm.com/mcr/sdk:v8-0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]
COPY ["Beta.AIRoadshow.H5Api/Beta.AIRoadshow.H5Api.csproj", "Beta.AIRoadshow.H5Api/"]
COPY ["Beta.AIRoadshow.Application/Beta.AIRoadshow.Application.csproj", "Beta.AIRoadshow.Application/"]
COPY ["Beta.AIRoadshow.Common/Beta.AIRoadshow.Common.csproj", "Beta.AIRoadshow.Common/"]
COPY ["Beta.AIRoadshow.DataAccess/Beta.AIRoadshow.DataAccess.csproj", "Beta.AIRoadshow.DataAccess/"]
COPY ["Beta.AIRoadshow.Entity/Beta.AIRoadshow.Entity.csproj", "Beta.AIRoadshow.Entity/"]
RUN dotnet restore "./Beta.AIRoadshow.H5Api/Beta.AIRoadshow.H5Api.csproj"
COPY . .
WORKDIR "/src/Beta.AIRoadshow.H5Api"
RUN dotnet build "./Beta.AIRoadshow.H5Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "./Beta.AIRoadshow.H5Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /beta
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Beta.AIRoadshow.H5Api.dll"]