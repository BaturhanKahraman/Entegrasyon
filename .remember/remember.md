# Handoff

## State
I completed Faz 0 of the Blazor→MVC migration on branch `feature/mvc-migration` (from `develop`). New project `Application/Entegrasyon.MVC/` — 36 source files, full solution builds 0 errors. Infrastructure done: Program.cs (Cookie Auth, AppPermissions, Output Cache, Rate Limiting, Health Checks, OpenTelemetry, SignalR), TenantResolutionMiddleware, Filters (Tenant, AutoValidation), ExceptionHandlers, Tabler layout + sidebar + error pages, HTMX extensions + site.js, TempData/ViewData extensions, Tag Helpers. No commits yet.

## Next
1. **Faz 1: Auth + Dashboard** — Create `Features/Auth/AuthController.cs` (login/logout via `IAuthService`, claims matching `UserSession` format at Blazor line 76-86), `Features/Dashboard/DashboardController.cs` (single query + cached partials)
2. Commit Faz 0 work before starting Faz 1
3. Plan file: `/home/baturhan/.claude/plans/foamy-purring-lemur.md`, MVC skill: `~/.claude/skills/aspnet-mvc-htmx/`

## Context
- `InProcessNotificationDeliveryService` + `NotificationEventPublisher` are Blazor-specific (not in Business layer) — deferred to Faz 7 with TODO in Program.cs
- Project name is `Entegrasyon.MVC` (not Web) per user preference
- Tabler UI confirmed MIT/free — no paywall on dashboard components
- Port 5100 (Blazor stays on 5099), both run simultaneously
- User wants Blazor kept until ALL features migrated and verified
