# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project files and restore dependencies
COPY MP3CONVERTER.csproj ./
RUN dotnet restore

# Copy the entire project and build it
COPY . ./
RUN dotnet publish -c Release -o /out

# Use a lightweight .NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# Expose the port your app runs on
EXPOSE 8080

# Start the application
CMD ["dotnet", "MP3CONVERTER.dll"]
