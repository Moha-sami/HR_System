# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

# Copy solution and project files
COPY *.slnx .
COPY src/Buy2.Domain/*.csproj src/Buy2.Domain/
COPY src/Buy2.Application/*.csproj src/Buy2.Application/
COPY src/Buy2.Infrastructure/*.csproj src/Buy2.Infrastructure/
COPY src/Buy2.Api/*.csproj src/Buy2.Api/

# Restore dependencies
RUN dotnet restore src/Buy2.Api/Buy2.Api.csproj

# Copy full source and publish
COPY . .
WORKDIR /app/src/Buy2.Api
RUN dotnet publish -c Release -o /out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Buy2.Api.dll"]
