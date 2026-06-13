# Kategori Eşleme Wizard'ı + Admin Geri-Besleme Döngüsü

**Tarih:** 2026-06-13
**Kapsam:** Kategori ↔ pazaryeri (Trendyol) eşleme akışı (modal → sayfa/wizard), değer eşleme düzeltmesi, eşleme silme (yetkili), ve admin tarafına geri-besleme döngüsü (master katalog promote)
**Durum:** Faz 1 uygulanabilir / Faz 2 tasarım

---

## Problem

Kategori/değer eşleme akışında dört kusur tespit edildi (kod doğrulandı):

1. **Modal kapanmıyor.** Kategori eşlemesi kaydedilince modal açık kalıyor. `CategorySyncController.CreateMapping` (`Features/MarketplaceSync/CategorySyncController.cs:55-100`) başarıda `refreshTable` + `showToast` trigger ediyor ama modal-close yok; form (`Views/CategorySync/Partials/_CategoryMappingTable.cshtml:92`) `hx-swap="none"` + `hx-on::after-request` yok → yanıt işlenmiyor.

2. **Parent/leaf körlüğü.** Trendyol yalnız **yaprak (leaf)** kategoriye ürün gönderir. Kullanıcı yanlışlıkla bir **parent** Trendyol kategorisine (ör. `Anne&Bebek&Çocuk>Oyuncak`, altında `Peluş Oyuncak` varken) eşleyebilir. Suggestion endpoint (`SuggestionsController.cs:30-44`) leaf/parent bilgisini expose etmiyor; uyarı altyapısı yok.

3. **Değer eşleme yanlış scope + yetersiz detay.** Değer arama `TrendyolAttributeCatalog` üzerinden çalışıyor (`SearchValuesAsync`, `TrendyolAttributeCatalog.cs:30-42`) ama katalog **tüm eşli kategorilerin** değerlerini **tek flat dict'te `attributeId` ile birleştiriyor** (`BuildCatalogAsync`, `TrendyolAttributeCatalog.cs:54-119`). Trendyol'da aynı attribute (ör. "Malzeme") farklı kategorilerde **farklı value seti** taşır → karışıyor, yanlış eşleşme riski. Ayrıca değer sadece adıyla gösteriliyor; hangi kategori/attribute bağlamına ait belli değil.

4. **Zorunlu özellik wizard'ı yok.** Backend zaten hesaplıyor (`CategoryMatchValidationService.cs:12-77` → `CategoryMatchValidationResultDto`: `TotalRequiredAttributes`/`MappedRequiredAttributes`/`Varianter`) ve `TrendyolMappingValidator.cs:10-65` eşli değilse publish'i **bloke ediyor** ("{n} zorunlu özellik eşleştirilmemiş"). Ama **hiçbir view bu DTO'yu tüketmiyor** — kullanıcı kategoriyi eşledikten sonra "bu kategorinin gönderim için neyi eksik" bilgisini görmüyor.

---

## Mevcut Durum (doğrulanmış altyapı)

| Yetenek | Durum | Konum |
|---|---|---|
| Kategori eşleme modal/save/remove | Var (modal kapatma bug'lı) | `Features/MarketplaceSync/CategorySyncController.cs`, `Views/CategorySync/` |
| Pazaryeri kategori arama | Var (in-memory flatten, DB tablosu yok) | `TrendyolMarketplaceSearchService.cs:27-54` |
| Değer arama → Trendyol stage API | Var (ama global-merge scope'lu) | `TrendyolAttributeCatalog.cs`, `TrendyolCategoryAttributeProvider.cs:19-53` |
| Kategori-bazlı attribute çekme | Var | `TrendyolCategoryAttributeProvider.GetAttributesForCategoryAsync` |
| Eşleşme tamlık hesabı | Var (UI tüketmiyor) | `CategoryMatchValidationService.cs` |
| Publish zorunlu-attr validasyonu | Var (bloke ediyor) | `TrendyolMappingValidator.cs` |
| `CategoryAttributeCategory.IsRequired/IsVarianter/IsSlicer` | Var | `Entity/Categories/CategoryAttributeCategory.cs` |
| Permission sistemi (policy-based) | Var | `ApplicationBootstrap/Security/AppPermissions.cs`, `PermissionGuardTagHelper` |
| **Master katalog (tam Trendyol eşleşmeli)** | **Var** | `AdminPanelDbContext` → `MasterCategory` + `Master*MarketplaceMapping` |
| **Master → Tenant kopya motoru** | **Var** | `MasterCatalogImportService.ImportFromMasterAsync` |
| Admin master katalog UI | Var | `AdminPanel/Features/MasterCatalog/` |
| Admin "hazır paket" (MatchedEntities) | Var (admin-curated, tenant değil) | `AdminPanel/Features/MatchedEntities/` |
| **Tenant → admin eşleşme aggregation / staging** | **YOK** (Faz 2'de kurulacak) | — |
| AdminPanel DB provider | **PostgreSQL** (sqlite değil) | `AdminPanel/Program.cs:13-17` |

---

## Tanımlar

| Terim | Anlam |
|---|---|
| **Leaf (yaprak) kategori** | Alt kategorisi yok. Trendyol'a ürün **yalnız** buraya gönderilir. |
| **Parent kategori** | Alt kategorisi var. Ürün gönderilemez. |
| **Adopt** | Master katalogdaki tam-eşli kategoriyi tenant'a kopyalama (`ImportFromMasterAsync`). Faz 1'de **otomatik değil** — sadece uyarı/not. |
| **Unmap** | Kategori↔pazaryeri eşlemesini bozma; cascade ile bağlı attribute/value match'leri de temizler. |

---

# FAZ 1 — Tenant Eşleme Wizard'ı (uygulanacak)

Modal kaldırılır; kategori eşleme **tam sayfa, adımlı (wizard) bir akış** olur. Tabler `steps` + `progress` + `status-dot` bileşenleri.

## 1.1 Sayfa iskeleti

Yeni route: `GET /marketplace/sync/categories/{categoryId}/map?mp={marketPlaceId}` → `CategoryMappingWizardController`.
Üç adımlı steps header; aktif adıma göre içerik partial swap (HTMX, `Request.IsHtmx()` → partial).

```
[1 Kategori Seç] —— [2 Zorunlu Özellikler] —— [3 Değerler] —— ✓ Gönderime Hazır
```

PRG: her save POST → TempData → redirect (geri tuşu güvenli). HTMX'te step partial swap + `status-dot` güncelle.

## 1.2 Adım 1 — Pazaryeri Kategorisi Seç

- Suggestion endpoint'e (`SuggestionsController.Categories`) **`isLeaf`** ve **`fullPath`** alanları eklenir. Kaynak: `TrendyolMarketplaceSearchService.SearchCategoriesAsync` flatten sırasında `subCategories.Count == 0` → `isLeaf=true`.
- **Leaf uyarısı (parent seçilirse):** Seçilen Trendyol kategorisi non-leaf ise inline `alert-warning`:
  > "Bu Trendyol kategorisinin alt kategorileri var. Trendyol yalnızca en alt (yaprak) kategoriye ürün göndermeye izin verir. Daha spesifik bir alt kategori seçmeniz önerilir."
  Engelleme değil — uyarı; kullanıcı yine de devam edebilir (business rule publish'te zaten yakalar).
- **Parent/leaf uyumsuzluğu:** Bizim kategori leaf + karşı parent → aynı uyarı. (Tek mekanizma: `targetIsLeaf` kontrolü.)
- **Master "hazır var" notu:** Seçilen Trendyol kategorisi için `MasterCategoryMarketplaceMapping`'te tam-eşli bir master kategori varsa, sayfada küçük bilgi notu:
  > "Bu kategori master katalogumuzda tüm özellik ve değerleriyle eşleşmiş olarak mevcut. İsterseniz manuel eşleme yerine hazır halini içe aktarabilirsiniz." (+ "İçe Aktar" linki → mevcut `ImportFromMasterAsync` akışı)

  **Faz 1'de otomatik adopt YOK** — sadece uyarı + not. Master stale olabilir; karar kullanıcıda.
- **Zaten-eşli guard:** Kategori bu pazaryerine zaten eşliyse re-map sunulmaz; bunun yerine "eşli" durumu + **Unmap** aksiyonu gösterilir (§1.5).

## 1.3 Adım 2 — Zorunlu Özellik Eşleme (wizard core)

- Kategori eşlendikten sonra `CategoryMatchValidationService.ValidateCategoryMatchAsync` çağrılır; DTO tüketilir.
- **Progress göstergesi:** "Zorunlu özellikler: M / N eşli" (Tabler `progress` + sayaç). Varianter için ayrı satır.
- Eşsiz zorunlu özellikler listelenir; her satırda `status-dot` (kırmızı=eşsiz, yeşil=eşli) + "Eşle" aksiyonu. Eşli olanlar yeşil.
- Hepsi eşlenince adım yeşile döner; "Değerlere geç" aktifleşir.

## 1.4 Adım 3 — Değer Eşleme (kategori-scoped + detaylı)

**Asıl bug fix burada.** Değer arama artık global katalogdan değil, **o kategoriye özel** çekilir:

- `SuggestionsController.AttributeValues` (`:62-79`) ve `TrendyolMarketplaceSearchService.SearchAttributeValuesAsync` (`:83-99`) **categoryId parametresi** alacak şekilde genişletilir.
- Değerler `TrendyolCategoryAttributeProvider.GetAttributesForCategoryAsync(marketplaceCategoryId)` ile **o kategorinin** attribute'undan filtrelenir — global `BuildCatalogAsync` merge'i **bu yol için kullanılmaz** (Malzeme çelişkisi biter).
- Cache: kategori-bazlı key (`TrendyolCategoryAttributes:{categoryId}`), tenant-scoped.
- **Detaylı gösterim:** her değer önerisinde `ad` + ait olduğu **kategori path** + **attribute adı** (gerekirse value id). TomSelect render template'i zenginleştirilir.

> Not: Global `TrendyolAttributeCatalog` tamamen kaldırılmıyor; arama bağlamı **kategori biliniyorsa** kategori-scoped provider'a yönlendirilir. Bağlamsız global arama (varsa) korunur ama wizard her zaman categoryId taşır.

## 1.5 Unmap (eşleme bozma) + Yetki

- Yeni aksiyon: `POST /marketplace/sync/categories/{categoryId}/unmap?mp={mp}` → `CategorySyncController` (mevcut `RemoveMapping` genişletilir/yeniden adlandırılır).
- **Cascade:** kategori↔pazaryeri match satırı + o kategoriye bağlı `CategoryAttributeMarketPlaceMatch` ve `CategoryAttributeValueMarketPlaceMatch` satırları (o marketplace için) silinir. Kategori ve kendi attribute/value tanımları **durur**. Orphan match kalmaz.
- **EF Mutasyon Persist (strict rule):** silme `ExecuteDelete` veya `AsTracking` + `Remove`. RED-first integration test (silmeden önce kayıt var, sonra yok).
- **Yetki:** yeni permission `AppPermissions.Categories.DeleteMapping = "Permissions.Categories.DeleteMapping"`:
  1. `AppPermissions.cs` → `Categories` class'ına const ekle
  2. `PermissionConstants.cs` mirror (test senkronizasyonu var)
  3. `GetAllPermissions()` listesine ekle
  4. Unmap action'a `[Authorize(Policy = AppPermissions.Categories.DeleteMapping)]`
  5. View'da `require-permission="Permissions.Categories.DeleteMapping"` ile buton gizle
  6. Policy registration + AdminPermissionSeeder otomatik
- **Onay:** Unmap geri-alınamaz cascade → UI'da onay modalı ("N özellik + K değer eşleşmesi de silinecek").

## 1.6 Backend değişiklik özeti (Faz 1)

| # | Değişiklik | Dosya |
|---|---|---|
| 1 | Suggestion endpoint `isLeaf`+`fullPath` | `SuggestionsController.cs`, `TrendyolMarketplaceSearchService.cs` |
| 2 | Modal→wizard sayfa + controller | yeni `CategoryMappingWizardController` + Views |
| 3 | Validation DTO'yu UI'da tüket (progress) | wizard step 2 + `CategoryMatchValidationService` (mevcut) |
| 4 | Değer arama categoryId-scoped | `SuggestionsController.AttributeValues`, `TrendyolMarketplaceSearchService.SearchAttributeValuesAsync` |
| 5 | Unmap cascade + persist | `CategorySyncController`, `AttributeMatchManager`/match repo |
| 6 | Permission `Categories.DeleteMapping` | `AppPermissions.cs`, `PermissionConstants.cs` |

## 1.7 TDD Test Planı (Faz 1)

- **Unit:** suggestion `isLeaf` hesabı; categoryId-scoped değer filtresi "Malzeme" çelişki senaryosu (iki kategori, aynı attribute adı, farklı value seti → doğru set döner); validation DTO progress sayacı.
- **Integration (Testcontainers):** unmap cascade — eşleme + attr/value match seed → unmap → tüm ilgili satırlar gitti, kategori durdu (RED-first); permission'sız kullanıcı unmap → 403; permission'lı → 200.
- **E2E (stage/prod, develop'ta değil):** wizard 3 adım happy-path; parent kategori seçince uyarı görünür; eşleme sonrası modal-yerine-sayfa kapanış davranışı.

---

# FAZ 2 — Admin Geri-Besleme Döngüsü (tasarım / spec)

**Amaç:** Tenant'ların yaptığı eşleşmeler admin panelde görünsün → admin kontrol etsin → doğruysa master kataloğa promote edilsin (Trendyol dışı platformlar dahil). Böylece master çok-platformlu zenginleşir; yeni müşteri hazır eşleşmelerle başlar.

## 2.1 Yazım-anı audit (seçilen mekanik)

DB-per-tenant izolasyonu var; admin tenant DB'lerini doğrudan okumuyor. Bu yüzden **read-time aggregation değil**, **write-time audit**:

- Tenant bir eşleme **kaydettiğinde** (kategori, attribute veya value match), ayrıca **paylaşımlı bir staging tablosuna** bir satır yazılır.
- Konum: `AdminPanelDbContext` (PostgreSQL, admin erişimli) içinde yeni `TenantMappingSubmission` tablosu. Tenant tarafından yazım: `IDbContextFactory<AdminPanelDbContext>` (MasterCatalogImportService'in zaten kullandığı pattern; cross-DB izinli).
- Şema (öneri):
  ```
  TenantMappingSubmission
    Id, TenantId, MarketPlaceId,
    SubmissionType  (Category | Attribute | Value),
    ApplicationName (tenant tarafı ad), ApplicationExternalKey,
    MarketPlaceExternalId, MarketPlaceName, MarketPlaceCategoryPath,
    ParentSubmissionRef (attribute/value'yu kategoriye bağlamak için),
    Status (Pending | Approved | Rejected | Promoted),
    CreatedAt, ReviewedByAdminId, ReviewedAt, ReviewNote,
    PayloadJson (tam bağlam snapshot'ı)
  ```
- Append-only; eşleme silinirse (unmap) ilgili submission `Status=Rejected/Withdrawn` veya yeni "removed" satırı (audit izi korunur).
- **Tenant izolasyonu:** submission satırı tenantId taşır; tenant'lar birbirinin submission'ını görmez; sadece admin görür.

## 2.2 Admin review UI

- `AdminPanel/Features/MappingReview/` — pending submission kuyruğu (filter: pazaryeri, tür, tenant, durum).
- Her submission detayında: tenant'ın eşlediği şey + master'da karşılığı var mı + "Onayla → Master'a promote" / "Reddet (+not)".
- Toplu trend görünümü: "şu Trendyol kategorisini N farklı tenant şöyle eşledi" (popüler/çelişen eşleşmeler).

## 2.3 Master'a promote (çok-platform)

- Onaylanınca submission → `MasterCategory`/`MasterAttribute`/`MasterAttributeValue` + ilgili `Master*MarketplaceMapping` upsert edilir.
- **Çok-platform:** Master şu an ağırlıklı Trendyol. Tenant başka platformu (Hepsiburada, PTT…) eşlediyse, promote o platformun `Master*MarketplaceMapping` satırını ekler → master katalog tek kategori için çok pazaryeri eşleşmesi taşır. Yeni müşteri tüm platformlar için hazır başlar.
- Mevcut ters-yön parçası (`MasterCatalogSeedService.ImportAttributesFromMainDbAsync:245`, raw SQL, yalnız template tenant) bu yapıyla **review-gate'li, çok-tenant** hale gelir.

## 2.4 Faz 2 açık konular

- Çelişki çözümü: iki tenant aynı kategoriyi farklı eşlerse admin hangisini master kabul eder? (oy/güven skoru veya manuel.)
- Master güncellenince mevcut tenant'lara yeniden-senkron önerisi gerekir mi? (out-of-scope, ayrı.)
- Submission yazımının ana transaction'ı yavaşlatmaması: outbox/async (proje zaten Channel→Outbox pattern'ine geçmiş — onu kullan).

---

## Skill Rehberi (adım-adım — doğru skill, doğru anda)

Skill'ler otomatik aktive olmaz; her adımda **açıkça** çağrılır. Wizard UI ağırlıklı → UI/UX skill'leri özellikle vurgulu.

### Keşif / araştırma (her adım öncesi)
- **`graphify`** — `graphify query "<soru>"` ham grep yerine (bağlam tasarrufu). Mevcut feature'ı bul/oku.
- **`microsoft-docs`** / **`context7`** — bilinmeyen ASP.NET Core / EF Core / HTMX davranışı; sürüme-özel doğrula, tahmin etme.

### UI / UX tasarım (Adım 5-8 — wizard, en kritik UI işi)
- **`ui-ux-pro-max`** — wizard akış planı: steps bileşeni, progress göstergesi, status-dot durumları, kategori/değer arama etkileşimi, empty/loading state, bilgi hiyerarşisi, mobil davranış. **Önce `plan`, sonra `build`.**
- **`impeccable`** — cilalama: bilişsel yük azaltma (3 adım net ayrım), uyarı/hata state'leri (leaf uyarısı, çelişki notu), a11y (keyboard nav, focus, ARIA), micro-interaction, UX copy (Türkçe diakritik tam). Wizard "tertemiz hissetsin" hedefi burada.
- **`tabler-ui`** — `steps`, `progress`, `status-dot`, `alert`, `badge`, `card`, `empty` için **kesin class kombinasyonu** (docs.tabler.io). Strict rule: class tahmin yok.
- **`aspnet-mvc-htmx`** — PRG, `Request.IsHtmx()` → step partial swap, feature-folder, tag-helper (`require-permission`), AutoValidationFilter, TempData/ViewData extensions.
- **`frontend-design`** — gerekirse ayırt edici, generic-AI olmayan görsel dil (Ürünler tasarım dili referans alınır).

### Backend / TDD (Adım 1-4)
- **`superpowers:test-driven-development`** — RED→GREEN zorunlu sıra (Malzeme-çelişki testi, unmap cascade persist testi RED-first).
- **`entegrasyon-db`** — match cascade silme (ExecuteDelete/AsTracking), `CategoryAttributeCategory` sorguları, multi-tenant izolasyon.
- **`postgres-performance`** — kategori-scoped değer fetch sorgusu + cache key, index kontrolü (hot path: suggestion arama).

### Doğrulama / review gate (atlanmaz)
- **`superpowers:requesting-code-review`** + **`ecc:csharp-reviewer`** (her C# değişikliği), **`ecc:database-reviewer`** (cascade/sorgu), **`ecc:security-reviewer`** (unmap yetki/mutasyon).
- **`chrome-devtools`** / **`playwright` MCP** + **`a11y-debugging`** — wizard'ı dev'de (`192.168.1.78:8085`) görsel + a11y doğrula (steps geçişi, uyarı görünürlüğü, klavye akışı).
- **`superpowers:verification-before-completion`** — done demeden test/build çıktısı kanıtı.

### Faz 2 (spec → uygulama zamanı)
- **`entegrasyon-pm`** — `TenantMappingSubmission` review akışı task'lara dökülürken.
- **`entegrasyon-db`** + **`postgres-performance`** — staging tablo şeması, cross-DB yazım (IDbContextFactory), outbox/async.

### Token yönlendirme
Mekanik/boilerplate (view iskeleti, partial kopya-uyarla, mapper) → yerel Ollama `entegrasyon-coder`. Opus düşünmeyi wizard akış/şema/cascade kararına ayır.

---

## Faz 1 Task Dağılımı (TDD-first)

1. **Suggestion `isLeaf`+`fullPath`** — unit RED→GREEN. (`SuggestionsController`, `TrendyolMarketplaceSearchService`)
2. **Değer arama categoryId-scoped + detaylı** — Malzeme-çelişki unit testi RED-first. (`SuggestionsController.AttributeValues`, `SearchAttributeValuesAsync`)
3. **Permission `Categories.DeleteMapping`** — mirror + GetAllPermissions + sync testi.
4. **Unmap cascade** — integration persist RED-first + 403/200 yetki testi. (`CategorySyncController`, match repo)
5. **Wizard sayfa iskeleti (3 adım, modal kaldır)** — controller + views + steps/progress. (`CategoryMappingWizardController`)
6. **Adım 1: kategori seç + leaf/parent uyarı + master notu + zaten-eşli guard.**
7. **Adım 2: zorunlu özellik progress (validation DTO tüketimi).**
8. **Adım 3: değer eşleme UI (kategori-scoped + detay render).**
9. **E2E happy-path + uyarı senaryosu** (stage/prod).

---

## Manuel Test Adımları (Faz 1, dev `192.168.1.78:8085`)

1. admin/123456789 ile giriş.
2. Pazaryeri Eşleme → Kategori sekmesi → bir kategoride "Eşle" → **modal değil, sayfa** açılır.
3. Adım 1: parent bir Trendyol kategorisi ara/seç → leaf uyarısı görünür. Master'da hazır olan bir kategoride bilgi notu görünür.
4. Zaten eşli bir kategoriye gir → re-map yerine "eşli" + "Eşlemeyi Kaldır" (yetkiliyse) görünür.
5. Adım 2: zorunlu özellik progress "M/N" doğru; eşsiz olanlar kırmızı status-dot.
6. Adım 3: bir attribute için değer ara → öneride **kategori path + attribute adı** görünür; farklı kategorideki aynı-adlı attribute'un değerleri **karışmaz**.
7. Eşleme tamamlanınca "Gönderime Hazır" yeşil durum.
8. Yetkisiz kullanıcıyla "Eşlemeyi Kaldır" butonu **görünmez**; doğrudan POST → 403.
9. Yetkiliyle unmap → onay modalı → silince kategori durur, attr/value eşleşmeleri gider.

---

# FAZ 3 — Kategori / Eşleşme / Ürün Yaşam Döngüsü (tasarım)

**Soru (kullanıcı):** Kategori silme/düzenlemede önceki kayıtlarımız (eşleşmeler **ve ürünler**) ne olacak? Ayrıca **karşı taraf (Trendyol kategorisi) parent'a dönüşürse** ne yapacağız?

**Mevcut koruma deseni:** soft-delete (`IsDeleted`/`DeletedAt` + query filter), ürün guard'ı (`SoftDelete` → `CategoryHasProducts` engeli). Geçmiş kaybolmaz, ürünlü kategori silinemez.

**Felsefe (mevcut desenle tutarlı):** *Veri kaybetme. Soft + guard + UYARI. Pazaryerindeki gönderilmiş ürüne otomatik dokunma (geri çekme riskli, kullanıcı kararı). Stale durumu sessizce silme — TESPIT ET + BİLDİR.*

## Senaryolar ve önerilen davranış

### 1. Bizim kategori SİLİNİRSE (soft-delete)
- **Ürünler:** ürün guard'ı korunur — ürünlü kategori silinemez (gönderilmiş ürünlü kategori de dolayısıyla silinemez). Kullanıcı önce ürünleri taşır/siler.
- **Eşleşmeler:** kategori `IsDeleted` olunca `CategoryMarketplace` + attribute/value match'leri **de soft-delete edilir** (orphan/stale kalmasın; katalog/validation görmez). Restore → eşleşmeler de restore edilebilir. *(Faz 1 unmap cascade mantığı yeniden kullanılır, hard yerine soft.)*
- **Pazaryeri:** Trendyol'daki ürüne dokunulmaz.

### 2. Eşleşme KALDIRILIRSA (unmap — Faz 1'de yapıldı)
- Orphan-safe attribute/value match temizliği ✓.
- **Eklenecek:** o kategoride **gönderilmiş ürün** (`ProductMarketplace.Status=Published`, `ExternalProductId` dolu) varsa unmap'te **UYAR**: "Bu kategoride N gönderilmiş ürün var; eşlemeyi kaldırırsanız bu ürünleri pazaryerinde güncelleyemezsiniz." Trendyol'daki ürün geri çekilmez (isteyene ayrı "arşivle/geri çek" aksiyonu — Trendyol `IsArchived` alanı var).

### 3. Bizim leaf → PARENT'a dönüşürse (alt kategori ekleme)
- `parentHasAttrs` guard'ı eşli kategorilerin çoğunu zaten blokluyor (eşli kategori genelde zorunlu attribute taşır).
- **Açık kenar durum:** eşli-ama-attribute'suz kategori parent olabilir → mapping stale (Trendyol non-leaf kabul etmez). **Davranış:** alt kategori eklerken o kategorinin **aktif eşleşmesi** varsa **UYAR** (otomatik unmap değil — kullanıcı kararı). Gönderilmiş ürün varsa uyarıyı güçlendir.

### 4. KARŞI taraf (Trendyol kategorisi) PARENT'a dönüşürse
- Trendyol "Oyuncak"a alt kategori ekler → eşleştiğimiz kategori artık **non-leaf** → gönderim reddedilir.
- **Tespit:** `MasterCatalogSyncService` günlük sync'te `MarketplaceReference.ParentExternalId` tutuyor → bir external id başka kategori tarafından parent olarak gösteriliyorsa o kategori non-leaf. Mapped tenant kategorilerinden Trendyol'da non-leaf'e dönenler tespit edilir.
- **Davranış:** eşleşmeyi **"geçersiz/uyarı" işaretle** + kullanıcıya **BİLDİRİM** ("Trendyol '{kategori}' kategorisi alt kategorilere bölündü; daha spesifik bir alt kategoriye yeniden eşleyin"). Otomatik silme yok. Cross-DB tespit → Faz 2 geri-besleme döngüsüyle aynı altyapı (admin sync → tenant bildirimi).

### 5. Ürün taşıma / kategori değişimi (edit)
- Ürünün kategorisi değişirse yeni kategorinin eşleşme/attribute'ları geçerli olmalı; gönderilmiş ürün ise re-validate gerekir. (Ürün-tarafı; ayrı ele alınır.)

## Uygulama fazlaması
- **Hemen (Faz 1 uzantısı):** (1) soft-delete eşleşmeleri de soft-temizlesin, (2) unmap'te sent-ürün uyarısı, (3) eşli kategoriye alt kategori eklerken uyarı.
- **Faz 2/3 (daha büyük):** Trendyol-taraf non-leaf tespiti + bildirim (sync + cross-DB), ürün geri-çek/arşivle aksiyonu, ürün kategori-değişim re-validate.
