# Handoff

## State
Blazor→MVC migration COMPLETE on branch `feature/mvc-migration` (9 commits from `develop`). Project `Application/Entegrasyon.MVC/` — 166 source files, 33 controllers, all 65 Blazor routes have MVC equivalents. 10 integration tests pass. Dockerfile + docker-compose.stage.yml ready. CLAUDE.md updated. Runtime tested: health ✓, login ✓, auth redirect ✓, static assets ✓. Not pushed to remote.

## Next
1. **Browser test** — `cd Application/Entegrasyon.MVC && dotnet run`, login admin/123456789, navigate pages
2. **Push** — `git push -u origin feature/mvc-migration` when ready
3. **Polish placeholders** — Product wizard, POS terminal, Chat SignalR JS client

## Context
- Pre-existing unit test failure: `TrendyolProductSendPage` missing permission mapping (unrelated)
- `IRoleService` lives in `Entegrasyon.Business.Concrete.Auth` namespace (not Abstract)
- `InProcessNotificationDeliveryService` Blazor-specific — TODO in Program.cs
- MVC port: 5100 (dev), 8082 (stage). Blazor: 5099/8081
