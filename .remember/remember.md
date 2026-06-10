# Handoff

## State
Dev ortamı 8085 TAM çalışır (login admin/123456789, WireMock, ürün-360). Bu oturumda kullanıcının 8085 testinden çıkan 21 geri bildirim (`docs/tasks/user-feedback-2026-06-10.md`) işlendi: 9 commit develop'a (Wave1 5 + Wave2 3 + güvenlik regresyon fix) — hepsi TDD + 8085'te canlı doğrulandı. Son develop tip ~`25c5e152`.
**Token modeli (KULLANICI KARARI):** kalıcı tmux takım YOK → her bağımsız iş için **efemeral alt-ajan** (`Agent` + `run_in_background` + `isolation:worktree`); brief'e ilk satır `git reset --hard gitea/develop` (bayat-base israfını keser). Küçük işi TL yapar.

## Next
1. Sıradaki blok kullanıcıya soruldu: Wave 2.5 (tıklanabilir tablo satırları) / Wave 3 (canlı kullanıcı takibi, Yardım formu backend→admin, dev observability) / #14 test triyajı.
2. #14 test triyajı net: `/discounts` NetworkIdle timeout, `MigrationTests` Respawn-isolation, ~45 Category integration, SellerManager slug.
3. #13 reset-password self-onboarding stub hâlâ açık (admin-reset eklendi).

## Context
- Ana çalışma ağacında ~25 commit'siz **Türkçe-karakter WIP** var (kullanıcının, kozmetik) → `VatRateManager`/`AuthService` agent değişiklikleriyle örtüşüyor; pull/rebase'i bloke ediyor. DOKUNMA; kullanıcı commit'lemeye hazır olunca reconcile. swe agent-def iyileştirmesi lokal commit `0efe234a` (push BEKLEMEDE, o reconcile'da).
- Gitea remote: `ssh://git@192.168.1.78:2222/baturhan/Entegrasyon.git`; develop push → `.gitea/workflows/deploy-dev.yml` (build→registry 5252→migrate→seed→deploy 8085). DB: `postgres_db`, IntegrationDb_Dev.
- 0ad50995 footgun tekrar ısırdı (no-tracking'te `Update` yoksa SaveChanges no-op) — login/seed/reorder hep buna düştü.
