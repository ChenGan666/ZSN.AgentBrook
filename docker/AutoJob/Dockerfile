# =========================================================
# .NET 10 + Playwright + Chromium + LibreOffice
# Stable Production Dockerfile
# ARM64 / x64
# =========================================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# =========================================================
# Environment
# =========================================================

ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=Asia/Shanghai
ENV PANDOC_VERSION=3.8

ENV ASPNETCORE_URLS=http://+:8080

# Playwright driver 路径（避免 GetExecutablePath 中的 Parent.Parent 空引用）
ENV PLAYWRIGHT_DRIVER_SEARCH_PATH=/app
# Playwright 浏览器安装目录
ENV PLAYWRIGHT_BROWSERS_PATH=/app/browsers

# =========================================================
# Install Dependencies
# =========================================================

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    \
    # Basic tools
    wget \
    curl \
    unzip \
    ca-certificates \
    tzdata \
    procps \
    xdg-utils \
    gnupg \
    \
    # Office
    ghostscript \
    libreoffice \
    \
    # FFmpeg (VoiceNode 音频预处理)
    ffmpeg \
    \
    # Fonts
    fonts-dejavu \
    fonts-liberation \
    fonts-noto-cjk \
    fonts-noto-color-emoji \
    \
    \
    # Playwright dependencies
    libnss3 \
    libnspr4 \
    libatk1.0-0t64 \
    libatk-bridge2.0-0t64 \
    libcups2t64 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libgtk-3-0 \
    libasound2t64 \
    libpangocairo-1.0-0 \
    libcairo2 \
    libpango-1.0-0 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxext6 \
    libxfixes3 \
    libxrender1 \
    libxshmfence1 \
    libxcb-dri3-0 \
    libglib2.0-0 \
    libdbus-1-3 \
    libxtst6 \
    libu2f-udev \
    libvulkan1 \
    \
    && fc-cache -fv && \
    rm -rf /var/lib/apt/lists/*

# =========================================================
# Install Pandoc
# =========================================================

RUN ARCH=$(dpkg --print-architecture) && \
    wget -q \
    https://github.com/jgm/pandoc/releases/download/${PANDOC_VERSION}/pandoc-${PANDOC_VERSION}-1-${ARCH}.deb && \
    dpkg -i pandoc-${PANDOC_VERSION}-1-${ARCH}.deb && \
    rm pandoc-${PANDOC_VERSION}-1-${ARCH}.deb

# =========================================================
# Create Non-Root User
# =========================================================

RUN groupadd -r appgroup && \
    useradd -r -g appgroup -G audio,video appuser && \
    mkdir -p /home/appuser/Downloads && \
    chown -R appuser:appgroup /home/appuser

# =========================================================
# Copy Application
# IMPORTANT:
# publish output MUST contain .playwright
# =========================================================

COPY --chown=appuser:appgroup ./publish/ ./

COPY --chown=appuser:appgroup ./appsettings.json ./appsettings.json

# 验证 Playwright driver 是否存在
RUN echo "=== Playwright Driver Check ===" && \
    ls -la /app/.playwright/node/ && \
    ls -la /app/.playwright/node/linux-arm64/node 2>/dev/null || \
    echo "WARNING: linux-arm64 driver NOT FOUND" && \
    echo "==============================="

RUN chown -R appuser:appgroup /app && \
    chmod +x /app/.playwright/node/*/node 2>/dev/null || true

# 确保 Chromium headless_shell 有执行权限
RUN chmod +x /app/browsers/chromium_headless_shell-*/chrome-linux/headless_shell 2>/dev/null || true

# =========================================================
# Runtime User
# =========================================================

USER appuser

# =========================================================
# Entrypoint
# =========================================================

ENTRYPOINT ["dotnet", "ZSN.AgentBrook.AutoJob.dll"]