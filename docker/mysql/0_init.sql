-- 创建多个数据库
CREATE DATABASE IF NOT EXISTS zsn_knowbase;

-- 创建用户并设置密码
CREATE USER 'zsn_knowbase'@'%' IDENTIFIED BY 'hE6y5WLbdjDR52x7';

-- 为用户授予权限
GRANT ALL PRIVILEGES ON zsn_knowbase.* TO 'zsn_knowbase'@'%';

-- 刷新权限，使修改生效
FLUSH PRIVILEGES;