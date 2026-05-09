#!/bin/bash
# ============================================================================
# PostgreSQL 初始化脚本
# 用途: 加载扩展和初始化数据库
# 版本: 2.0
# 日期: 2026-04-01
# ============================================================================

set -e

echo "============================================================================"
echo "PostgreSQL 初始化开始..."
echo "============================================================================"

# ----------------------------------------------------------------------------
# 1. 加载 pgvector 扩展（向量搜索）
# ----------------------------------------------------------------------------
echo ""
echo ">>> 步骤1: 加载 pgvector 扩展..."
psql -U postgres -c "CREATE EXTENSION IF NOT EXISTS vector;" && \
    echo "✓ pgvector 扩展加载成功" || \
    echo "✗ pgvector 扩展加载失败"

# ----------------------------------------------------------------------------
# 2. 加载 Apache AGE 扩展（图数据库）- 可选
# ----------------------------------------------------------------------------
echo ""
echo ">>> 步骤2: 加载 Apache AGE 扩展（如果已安装）..."

# 检查 AGE 是否已安装
if psql -U postgres -c "SELECT 1 FROM pg_available_extensions WHERE name = 'age';" | grep -q 1; then
    echo "检测到 Apache AGE 扩展，正在加载..."
    psql -U postgres -c "CREATE EXTENSION IF NOT EXISTS age;" && \
        echo "✓ Apache AGE 扩展加载成功" || \
        echo "✗ Apache AGE 扩展加载失败"
else
    echo "⚠ Apache AGE 扩展未安装，跳过加载"
    echo "  如需使用图数据库功能，请在 Dockerfile 中安装 Apache AGE"
fi

# ----------------------------------------------------------------------------
# 3. 重新加载 PostgreSQL 配置
# ----------------------------------------------------------------------------
echo ""
echo ">>> 步骤3: 重新加载 PostgreSQL 配置..."
pg_ctl reload && \
    echo "✓ PostgreSQL 配置重新加载成功" || \
    echo "✗ PostgreSQL 配置重新加载失败"

# ----------------------------------------------------------------------------
# 4. 显示已安装的扩展
# ----------------------------------------------------------------------------
echo ""
echo ">>> 步骤4: 显示已安装的扩展..."
psql -U postgres -c "SELECT extname, extversion FROM pg_extension WHERE extname IN ('vector', 'age') ORDER BY extname;"

echo ""
echo "============================================================================"
echo "PostgreSQL 初始化完成！"
echo "============================================================================"
echo ""
echo "已安装的扩展:"
echo "  - pgvector: 向量搜索支持"
if psql -U postgres -c "SELECT 1 FROM pg_available_extensions WHERE name = 'age';" | grep -q 1; then
    echo "  - Apache AGE: 图数据库支持"
fi
echo ""
echo "下一步:"
echo "  1. 运行 0_init.sql 创建 ClawAI 记忆系统表结构"
echo "  2. 验证表创建: psql -U postgres -d zsn_agentbrook_base -c '\\dt'"
echo "  3. 查看向量索引: psql -U postgres -d zsn_agentbrook_base -c '\\di'"
echo ""
echo "============================================================================"
