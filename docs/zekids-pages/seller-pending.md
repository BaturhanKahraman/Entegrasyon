---
title: Satıcı Onay Bekliyor
target_view: Views/Seller/Pending.cshtml
controller_action: Seller/Pending
route: /satici/bekliyor
model: None (sample data ile)
status: pending
source_html: henüz tasarlanmadı
priority: low
---

# Satıcı Onay Bekliyor

**Bağlam (tek paragraf):** Satıcı başvurusu sonrası inceleme bekleme sayfası — yatay stepper + bilgilendirme. Storefront chrome aynı. Sample data: ZekidsBebe'ye 25 Mayıs başvuru, 2-3 iş günü inceleme.

## Tasarım istekleri
- Ortalı `card max-w-2xl py-16`.
- Üstte yumuşak saat ikonu `bg-secondary/15`.
- H1 "Başvurunuz Alındı".
- "Mağaza başvurunuzu inceliyoruz. Genellikle 2-3 iş günü içinde sonuçlanır."
- 4 adım yatay stepper: Başvuru Alındı (check) → İnceleniyor (aktif pulse) → Onay → Mağaza Aktif.
- 2 CTA: "Bilgilerimi Güncelle" outline + "İletişim" outline.
- Alt: "Bu süreçte bizi tanıyın — Satıcı Rehberi" link card.

## Sample data ipucu
- Başvuru tarihi 25 Mayıs; model yok → ViewBag/inline.

## JS etkileşim ipucu
- Statik; aktif step pulse CSS. Ekstra JS yok.

## Claude design'a yapıştırılabilir prompt

```
Çok satıcılı (marketplace) tarafının bir sayfasını tasarla. Chrome aynı. Küçük bir ekibin küçük admin paneli — minimal, fonksiyonel, kid-brand'a uygun.

Seller/Pending — Onay bekliyor
Sample data: ZekidsBebe'ye 25 Mayıs'ta başvuru, 2-3 iş günü inceleme.
- Ortalı card max-w-2xl py-16
- Üstte yumuşak saat ikonu bg-secondary/15
- H1 "Başvurunuz Alındı"
- "Mağaza başvurunuzu inceliyoruz. Genellikle 2-3 iş günü içinde sonuçlanır."
- 4 adım stepper yatay: Başvuru Alındı (check) → İnceleniyor (aktif pulse) → Onay → Mağaza Aktif
- 2 CTA: "Bilgilerimi Güncelle" outline + "İletişim" outline
- Alt: "Bu süreçte bizi tanıyın — Satıcı Rehberi" link card
```

## Çıktı geldiğinde

1. HTML'i `İndirilenler/zekids/SaticiBekliyor.html` olarak indir.
2. Razor + JS çevirisi için uygun `agent-...` ajanına ver.
3. View üretildikten sonra bu MD'yi sil.
</content>
