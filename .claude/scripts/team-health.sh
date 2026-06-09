#!/usr/bin/env bash
# Entegrasyon takım sağlık kontrolü.
# Kullanım: team-health.sh [beklenen-isim ...]
#   ör: team-health.sh SWE-Ahmet SWE-Mehmet QA
# "idle (canlı, mesaj bekliyor)" ile "ölü (süreç yok)" ayrımını netleştirir.
# Idle teammate süreçte GÖRÜNÜR; süreç listede yoksa teammate ÖLMÜŞTÜR.
set -u

echo "=== Canlı teammate süreçleri ==="
live=$(ps aux | grep -oE 'agent-name (SWE-[A-Za-z]+|QA|DB|PM)[A-Za-z-]*' | sed 's/agent-name //' | sort -u)
[ -n "$live" ] && echo "$live" || echo "  <canlı teammate yok>"

if [ "$#" -gt 0 ]; then
  echo ""
  echo "=== Beklenen roster durumu ==="
  dead=""
  for name in "$@"; do
    if printf '%s\n' "$live" | grep -qx "$name"; then
      echo "  ✓ $name canlı"
    else
      echo "  ✗ $name ÖLÜ — TL yeniden doğurmalı (bağlamı + peer cevaplarını gömerek)"
      dead="$dead $name"
    fi
  done
  [ -n "$dead" ] && echo "" && echo "AKSIYON: şu teammate(ler) ölü:$dead"
fi

echo ""
echo "=== tmux swarm panelleri ==="
sock=$(ps aux | grep -oE 'claude-swarm-[0-9]+' | sort -u | head -1)
if [ -n "${sock:-}" ]; then
  tmux -L "$sock" list-panes -t claude-swarm \
    -F '  panel #{pane_index}: #{pane_title} (active=#{pane_active} dead=#{pane_dead})' 2>/dev/null \
    || echo "  panel okunamadı"
  echo "  (izlemek için: tmux -L $sock attach -t claude-swarm)"
else
  echo "  tmux swarm oturumu yok"
fi
