FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=5001
EXPOSE 5001
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AccountApi/AccountApi.csproj", "AccountApi/"]
COPY ["Account.Application/Account.Application.csproj", "Account.Application/"]
COPY ["Account.Contracts/Account.Contracts.csproj", "Account.Contracts/"]
COPY ["Account.Domain/Account.Domain.csproj", "Account.Domain/"]
COPY ["Account.Infrastructure/Account.Infrastructure.csproj", "Account.Infrastructure/"]
RUN dotnet restore "AccountApi/AccountApi.csproj"
COPY . .
WORKDIR "/src/AccountApi"
RUN dotnet build "./AccountApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AccountApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountApi.dll"]
