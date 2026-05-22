#!/bin/bash
# ============================================
#   ZSN.AgentBrook.Client macOS Publish
# ============================================

set -e

# ---- Configuration ----
API_URL="http://localhost:5003"
PUBLISH_DIR="../Publish/ClientApp"
# 代码签名身份:
#   "-"  = ad-hoc 签名（仅开发机可用，其他 Mac 需手动绕过 Gatekeeper）
#   "Developer ID Application: Your Company (TEAMID)" = 正式分发签名
CODESIGN_IDENTITY="-"
# Apple Team ID - 正式分发时用于公证(Notarization)，配合 CODESIGN_IDENTITY 使用
# APPLE_TEAM_ID=""
# APPLE_ID="your@email.com"
# ---- End Configuration ----

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CLIENT_APP="$SCRIPT_DIR/client-app"

echo "============================================"
echo "  ZSN.AgentBrook.Client macOS Publish"
echo "============================================"
echo ""

echo "[1/6] Checking environment..."
echo ""

command -v node >/dev/null 2>&1 || { echo "[ERROR] Node.js not found. Install Node.js 20+."; exit 1; }
echo "  Node.js: $(node -v)"

command -v dotnet >/dev/null 2>&1 || { echo "[ERROR] .NET SDK not found. Install .NET 10.0 SDK."; exit 1; }
echo "  .NET SDK: $(dotnet --version)"

if command -v rustc >/dev/null 2>&1; then
    echo "  Rust:    $(rustc --version)"
    HAS_RUST=1
else
    echo "  Rust:    NOT INSTALLED - Tauri desktop build will be skipped"
    HAS_RUST=0
fi
echo ""

echo "[2/6] Writing production API URL..."
echo "  VITE_API_BASE_URL=$API_URL/api"
cat > "$CLIENT_APP/.env.production" << EOF
VITE_API_BASE_URL=$API_URL/api
VITE_APP_TITLE=ZSN AgentBrook
VITE_APP_ID=
VITE_APP_SECRET=
EOF
echo "  Done."
echo ""

echo "[3/6] Installing frontend dependencies..."
cd "$CLIENT_APP"
rm -rf node_modules
npm install
echo ""

echo "[4/6] Building frontend..."
npm run build:web
echo "  Output: wwwroot/"
echo ""

echo "[5/6] Publishing .NET project (Web)..."
cd "$SCRIPT_DIR"
mkdir -p "$PUBLISH_DIR"
dotnet publish ZSN.AgentBrook.Client.csproj -c Release -o "$PUBLISH_DIR" --self-contained false
echo "  Done: $PUBLISH_DIR"
echo ""

if [ "$HAS_RUST" = "1" ]; then
    echo "[6/6] Building Tauri desktop app (macOS Universal)..."
    echo "  This may take several minutes on first build..."
    cd "$CLIENT_APP"

    # Check if both Rust targets are installed
    RUSTUP_AARCH64=$(rustup target list --installed | grep -c "aarch64-apple-darwin" || true)
    RUSTUP_X86=$(rustup target list --installed | grep -c "x86_64-apple-darwin" || true)
    if [ "$RUSTUP_AARCH64" -eq 0 ]; then
        echo "  Installing aarch64-apple-darwin target..."
        rustup target add aarch64-apple-darwin
    fi
    if [ "$RUSTUP_X86" -eq 0 ]; then
        echo "  Installing x86_64-apple-darwin target..."
        rustup target add x86_64-apple-darwin
    fi

    npm run tauri:build -- --target universal-apple-darwin

    DESKTOP_DIR="$SCRIPT_DIR/../Publish/ClientApp-Desktop"
    mkdir -p "$DESKTOP_DIR"

    APP_BUNDLE="src-tauri/target/universal-apple-darwin/release/bundle/macos/ZSN AgentBrook.app"
    if [ -d "$APP_BUNDLE" ]; then
        rm -rf "$DESKTOP_DIR/ZSN AgentBrook.app"
        cp -R "$APP_BUNDLE" "$DESKTOP_DIR/"
        echo "  Copied: ZSN AgentBrook.app (Universal)"

        if [ "$CODESIGN_IDENTITY" = "-" ]; then
            echo "  [WARNING] Using ad-hoc signing (only works on this machine)"
            echo "  For distribution, set CODESIGN_IDENTITY to your Developer ID"
            codesign --force --deep --sign - "$DESKTOP_DIR/ZSN AgentBrook.app" 2>&1
            echo "  Signed: ad-hoc (local only)"

            xattr -cr "$DESKTOP_DIR/ZSN AgentBrook.app" 2>/dev/null

            cat > "$DESKTOP_DIR/install-mac.command" << 'INSTALL_SCRIPT'
#!/bin/bash
# ======================================
#  ZSN AgentBrook macOS Install Helper
# ======================================
# Double-click this script to remove
# macOS security restrictions, then
# you can open the app normally.
# ======================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP_PATH="$SCRIPT_DIR/ZSN AgentBrook.app"

if [ ! -d "$APP_PATH" ]; then
    echo "Error: ZSN AgentBrook.app not found."
    echo "Please place this script in the same folder as the .app"
    read -p "Press Enter to exit..."
    exit 1
fi

echo "Removing security restrictions..."
xattr -cr "$APP_PATH"
codesign --force --deep --sign - "$APP_PATH" 2>/dev/null
echo ""
echo "Done! You can now open ZSN AgentBrook.app normally."
echo ""
read -p "Press Enter to exit..."
INSTALL_SCRIPT
            chmod +x "$DESKTOP_DIR/install-mac.command"
            echo "  Created: install-mac.command (helper for other Macs)"
        else
            codesign --force --deep --sign "$CODESIGN_IDENTITY" "$DESKTOP_DIR/ZSN AgentBrook.app" 2>&1
            echo "  Signed: $CODESIGN_IDENTITY"

            # Uncomment below for notarization (requires APPLE_TEAM_ID and APPLE_ID)
            # if [ -n "$APPLE_TEAM_ID" ] && [ -n "$APPLE_ID" ]; then
            #     echo "  Notarizing..."
            #     ditto -c -k --keepParent "$DESKTOP_DIR/ZSN AgentBrook.app" "$DESKTOP_DIR/ZSN AgentBrook.zip"
            #     xcrun notarytool submit "$DESKTOP_DIR/ZSN AgentBrook.zip" \
            #         --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" --wait
            #     xcrun stapler staple "$DESKTOP_DIR/ZSN AgentBrook.app"
            #     rm -f "$DESKTOP_DIR/ZSN AgentBrook.zip"
            #     echo "  Notarized and stapled."
            # fi
        fi
    fi

    # Copy DMG if exists
    DMG_FILE=$(find src-tauri/target/universal-apple-darwin/release/bundle -name "*.dmg" -type f 2>/dev/null | head -1)
    if [ -n "$DMG_FILE" ]; then
        cp "$DMG_FILE" "$DESKTOP_DIR/"
        echo "  Copied: $(basename "$DMG_FILE")"
    fi

    if [ "$CODESIGN_IDENTITY" = "-" ]; then
        echo ""
        echo "  ================================================"
        echo "  macOS Distribution Guide (No Certificate):"
        echo "  ------------------------------------------------"
        echo "  Option 1: Send DMG + tell users to run:"
        echo "    xattr -cr /Applications/ZSN\\ AgentBrook.app"
        echo ""
        echo "  Option 2: Send .app + install-mac.command"
        echo "    User double-clicks install-mac.command first,"
        echo "    then opens the app normally."
        echo ""
        echo "  Option 3: User right-clicks .app -> Open -> Open"
        echo "    (bypasses Gatekeeper on first launch)"
        echo "  ================================================"
    fi
    echo ""
else
    echo "[6/6] Skipping Tauri desktop build (Rust not installed)."
    echo "  Install Rust from https://rustup.rs/"
    echo ""
fi

echo "============================================"
echo "  macOS publish complete!"
echo ""
echo "  Web:     $SCRIPT_DIR/$PUBLISH_DIR"
echo "  Desktop: $SCRIPT_DIR/../Publish/ClientApp-Desktop/"
echo "  API:     $API_URL"
echo ""
echo "  Start Web:"
echo "    cd $PUBLISH_DIR"
echo "    dotnet ZSN.AgentBrook.Client.dll"
echo "    Visit: http://localhost:5006"
echo "============================================"
