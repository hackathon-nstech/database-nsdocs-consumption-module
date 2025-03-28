# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy project files first
# COPY *.sln . # Removed solution copy
COPY src/NSdocs.Domain/*.csproj ./src/NSdocs.Domain/
COPY src/NSdocs.Application/*.csproj ./src/NSdocs.Application/
COPY src/NSdocs.Infrastructure/*.csproj ./src/NSdocs.Infrastructure/
COPY src/NSdocs.Flusher/*.csproj ./src/NSdocs.Flusher/

# Restore the Flusher project (which will restore dependencies)
RUN dotnet restore src/NSdocs.Flusher/NSdocs.Flusher.csproj

# Copy the rest of the source code
COPY . .

# Publish the application
WORKDIR /source
# Publish Flusher project directly, ensuring restored packages are used
RUN dotnet publish src/NSdocs.Flusher/NSdocs.Flusher.csproj -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Set the entry point for the container
ENTRYPOINT ["dotnet", "NSdocs.Flusher.dll"]
