# --- Bước 1: Môi trường Build (Dùng SDK để biên dịch code) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ source code vào trong Docker
COPY . .

# Khôi phục các thư viện (NuGet packages) và Build dự án
RUN dotnet restore "src/AutoWashPro.API/AutoWashPro.API.csproj"
RUN dotnet publish "src/AutoWashPro.API/AutoWashPro.API.csproj" -c Release -o /app/publish

# --- Bước 2: Môi trường Runtime (Chỉ dùng ASP.NET để chạy, giúp file nhẹ nhất có thể) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Mở cổng 8080 để Cloud có thể giao tiếp với API
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copy file đã build từ Bước 1 sang
COPY --from=build /app/publish .

# Lệnh khởi chạy API
ENTRYPOINT ["dotnet", "AutoWashPro.API.dll"]