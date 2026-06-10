#!/usr/bin/env bash
#
# WireMock recording sanitize — recorded mapping dosyasindan hassas bilgileri
# temizler. record.sh otomatik olarak calistirir, manuel de cagrilabilir.
#
# Kullanim:
#   ./sanitize.sh <mapping-file.json>
#
# Temizlenen alanlar:
#   - Request headers:    Authorization, x-api-key, X-API-Key, Cookie, Set-Cookie
#   - Response headers:   Set-Cookie
#   - Response body:      "access_token", "refreshToken", "apiSecret", "password"
#                         field'larinin degerleri "REDACTED" olarak degistirilir
#
# Not: Body sanitize regex-based, mukemmel degil. Recorded JSON'u commit etmeden
# once manuel goz gezdirmek sart.

set -euo pipefail

if [ $# -ne 1 ]; then
    echo "Usage: $0 <mapping-file.json>"
    exit 1
fi

FILE="$1"

if [ ! -f "$FILE" ]; then
    echo "HATA: Dosya bulunamadı: $FILE"
    exit 2
fi

TMP=$(mktemp)
trap 'rm -f "$TMP"' EXIT

# Sensitive headers listesi
SENSITIVE_HEADERS=(
    "Authorization"
    "authorization"
    "x-api-key"
    "X-API-Key"
    "X-Api-Key"
    "Cookie"
    "cookie"
    "Set-Cookie"
    "set-cookie"
    "x-amz-access-token"
)

# Her mapping'i jq ile isle: request.headers ve response.headers'tan
# hassas alanlari temizle, response body icindeki token field'larini REDACTE et
JQ_FILTER='
def sanitize_headers:
    if . == null then .
    else
        with_entries(
            if (.key | ascii_downcase) as $lk |
               ($lk == "authorization" or $lk == "x-api-key" or $lk == "cookie" or
                $lk == "set-cookie" or $lk == "x-amz-access-token")
            then .value = "REDACTED"
            else .
            end
        )
    end;

def sanitize_body:
    if . == null or type != "string" then .
    else
        . | gsub("\"access_token\"\\s*:\\s*\"[^\"]*\""; "\"access_token\": \"REDACTED\"")
          | gsub("\"refresh_token\"\\s*:\\s*\"[^\"]*\""; "\"refresh_token\": \"REDACTED\"")
          | gsub("\"refreshToken\"\\s*:\\s*\"[^\"]*\""; "\"refreshToken\": \"REDACTED\"")
          | gsub("\"apiSecret\"\\s*:\\s*\"[^\"]*\""; "\"apiSecret\": \"REDACTED\"")
          | gsub("\"password\"\\s*:\\s*\"[^\"]*\""; "\"password\": \"REDACTED\"")
    end;

.mappings |= map(
    if .request and .request.headers then
        .request.headers = (.request.headers | with_entries(
            if (.key | ascii_downcase) as $lk |
               ($lk == "authorization" or $lk == "x-api-key" or $lk == "cookie" or
                $lk == "x-amz-access-token")
            then .value = {"equalTo": "REDACTED"}
            else .
            end
        ))
    else . end
    | if .response then
        .response.body = (.response.body // null | sanitize_body)
        | (if .response.headers then
             .response.headers = (.response.headers | with_entries(
                 if (.key | ascii_downcase) as $lk |
                    ($lk == "set-cookie")
                 then .value = "REDACTED"
                 else .
                 end
             ))
           else . end)
    else . end
)
'

jq "$JQ_FILTER" "$FILE" > "$TMP"
mv "$TMP" "$FILE"

echo "✓ Sanitized: $FILE"
echo ""
echo "ONEMLI: Commit etmeden once manuel goz gezdir!"
echo "  Kontrol edilecek alanlar: body icindeki hassas JSON field'lari"
echo "  Komut: jq '.mappings[] | .response.body' $FILE | head -50"
