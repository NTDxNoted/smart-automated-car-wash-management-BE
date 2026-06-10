FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/AutoWashPro.API/AutoWashPro.API.csproj", "AutoWashPro.API/"]
COPY ["src/AutoWash.Application/AutoWash.Application.csproj", "AutoWash.Application/"]
COPY ["src/AutoWash.Domain/AutoWash.Domain.csproj", "AutoWash.Domain/"]
COPY ["src/AutoWash.Infrastructure/AutoWash.Infrastructure.csproj", "AutoWash.Infrastructure/"]
RUN dotnet restore "AutoWashPro.API/AutoWashPro.API.csproj"

# Copy the rest of the source code and build
COPY src/ .
WORKDIR "/src/AutoWashPro.API"
RUN dotnet build "AutoWashPro.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "AutoWashPro.API.csproj" -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 5000 and run the application
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "AutoWashPro.API.dll"]
