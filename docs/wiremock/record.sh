#!/usr/bin/env bash
#
# WireMock recording — gercek marketplace sandbox API'sine proxy olarak calisir
# ve gelen response'lari JSON fixture dosyasi olarak kaydeder.
#
# Kullanim:
#   ./record.sh <marketplace> <target-url>
#
# Ornek (Trendyol sandbox):
#   ./record.sh trendyol https://stageapigw.trendyol.com
#
# On kosul:
#   - docker-compose.dev.yml calisiyor olmali (WireMock container aktif)
#   - WireMock admin UI erisilebilir: http://localhost:8080/__admin
#   - jq kurulu olmali
#
# Akis:
#   1. WireMock'a proxy mapping kaydedilir — tum istekleri <target-url>'e forward eder
#   2. Gelistirici MVC uygulamasindan marketplace akislarini tetikler
#   3. Enter'a basilinca WireMock snapshot alinir, JSON'a kaydedilir
#   4. sanitize.sh otomatik calistirilir — Authorization header'lari temizlenir
#
# Cikti:
#   docs/wiremock/mappings/<marketplace>/recorded-YYYYMMDD-HHMMSS.json

set -euo pipefail

if [ $# -ne 2 ]; then
    echo "Usage: $0 <marketplace> <target-url>"
    echo "Example: $0 trendyol https://stageapigw.trendyol.com"
    exit 1
fi

MARKETPLACE="$1"
TARGET="$2"
ADMIN="${WIREMOCK_ADMIN:-http://localhost:8080/__admin}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/mappings/$MARKETPLACE"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
OUTPUT_FILE="$OUTPUT_DIR/recorded-$TIMESTAMP.json"

# WireMock ayakta mi kontrol et
if ! curl -sf "$ADMIN/health" > /dev/null 2>&1; then
    echo "HATA: WireMock admin API erisilemez ($ADMIN)"
    echo "docker-compose.dev.yml calistiriliyor mu?"
    exit 2
fi

# Output directory olustur
mkdir -p "$OUTPUT_DIR"

echo "→ Proxy mapping kaydediliyor: $TARGET"

curl -sf -X POST "$ADMIN/mappings" \
    -H "Content-Type: application/json" \
    -d "{
        \"priority\": 10,
        \"request\": { \"urlPathPattern\": \".*\" },
        \"response\": { \"proxyBaseUrl\": \"$TARGET\" }
    }" > /dev/null

echo "✓ Proxy aktif: $TARGET"
echo ""
echo "Simdi MVC uygulamasinda marketplace akislarini tetikle:"
echo "  - http://localhost:5100 → Urun listeleme, siparis sync vs."
echo "  - WireMock tum request'leri kaydedecek"
echo ""
read -p "Akis tamamlandiysa Enter'a bas — snapshot alinacak... "

echo ""
echo "→ Snapshot aliniyor..."

# Snapshot — WireMock kaydedilen istek/response ciftlerini mapping JSON'a cevirir
curl -sf -X POST "$ADMIN/recordings/snapshot" \
    -H "Content-Type: application/json" \
    -d '{ "outputFormat": "FULL", "persist": "false" }' \
    | jq '.' > "$OUTPUT_FILE"

RECORDED_COUNT=$(jq '.mappings | length' "$OUTPUT_FILE")

echo "✓ $RECORDED_COUNT mapping kaydedildi: $OUTPUT_FILE"
echo ""
echo "→ Credential'lar temizleniyor (sanitize.sh)..."

"$SCRIPT_DIR/sanitize.sh" "$OUTPUT_FILE"

echo ""
echo "✓ Recording tamamlandi."
echo ""
echo "Sonraki adim: Fixture dosyasini integration test stub'larinda kullan:"
echo "  - $OUTPUT_FILE"
echo "  - Ilgili XxxStubs.cs sinifindaki TODO'lari doldur"
echo "  - Gerekirse mapping'leri kucuk parcalara bol (endpoint basina ayri dosya)"
