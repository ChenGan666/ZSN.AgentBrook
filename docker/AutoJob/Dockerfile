# 使用 .NET 10 运行时镜像
FROM mcr.m.daocloud.io/dotnet/aspnet:10.0
WORKDIR /app


# 安装必要工具和字体
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    ca-certificates \
    ghostscript \
    libreoffice \
    fonts-dejavu \
    fonts-liberation \
    fonts-noto-cjk \ 
    && fc-cache -fv \
    && rm -rf /var/lib/apt/lists/*

# 安装最新 Pandoc (从 GitHub Releases 下载)
# 这里用的是 3.8 版本，你可以根据需要替换 PANDOC_VERSION
ENV PANDOC_VERSION=3.8

RUN wget -q https://github.com/jgm/pandoc/releases/download/${PANDOC_VERSION}/pandoc-${PANDOC_VERSION}-1-arm64.deb \
    && dpkg -i pandoc-${PANDOC_VERSION}-1-arm64.deb \
    && rm pandoc-${PANDOC_VERSION}-1-arm64.deb
    
# 将发布的文件复制到容器中
COPY ./publish/ ./
COPY ./appsettings.json ./appsettings.json

# 设置入口点
ENTRYPOINT ["dotnet", "ZSN.AgentBrook.AutoJob.dll"]
