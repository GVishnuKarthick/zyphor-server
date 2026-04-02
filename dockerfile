# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else
COPY . ./

# Build the app
RUN dotnet publish -c Release -o out


# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy build output
COPY --from=build /app/out ./

# Expose port
EXPOSE 8080

# Run app
ENTRYPOINT ["dotnet", "ZyphorAPI.dll"]