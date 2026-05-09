# ============================================================================
# PostgreSQL Docker 镜像 - 支持向量搜索和图数据库
# 版本: 2.0
# 日期: 2026-04-01
# 包含: PostgreSQL 16.6 + pgvector 0.8.0 + Apache AGE 1.5.0 (可选)
# ============================================================================

# 使用官方 PostgreSQL 镜像作为基础镜像
FROM postgres:16.6 AS builder

# ============================================================================
# 环境变量配置
# ============================================================================

# pgvector版本（向量搜索扩展）
ENV PGVECTOR_VERSION=0.8.0

# Apache AGE版本（图数据库扩展）- 可选
ENV AGE_VERSION=1.6.0

# 设置时区
ENV TZ=Asia/Shanghai

# ============================================================================
# 安装扩展
# ============================================================================

# 安装编译依赖
RUN apt-get update && apt-get install -y \
    build-essential \
    postgresql-server-dev-16 \
    curl \
    libcap-dev \
    flex \
    bison \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /tmp

# 下载并编译 Apache AGE
# 使用 GitHub 源码归档，注意标签路径的特殊编码
RUN curl -L "https://github.com/apache/age/releases/download/PG16%2Fv${AGE_VERSION}-rc0/apache-age-${AGE_VERSION}-src.tar.gz" -o age.tar.gz && \
    tar -xzf age.tar.gz && \
    cd apache-age-${AGE_VERSION} && \
    make && \
    make install

## 第二阶段：运行阶段 (Final)
FROM postgres:16

ARG AGE_VERSION
LABEL maintainer="Custom Image with AGE & PGVector"

# 1. 安装运行时依赖 (pgvector 是官方仓库提供的，可直接安装)
RUN apt-get update && apt-get install -y \
    postgresql-16-pgvector \
    && rm -rf /var/lib/apt/lists/*

# 2. 从构建阶段拷贝编译好的扩展文件
# AGE 的核心组件包括 .so 库、.control 和 .sql 文件
COPY --from=builder /usr/lib/postgresql/16/lib/age.so /usr/lib/postgresql/16/lib/
COPY --from=builder /usr/share/postgresql/16/extension/age* /usr/share/postgresql/16/extension/

# ============================================================================
# 复制初始化脚本
# ============================================================================

# 复制初始化脚本（按字母顺序执行）
# 1. init.sh - 加载扩展
COPY ./init.sh /docker-entrypoint-initdb.d/01_init.sh

# 2. 0_init.sql - 创建数据库和表结构
COPY ./0_init.sql /docker-entrypoint-initdb.d/02_init.sql

# 设置脚本执行权限
RUN chmod +x /docker-entrypoint-initdb.d/01_init.sh

# ============================================================================
# 配置优化（可选）
# ============================================================================

# 创建自定义配置文件目录
RUN mkdir -p /etc/postgresql/conf.d

# 复制自定义配置（如果有）
# COPY ./postgresql.conf /etc/postgresql/conf.d/custom.conf

# ============================================================================
# 暴露端口
# ============================================================================

# PostgreSQL 默认端口
EXPOSE 5432

# ============================================================================
# 健康检查
# ============================================================================

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD pg_isready -U postgres || exit 1

# ============================================================================
# 元数据标签
# ============================================================================

LABEL maintainer="ZSN.AgentBrook Team" \
      version="2.0" \
      description="PostgreSQL with pgvector and Apache AGE for AgentBrook Memory System" \
      postgresql.version="16.6" \
      pgvector.version="0.8.0" \
      age.version="1.6.0"

# ============================================================================
# 使用说明
# ============================================================================

# 构建镜像:
#   docker build -t zsn-postgres:16.6 .
#
# 启动容器:
#   docker run -d \
#     --name zsn-postgres \
#     -p 5432:5432 \
#     -e POSTGRES_PASSWORD=your_password \
#     -v $(pwd)/data:/var/lib/postgresql/data \
#     zsn-postgres:16.6
#
# 验证扩展:
#   docker exec -it zsn-postgres psql -U postgres -c "\dx"
#
# 查看日志:
#   docker logs -f zsn-postgres
#
# 连接数据库:
#   docker exec -it zsn-postgres psql -U postgres -d zsn_agentbrook_base
