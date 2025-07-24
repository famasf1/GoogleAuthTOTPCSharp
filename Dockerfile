# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GoogleAuthTotpPrototype/GoogleAuthTotpPrototype.csproj", "GoogleAuthTotpPrototype/"]
RUN dotnet restore "GoogleAuthTotpPrototype/GoogleAuthTotpPrototype.csproj"
COPY . .
WORKDIR "/src/GoogleAuthTotpPrototype"
RUN dotnet build "GoogleAuthTotpPrototype.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GoogleAuthTotpPrototype.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GoogleAuthTotpPrototype.dll"]