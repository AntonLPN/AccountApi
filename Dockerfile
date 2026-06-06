# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project files first for restore-layer caching
COPY global.json ./
COPY AccountApi/AccountApi.csproj AccountApi/
COPY Account.Application/Account.Application.csproj Account.Application/
COPY Account.Infrastructure/Account.Infrastructure.csproj Account.Infrastructure/
COPY Account.Domain/Account.Domain.csproj Account.Domain/
COPY Account.Contracts/Account.Contracts.csproj Account.Contracts/
RUN dotnet restore AccountApi/AccountApi.csproj

# Copy the rest of the sources and publish
COPY . .
RUN dotnet publish AccountApi/AccountApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AccountApi.dll"]