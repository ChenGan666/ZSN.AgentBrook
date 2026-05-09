@echo off
REM ============================================================================
REM PostgreSQL 镜像构建脚本
REM 版本: 2.0
REM 日期: 2026-04-01
REM ============================================================================

echo ============================================================================
echo PostgreSQL + pgvector + Apache AGE 镜像构建
echo ============================================================================
echo.

REM 切换到docker目录
cd /d "%~dp0"

echo [1/3] 清理旧镜像...
docker rmi zsn-postgres:16.6 2>nul
echo.

echo [2/3] 开始构建镜像（包含Apache AGE，预计5-10分钟）...
echo.
docker compose build --no-cache postgres

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================================================
    echo [3/3] 构建成功！
    echo ============================================================================
    echo.
    echo 下一步:
    echo   1. 启动服务: docker compose up -d postgres
    echo   2. 查看日志: docker compose logs -f postgres
    echo   3. 验证扩展: docker compose exec postgres psql -U postgres -c "\dx"
    echo.
) else (
    echo.
    echo ============================================================================
    echo 构建失败！
    echo ============================================================================
    echo.
    echo 可能的原因:
    echo   1. 网络问题 - 无法访问GitHub
    echo   2. Docker服务未启动
    echo   3. 磁盘空间不足
    echo.
    echo 解决方案:
    echo   1. 检查网络连接
    echo   2. 启动Docker Desktop
    echo   3. 清理Docker缓存: docker system prune -a
    echo.
    echo 如果网络问题持续，可以切换到方案A（只安装pgvector）:
    echo   编辑 postgres/Dockerfile，注释方案B，启用方案A
    echo.
)

pause
