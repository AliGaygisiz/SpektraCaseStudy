FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y curl
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SpektraCaseStudy.csproj", "./"]
RUN dotnet restore "SpektraCaseStudy.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "SpektraCaseStudy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpektraCaseStudy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpektraCaseStudy.dll"]
