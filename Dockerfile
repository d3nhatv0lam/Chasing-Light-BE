# ==============================================================================
# Stage 1: Build & Publish ứng dụng .NET 10
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props và .csproj trước để tận dụng Docker layer cache khi restore NuGet
COPY Directory.Build.props .
COPY src/ChasingLight.Api/ChasingLight.Api.csproj src/ChasingLight.Api/
RUN dotnet restore src/ChasingLight.Api/ChasingLight.Api.csproj

# Copy mã nguồn API và publish bản Release độc lập (tắt AppHost để chạy trực tiếp qua dotnet)
COPY src/ChasingLight.Api/ src/ChasingLight.Api/
RUN dotnet publish src/ChasingLight.Api/ChasingLight.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ==============================================================================
# Stage 2: Runtime image tối giản
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Cài tối giản curl để phục vụ HEALTHCHECK (chạy dưới quyền root trước khi chuyển sang USER app)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Chạy ứng dụng bằng user non-root có sẵn của .NET image để tăng bảo mật
USER app

COPY --from=build /app/publish .

# Xóa ASPNETCORE_HTTP_PORTS mặc định của base image để ưu tiên cấu hình động qua PORT (Render/Railway)
# hoặc biến ASPNETCORE_HTTP_PORTS do người dùng truyền vào lúc chạy container; mặc định 8080
ENV PORT=8080 \
    ASPNETCORE_HTTP_PORTS="" \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Kiểm tra sức khỏe dựa theo cổng thực tế container đang lắng nghe
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD sh -c 'TARGET_PORT=${ASPNETCORE_HTTP_PORTS:-${PORT:-8080}}; curl -fsS "http://localhost:${TARGET_PORT%%;*}/health" || exit 1'

# Dùng exec để tiến trình dotnet nhận trực tiếp tín hiệu SIGTERM khi dừng container
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_HTTP_PORTS=${ASPNETCORE_HTTP_PORTS:-${PORT:-8080}}; exec dotnet ChasingLight.Api.dll"]
