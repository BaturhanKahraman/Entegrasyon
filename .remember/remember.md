# Handoff

## State
MVC migration COMPLETE on `feature/mvc-migration` (15 commits). All placeholder features replaced with functional implementations. 190+ source files. POS terminal, Chat/SignalR, Notification Bell, Product CRUD wizard, Category wizard, Image upload — all implemented. Not pushed to remote yet.

## Next
1. **Browser test** — test all new features: POS flow, Product create wizard, Chat, Edit save
2. **Push** — `git push -u origin feature/mvc-migration`
3. **Desktop App Settings** — last remaining placeholder (low priority, desktop-app specific)

## Context
- POS cart uses HTTP Session (AddSession in Program.cs)
- SignalR: signalr.min.js global in layout, NotificationHub + ChatHub connected
- Business layer: 2 minor .Include() changes (CategoryManager, CategoryAttributeManager), 1 search fix (ToLower in ApplyGlobalSearch)
- `IRoleService` in `Entegrasyon.Business.Concrete.Auth` namespace
