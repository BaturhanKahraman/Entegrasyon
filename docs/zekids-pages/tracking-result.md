---
title: Kargo Takip Sonuç
target_view: Views/Tracking/Result.cshtml
controller_action: Tracking/Result
route: /kargo-takip/{kod}
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: high
---

# Kargo Takip Sonuç

**Bağlam (tek paragraf):** Kargo takip sonucu — üst durum kartı + dikey timeline + detaylı hareketler tablosu. Ortalı max-w-3xl. Sample data: ZB-2026-1234, Kargoda, Aras 1234567890123, tahmini 4 Haziran, 5 hareket.

## Tasarım istekleri
- Ortalı `max-w-3xl`.
- **Üst durum kartı** `bg-cream rounded-3xl p-8`: carrier logo (Aras) + takip no mono + "Tahmini: 4 Haziran Çarşamba"; büyük durum chip "Kargoda" `bg-accent/25 text-warning`.
- **Timeline** dikey büyük card `bg-white border`: 6 step (Sipariş Alındı 24 May 14:32 → Onaylandı 24 May 15:01 → Hazırlanıyor 25 May 09:15 → Kargoya Verildi 26 May 11:30 → Dağıtım Merkezi 27 May 06:45 AKTİF pulse → Teslim —); her step yuvarlak ikon + bg renkli (tamamlanmış success, aktif primary pulse, sonraki muted); yanında lokasyon + saat.
- **Detaylı hareketler** card `border divide-y`: 5 satır timestamp + lokasyon + açıklama.
- 2 CTA: "Kargocuyu Ara" outline + "Sipariş Detayına Git".
- Bulunamadıysa empty state: "Kayıt bulunamadı, kodu kontrol edin".

## Sample data ipucu
- ZB-2026-1234, Aras, tahmini 4 Haziran, 6 step timeline + 5 hareket satırı. Model yok → ViewBag/inline.

## JS etkileşim ipucu
- Statik ağırlıklı; aktif step pulse CSS. Empty state ayrı blok.

## Claude design'a yapıştırılabilir prompt

```
Kalan utility sayfalarından birini tasarla.

Tracking/Result — Sonuç
Sample data: ZB-2026-1234, Kargoda, Aras 1234567890123, tahmini 4 Haziran, 5 hareket kaydı.
- Ortalı max-w-3xl
- **Üst durum kartı** bg-cream rounded-3xl p-8:
  - Carrier logo (Aras) + takip no mono + "Tahmini: 4 Haziran Çarşamba"
  - Büyük durum chip "Kargoda" bg-accent/25 text-warning
- **Timeline** dikey büyük card bg-white border:
  - 6 step: Sipariş Alındı (24 May 14:32) → Onaylandı (24 May 15:01) → Hazırlanıyor (25 May 09:15) → Kargoya Verildi (26 May 11:30) → Dağıtım Merkezi (27 May 06:45, AKTİF pulse) → Teslim (—)
  - Her step yuvarlak ikon + bg renkli (tamamlanmışlar success, aktif primary pulse, sonraki muted)
  - Step yanında: lokasyon + saat
- **Detaylı hareketler** card border divide-y:
  - 5 satır: timestamp + lokasyon + açıklama
- 2 CTA: "Kargocuyu Ara" outline + "Sipariş Detayına Git"
- Bulunamadıysa: empty state "Kayıt bulunamadı, kodu kontrol edin"
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/KargoTakipSonuc.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
