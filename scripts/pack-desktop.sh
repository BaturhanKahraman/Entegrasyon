#!/usr/bin/env bash
# Entegrasyon.Desktop için Velopack installer üretir.
# Kullanım:
#   scripts/pack-desktop.sh <version> [<channel>]
# Örnek:
#   scripts/pack-desktop.sh 1.0.0 stable

set -euo pipefail

VERSION="${1:?Sürüm parametresi gerekli, ör: 1.0.0}"
CHANNEL="${2:-stable}"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DESKTOP_DIR="$ROOT/Application/Entegrasyon.Desktop"
PUBLISH_DIR="$ROOT/bin/publish/Entegrasyon.Desktop"
RELEASE_DIR="$ROOT/bin/Releases"

PACK_ID="Entegrasyon.Desktop"
PACK_TITLE="Entegrasyon Masaüstü"

if ! command -v vpk >/dev/null 2>&1; then
    echo "→ vpk bulunamadı, kuruluyor: dotnet tool install -g vpk"
    dotnet tool install -g vpk
    export PATH="$HOME/.dotnet/tools:$PATH"
fi

RUNTIME="${VPK_RUNTIME:-linux-x64}"
case "$(uname -s)" in
    Darwin*) RUNTIME="${VPK_RUNTIME:-osx-x64}" ;;
    MINGW*|MSYS*|CYGWIN*) RUNTIME="${VPK_RUNTIME:-win-x64}" ;;
esac

echo "→ Publish: $RUNTIME"
rm -rf "$PUBLISH_DIR"
dotnet publish "$DESKTOP_DIR/Entegrasyon.Desktop.csproj" \
    -c Release \
    -r "$RUNTIME" \
    --self-contained true \
    -o "$PUBLISH_DIR" \
    -p:Version="$VERSION" \
    -p:PublishSingleFile=false

echo "→ vpk pack: $VERSION ($CHANNEL)"
mkdir -p "$RELEASE_DIR"
vpk pack \
    --packId "$PACK_ID" \
    --packVersion "$VERSION" \
    --packDir "$PUBLISH_DIR" \
    --packTitle "$PACK_TITLE" \
    --channel "$CHANNEL" \
    --outputDir "$RELEASE_DIR" \
    --mainExe "Entegrasyon.Desktop$( [[ "$RUNTIME" == win-* ]] && echo .exe )"

echo "✓ Installer hazır: $RELEASE_DIR"
ls -lh "$RELEASE_DIR" | tail -10
