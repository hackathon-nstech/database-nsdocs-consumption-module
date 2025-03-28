# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy csproj and restore as distinct layers to leverage Docker cache
COPY *.sln .
COPY src/NSdocs.API/*.csproj ./src/NSdocs.API/
COPY src/NSdocs.Application/*.csproj ./src/NSdocs.Application/
COPY src/NSdocs.Domain/*.csproj ./src/NSdocs.Domain/
COPY src/NSdocs.Infrastructure/*.csproj ./src/NSdocs.Infrastructure/
COPY src/NSdocs.Flusher/*.csproj ./src/NSdocs.Flusher/ 

# Restore dependencies
RUN dotnet restore NSdocs.sln

# Copy everything else and build app
COPY . .
WORKDIR /source/src/NSdocs.API
RUN dotnet publish -c Release -o /app/publish

# Use the official ASP.NET Core runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose the port the app runs on (adjust if your app uses a different port)
EXPOSE 8080 

# Set the entrypoint for the application
ENTRYPOINT ["dotnet", "NSdocs.API.dll"]
