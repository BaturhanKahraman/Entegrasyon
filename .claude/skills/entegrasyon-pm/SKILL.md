---
name: entegrasyon-pm
description: Entegrasyon platformunun ürün yönetimi — vizyon/misyon, domain, backlog ve spec şablonları. Kullanıcı isteğini veya saha araştırmasını gerçek, uygulanabilir task'lara çevirirken; docs/tasks/tasks.json backlog'unu düzenlerken; özellik önceliklendirirken; kabul kriteri/manuel test adımı yazarken; rakip/saha analizi yaparken kullan. Her task "gerçek hayatta işe yarar mı" testinden geçmeli.
---

# Entegrasyon — Product Management

Bu projede ürün kararları, backlog ve gereksinimler için referans. Hedef: **doğru işi tanımlamak** — her task gerçek esnafın gerçek derdini çözmeli.

## Vizyon & misyon

Tek bir yerden (`Entegrasyon.MVC`) bir esnafın ürününü girip **tüm pazaryerlerine + kendi e-ticaret sitesine + fiziksel mağaza satışına** dağıtabildiği, sonra hepsini tek panelden kontrol edebildiği merkezi çok-pazaryeri entegrasyon platformu (SaaS, multi-tenant).

- **İlk müşteri:** zekidsbebe.com (fiziksel mağaza + e-ticaret). Nebim + Ticimax dağınıklığı + kötü destek → bıkmışlar. İstek: tek yerden ürün ekle → tüm mağazalara gönder → tek panelden kontrol.
- **Parçalar:** `Entegrasyon.MVC` (esnaf paneli: ürün, fatura, tüm gereksinimler) · `Storefront` (müşteri e-ticaret sitesi; 2. müşteride iskelet + dizayn) · `Admin` (Baturhan'ın müşteri yönetim katı) · `Agent` (yazıcı/offline).
- **Kuzey yıldızı:** Esnaf 10 ayrı yere girip çile çekmesin. Üretilen her task gerçek hayatta işe yaramalı.

## Saha araştırması kaynakları

- `docs/rakip-analizi.md` — Türkiye + yurtdışı entegrasyon yazılımları, bizde olmayan özellikler, rekabet avantajı önerileri.
- `docs/production-audit.md` — canlıya çıkış öncesi eksikler.
- `docs/tasks/*.md` — mevcut planlar (import-export, lifecycle, matching, user-management).
- `docs/offline-satis-plani.md`, `docs/storefront-platform-spec.md`.
- Gerektiğinde web (`deep-research` skill'i) — gerçek pazaryeri kuralları, rakip özellikleri.

## Backlog — `docs/tasks/tasks.json` (MEVCUT ŞEMA, KORU)

```json
{
  "task": "Kısa net başlık",
  "description": "Ne ve neden — hangi esnaf derdini çözüyor",
  "status": "todo | in_progress | done | deferred | wont_do",
  "notes": "Karar gerekçesi / kapsam / neden wont_do",
  "priority": "high | medium | low",
  "plan": true,
  "manual_test_steps": ["Gerçek hayatta doğrulanabilir adım", "..."],
  "addOrUpdateUnitTests": true,
  "addOrUpdateIntegrationTests": true
}
```

- `manual_test_steps` **zorunlu** — "gerçekten işe yarıyor mu" testidir. Boşsa task eksik.
- `wont_do`/`deferred` task'larda `notes` mutlaka gerekçeli olsun (mevcut backlog'da örnekleri var — leaky abstraction, yüksek maliyet/düşük fayda vb.).

## Task yazma disiplini

Her task için:
1. **Problem:** Esnaf neyi yapamıyor / nerede çile çekiyor?
2. **Kullanıcı faydası:** Çözünce ne kazanıyor?
3. **Kabul kriteri:** Hangi gözlemlenebilir davranış "tamam" demek?
4. **Manuel test adımları:** Gerçek UI/akışta nasıl doğrulanır?
5. **Öncelik gerekçesi:** İlk müşteriyi (zekidsbebe) canlıya taşımaya hizmet mi?

## Önceliklendirme

`müşteri değeri × aciliyet ÷ efor`. İlk müşteriyi canlıya çıkaran ve "10 yere girme" derdini azaltan işler önde. "Hoş olur" özellikleri post-production'a ertele.

## Spec dokümanı (büyük işler)

Karmaşık iş → `docs/superpowers/specs/YYYY-MM-DD-<konu>-design.md` (mimari, bileşenler, veri akışı, test). `brainstorming` + `writing-plans` skill'leri.

## Sınırlar

- **Kod yazma, dosya düzenleme, test koşma yok** — sadece backlog + doküman.
- **Team Leader onay kapısı:** Saçma/kapsam dışı/mantıksız task geçmez. Onaysız task kesinleşmiş değildir.
- Belirsizlikte uydurma — netleştirici soru sor.
