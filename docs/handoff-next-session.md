# Yeni Session Handoff — Entegrasyon TL

> Bu dosyayı yeni bir temiz session'a **kopyala-yapıştır**. Önceki oturum maliyet/context nedeniyle kapandı.

## Sen kimsin
Entegrasyon (çok-pazaryeri e-ticaret entegrasyon platformu; ilk müşteri zekidsbebe.com) geliştirme takımının **Team Leader**'ısın. ASP.NET Core MVC + Razor + Tabler UI + HTMX + EF Core/PostgreSQL, katmanlı mimari.

## Çalışma modeli (ZORUNLU — geçen oturumda oturdu)
- **Ephemeral background sub-agent:** Her bağımsız görev için `Agent(run_in_background:true, isolation:"worktree")` ile bir agent doğ → işini yap → öl. Kalıcı tmux teammate KULLANMA (token yer). `takim-baslat` komutu hâlâ tmux diyor — bayat (aşağıda P0).
- **Push yetkisi sende (TL gate):** Agent'lar push ETMEZ; worktree'de bırakır. Sen diff'i incele → build+test yeşil → **gitea + origin İKİSİNE** push → worktree temizle (`git worktree remove --force`).
- **Agent tipleri:** Implementasyon için ECC `ecc:tdd-guide` VEYA proje `swe-entegrasyon` (kuralları taşır, daha güvenli); gate için ECC `ecc:csharp-reviewer`/`ecc:database-reviewer`/`ecc:security-reviewer`. ECC `tdd-guide` keşiften sonra erken durabiliyor → boş worktree görürsen `SendMessage(agentId, "planını uygula, bitir")` ile devam ettir.
- **Token #1 öncelik.** Maliyet kritik. Mekaniği Ollama'ya (`entegrasyon-coder`) offload et; graphify query > ham grep; gereksiz paralel agent yok.
- **Container politikası:** Yerel makinede container YOK. Testcontainers → server Docker socket-tünel: `ssh -fNT -L /tmp/docker-server.sock:/var/run/docker.sock server` + `DOCKER_HOST=unix:///tmp/docker-server.sock TESTCONTAINERS_HOST_OVERRIDE=192.168.1.78 TESTCONTAINERS_RYUK_DISABLED=true`.
- **GateGuard fact-force hook:** Her Edit/Write öncesi 4-maddelik beyan ister; beyanı yaz, aynı edit'i tekrar dene (batch edit'lerde hepsi bloklanır → tek mesajda retry).
- **EF footgun:** Global NoTracking — mutasyon yapacaksan `.AsTracking()`/`context.Update()` şart yoksa SaveChanges sessiz no-op.

## Mevcut durum
- `develop` = **1f45a1e7** (gitea = origin senkron). Yerel temiz (sadece `.remember/remember.md`).
- **Bu oturumda BİTEN:** E6 Amazon fiyat override (`b4f72bd5`), S1 sipariş iptali stok event (`043b454b`), S2 checkout overselling kritik (`d17d843b`) — hepsi RED→GREEN+gate. 6 agent'a ECC cephanesi (`791e4c4f`). designer→tabler-ui (`730ba2ff`). tasks.json E6/S1/S2 done (`9fdf05e1`). CLAUDE.md ECC verimli delegasyon ilkesi (`2d2af09a`). tabler-ui skill (`1f45a1e7`).

## Yapılacaklar (öncelik sırası)

### P0 — Süreç/altyapı
1. **`takim-baslat` komutunu ephemeral modele çevir** — `.claude/commands/takim-baslat.md` hâlâ "Agent ile canlı (tmux) başlat" + TeamCreate diyor. Yukarıdaki ephemeral background-subagent + TL-gate+push modeliyle yeniden yaz.
2. **ECC kurulum dedup kontrolü** — plugin VEYA manuel, ikisi üst üste değil (duplicate-hook riski; README'nin en sık bozuk kurulumu).

### P1 — 8085 feedback (doğrula/bitir)
3. **#7 Vergi varsayılan** — `VatRateManager.SetDefault` bu oturumda düzeltildi (AsTracking + ExecuteUpdate filter); 8085'te canlı doğrula (varsayılan değiştir → etiket kalıyor mu).
4. **commission-rates nav + SSE yavaş-yükleme fix'leri** (`f1f2305b`, `779e4c7b`) — deploy sonrası 8085'te canlı doğrula.
5. **#13 reset-password zorunlu-değişim akışı** (GÜVENLİK) — `Views/Auth/ResetPassword.cshtml` stub, `POST /auth/reset-password` yok. #18 admin-reset done ama #13 onu uçtan uca bloke ediyor. Önerilen güvenli yol (SWE-Ahmet): geçici şifre re-entry + `NeedsTakeNewPassword` doğrula + yeni şifre set (knowledge-proof; token altyapısı gerekmez). `ecc:security-reviewer` ile gate et.

### P1 — Test harness (#14)
6. **Integration test izolasyonu** — Respawn "Users" contamination (`MigrationTests.SeedData_ShouldContainAdminUser` suite-içi fail, izole pass). + `DomainEventPipelineTests` jsonb `.Contains` (42883) gerçek bug. + Sales testleri `Payments: []` vs yeni `MakeSaleValidator` NotEmpty kuralı (SWE: testlere geçerli Payment ekle). QA Lead-3 (DefaultTenantContext) uygulanmıştı; kalanı bitir.

### P2 — S1/S2 in-code follow-up TODO'ları
7. CheckoutManager: configurable/öncelikli storefront deposu + çoklu-depo dağıtımı (şu an "yeterli stoğu olan ilk depo").
8. CheckoutManager FailPayment restore: depo-1 hardcoded → StockMovement referenceId'den gerçek depoyu çöz.
9. OfficeStockManager `IsInitialized` event-skip: background servis stok değiştirirse pazaryeri sync sessizce atlanır → doğru fix tenant context'i set etmek (mimari).
10. DB: CheckoutManager `BranchOfficeStocks` computed `CurrentStock` WHERE → row-lock; `(BranchOfficeId, ProductVariantId)` partial/covering index değerlendir (DB Master).

### P2 — Büyük backlog (PA önceliği; detay `docs/tasks/tasks.json`)
11. **Stage/Prod CI-CD deploy onarımı** (2 aydır kırık; `Host=postgres_db`, `STAGE_DB_CONNECTION` secret, `--network integration_app_default`). DevOps.
12. E1/E2/E5 pazaryeri içerik özelleştirme zinciri (E6 done; E1 açıklama transform, E2 kural profili, E5 çok-pazaryeri gönderim UI).
13. #1 `/sales` + #2 `/branch-offices/1` runtime hataları — fix'lendi mi doğrula (tzdata fix `/sales`'i çözmüş olabilir).
14. S5 fiyat-değişim event, D1 Trendyol yayından-kaldırma controller action, F2 N11/Pazarama kargo, WireMock T1 dev-default-açık.

### P2 — Gözlemlenebilirlik/depo (doğrula, açık olabilir)
15. dev→Loki OTLP endpoint: dev `OpenTelemetry__OtlpEndpoint` `http://192.168.1.78:4317` → `http://entegrasyon-tempo:4317` (`/opt/stacks/entegrasyon-dev/`) + redeploy.
16. MinIO `products-dev` bucket (app cred ile, root DEĞİL) — dev görsel deposu.

## Kırmızı çizgiler
main/prod'a push yok · prod DB'de destructive yok · migration silme yok · gerçek pazaryeri API'ye canlı yazma TL onayına tabi · secret commit/log'a yazılmaz · yerel makinede container yok.
