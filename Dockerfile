# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["CSE325FinalProject.csproj", "./"]
RUN dotnet restore "./CSE325FinalProject.csproj"

# Copy the remaining source code
COPY . .
WORKDIR "/src/."

# Build and publish the application
RUN dotnet build "CSE325FinalProject.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "CSE325FinalProject.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configure for Render
# Render sets the PORT environment variable.
# .NET 8+ by default listens on 8080.
# We explicitly set it to 8080 to be sure/consistent.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CSE325FinalProject.dll"]
