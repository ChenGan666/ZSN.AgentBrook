# 使用官方的 MySQL 8 镜像作为基础镜像
FROM mysql:8

# 设置环境变量
ENV MYSQL_ROOT_PASSWORD=1q2w3e

# 在容器启动时，运行自定义初始化脚本（如果有）
COPY ./0_init.sql /docker-entrypoint-initdb.d/
COPY ./SQL/*.sql /docker-entrypoint-initdb.d/

# 暴露 MySQL 默认端口
EXPOSE 3306
