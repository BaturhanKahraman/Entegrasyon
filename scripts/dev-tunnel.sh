#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Local debug SSH tüneli — Entegrasyon
#
# KURAL 1: Yerel makinede HİÇ container çalıştırma. Yerel sadece `dotnet run`.
# Bu script, yerel `dotnet run` debug'ının SERVER'daki servislere güvenli
# (postgres'i LAN'a AÇMADAN) erişmesini sağlar:
#
#   • postgres_db  (server 127.0.0.1:5432, LAN'a kapalı) → localhost:5432
#   • minio_storage(server 127.0.0.1:9000, LAN'a kapalı) → localhost:9000  (opsiyonel, görsel)
#
# WireMock'a tünel GEREKMEZ: server'da LAN portu 8091'de yayında →
# DevMode:WireMockUrl=http://192.168.1.78:8091 ile doğrudan erişilir.
#
# Tünel açıkken appsettings.Development.json'da Host=localhost kullan
# (Host=192.168.1.78 ÇALIŞMAZ — postgres LAN'a kapalı). Bkz. README.
#
# Kullanım:
#   ./scripts/dev-tunnel.sh          # tüneli aç (foreground; Ctrl-C ile kapat)
#   ./scripts/dev-tunnel.sh --bg     # arka planda aç
#   ./scripts/dev-tunnel.sh --stop   # arka plan tünelini kapat
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

SSH_HOST="${ENTEGRASYON_SSH_HOST:-server}"   # ~/.ssh/config'teki 'server' (192.168.1.78)
PIDFILE="/tmp/entegrasyon-dev-tunnel.pid"

# -L <localport>:<server-içi-host:port>  (server üzerinden 127.0.0.1'e bağlanır)
TUNNELS=(
  "5432:127.0.0.1:5432"   # postgres_db → IntegrationDb (local debug)
  "9000:127.0.0.1:9000"   # minio_storage (görsel önizleme; istemezsen sil)
)

build_args() {
  local args=()
  for t in "${TUNNELS[@]}"; do args+=( -L "$t" ); done
  printf '%s\n' "${args[@]}"
}

case "${1:-}" in
  --stop)
    if [[ -f "$PIDFILE" ]] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
      kill "$(cat "$PIDFILE")" && rm -f "$PIDFILE"
      echo "Tünel kapatıldı."
    else
      echo "Aktif arka plan tüneli yok."
    fi
    ;;
  --bg)
    mapfile -t A < <(build_args)
    ssh -nNT "${A[@]}" "$SSH_HOST" &
    echo $! > "$PIDFILE"
    echo "Tünel arka planda açıldı (PID $(cat "$PIDFILE")). Kapatmak için: $0 --stop"
    echo "  postgres → localhost:5432   minio → localhost:9000   WireMock → 192.168.1.78:8091"
    ;;
  *)
    mapfile -t A < <(build_args)
    echo "Tünel açılıyor (Ctrl-C ile kapat):"
    echo "  postgres → localhost:5432   minio → localhost:9000   WireMock → 192.168.1.78:8091"
    exec ssh -nNT "${A[@]}" "$SSH_HOST"
    ;;
esac
