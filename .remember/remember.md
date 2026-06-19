# Handoff — 2026-06-19 (otonom oturum 2, "hepsini hallet")

## Bu oturumda yapılan (T109–T119 backlog kapatma)
Kullanıcı: "hepsini hallet, kendi testlerin için Chrome kullanabilirsin (kaydet), önemli tasklarda doğrulama yap."

**5 commit (develop, origin dual-pushurl → GitHub+gitea → dev deploy):**
1. `87c111e2` T110/T112/T109: Trendyol gönderim P0 düzeltmeleri
   - T110: MarketplaceContentTransformer + RuleProvider TrendyolProductMapper'a wire — Trendyol=PlainText, HTML auto-strip, limitler merkezî kural profilinden (magic-number kaldırıldı).
   - T112: GetSendPreflightAsync görsel kontrolü (AllVariantsHaveImages + VariantsWithoutImages, AllPassed bloklar) + TrendyolSend.cshtml "Görsel Durumu" satırı.
   - T109: mapper quantity:0 uyarı log + preflight StockSourceConfigured + DevWireMockSeeder dev'de ilk depoyu varsayılan stok kaynağı işaretler.
   - **Pre-existing 4 integration fail DÜZELTİLDİ:** SeedMarketPlaceAsync dummy credential (credential guard); GetTenantScopedService helper (servis-seviyesi testte tenant init — "Tenant context is not initialized"); stale "beden"→"Beden" assertion.
2. `a0b6a833` T111: FlexibleDecimalModelBinder (Türkçe+invariant para parse, decimal/decimal? global, 16 test). JS fix d992b002 korunuyor.
3. `f87d5d6e` T113/T119/T116/T117: SyncDetail gerçek durum + tüm pazaryeri dinamik + retry endpoint; health LogAction.None→"—" + TR etiket; SaveChanges Added'da UpdatedAt=CreatedAt.
4. `19d2f6d8` T115/T116: send-flow+sync diakritik + tasks.json durum.

**Testler:** 1825 unit + 16 integration (ProductSend+SyncPage) YEŞİL. SSH docker tüneli (`/tmp/docker-server.sock`) bu oturumda ÇALIŞTI — integration koşuldu.

## tasks.json durumu
- DONE: T109, T110, T111, T112, T113, T114(prev), T116, T117
- KISMİ: T115 (send-flow+sync diakritik yapıldı; Storefront/Settings/Orders/BranchOffices kozmetik kaldı ~20 dosya), T119 (SyncDetail tüm pazaryeri görünür; generic SEND formu N11/Pazarama/… henüz yok)
- AÇIK/DÜŞÜK: T118 (edit file input zaten class="form-control"=Tabler-styled; full dropzone paritesi opsiyonel)

## Sıradaki
1. Chrome canlı doğrulama (deploy sonrası): HTML açıklama → WireMock payload düz metin mi? preflight görsel/stok-kaynağı satırları? SyncDetail durum? fiyat ×10 yok?
2. T115 kalan diakritik sweep (Storefront/Settings — kozmetik, dikkatli; Razor ifadelerini bozmadan).
3. T119 generic marketplace send formu (T110 kural profiliyle dinamik).
4. T116 sayım uyumsuzluğu (health "Son 24S Hata" vs dashboard "Trendyol HATA") — iki query kaynağı birleştir (deferred).

## Notlar
- `git push origin develop` → GitHub+gitea ikisine gider (origin dual-pushurl), dev deploy tetiklenir.
- Hafıza: [[chrome-for-self-verification]], [[currency-imask-prefill-10x]], [[entegrasyon-trendyol-send-flow-gaps]].
- "Başarılı resend sonrası Trendyol: Hatalı" — WireMock batch-status mock reject dönüyor olabilir (incelenmedi).
