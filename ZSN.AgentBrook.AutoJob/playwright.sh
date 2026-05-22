#!/bin/bash
DIR="$(cd "$(dirname "$0")" && pwd)"
export PLAYWRIGHT_DRIVER_SEARCH_PATH="$DIR"
exec dotnet "$DIR/Microsoft.Playwright.dll" "$@"
