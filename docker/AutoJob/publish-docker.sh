#!/bin/bash
# =========================================================
# Docker 部署发布脚本
# 用法: ./publish-docker.sh [Release|Debug]
# =========================================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SOLUTION_ROOT="$(cd "$SCRIPT_DIR/../../ZSN.Knowbase.Core" && pwd)"

CONFIG=${1:-Release}
PROJECT="$SOLUTION_ROOT/ZSN.AgentBrook.AutoJob/ZSN.AgentBrook.AutoJob.csproj"
OUTPUT="$SCRIPT_DIR/publish"
BROWSERS_DIR="$OUTPUT/browsers"
CHROMIUM_REV="1169"
# Playwright 1.52.0 默认使用 headless_shell，目录名为 chromium_headless_shell-{revision}
BROWSER_DIR_NAME="chromium_headless_shell-$CHROMIUM_REV"

echo ">>> 项目: $PROJECT"
echo ">>> 输出: $OUTPUT"
echo ">>> 发布 AutoJob (linux-arm64, $CONFIG) ..."
dotnet publish "$PROJECT" -r linux-arm64 -c "$CONFIG" -o "$OUTPUT" --self-contained false

if [ $? -ne 0 ]; then
    echo "!!! 发布失败"
    exit 1
fi

echo ">>> 验证 Playwright driver ..."
if [ -d "$OUTPUT/.playwright/node/linux-arm64" ]; then
    echo "    [OK] linux-arm64 driver 已包含"
else
    echo "    [WARN] linux-arm64 driver 未找到，Docker 中 Playwright 将无法工作"
fi

if [ -f "$OUTPUT/playwright.sh" ]; then
    echo "    [OK] playwright.sh 已包含"
else
    echo "    [WARN] playwright.sh 未找到"
fi

# =========================================================
# 下载 Playwright Chromium (避免 Docker 构建时 OOM)
# =========================================================
CHROMIUM_DIR="$BROWSERS_DIR/$BROWSER_DIR_NAME/chrome-linux"

if [ -f "$CHROMIUM_DIR/headless_shell" ]; then
    echo ">>> Chromium headless_shell 已存在，跳过下载"
else
    echo ">>> 下载 Playwright Chromium headless_shell (revision=$CHROMIUM_REV, linux-arm64) ..."
    mkdir -p "$CHROMIUM_DIR"
    CHROMIUM_URL="https://cdn.playwright.dev/dbazure/download/playwright/builds/chromium/$CHROMIUM_REV/chromium-headless-shell-linux-arm64.zip"
    ZIP_FILE="$BROWSERS_DIR/chromium-headless-shell-$CHROMIUM_REV.zip"

    curl -L --progress-bar -o "$ZIP_FILE" "$CHROMIUM_URL"

    if [ $? -ne 0 ]; then
        echo "!!! Chromium 下载失败"
        rm -f "$ZIP_FILE"
        exit 1
    fi

    echo ">>> 解压 Chromium ..."
    # 解压到临时目录，然后移动到正确位置
    TMP_DIR="$BROWSERS_DIR/tmp-chromium"
    mkdir -p "$TMP_DIR"
    unzip -q -o "$ZIP_FILE" -d "$TMP_DIR"

    # 解压后找到 headless_shell 二进制文件
    CHROME_FILE=$(find "$TMP_DIR" -name "headless_shell" -type f | head -1)
    if [ -z "$CHROME_FILE" ]; then
        # fallback: 找 chrome 二进制文件
        CHROME_FILE=$(find "$TMP_DIR" -name "chrome" -type f | head -1)
    fi
    if [ -n "$CHROME_FILE" ]; then
        CHROME_DIR=$(dirname "$CHROME_FILE")
        # 将整个 chrome 目录内容移到目标位置
        rm -rf "$CHROMIUM_DIR"
        mv "$CHROME_DIR" "$CHROMIUM_DIR"
        echo "    [OK] Chromium 已安装: $CHROMIUM_DIR/chrome"
    else
        echo "    [WARN] 解压后未找到 chrome 二进制文件"
    fi

    rm -rf "$TMP_DIR" "$ZIP_FILE"
fi

echo ">>> 发布完成: $OUTPUT"
