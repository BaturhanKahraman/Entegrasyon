# Deprecation Notice: Entegrasyon.MessageQueue

This project (`Entegrasyon.MessageQueue` and `Entegrasyon.MessageQueue.Commands`) is deprecated and scheduled for removal.

## Migration Plan

1. **Identify Dependencies**: Review all handlers and consumers in `Entegrasyon.MessageQueue` and `Entegrasyon.MessageQueue.Commands`.
2. **Replace with Alternatives**: 
   - For distributed messaging: Use Wolverine directly in the appropriate layer (e.g., Business or ApplicationBootstrap).
   - For in-process events: Continue using .NET Channels via `EventChannel<T>` in `Entegrasyon.Blazor.Services.Channels`.
3. **Update Tests**: Ensure all tests pass after migration.
4. **Remove Projects**: Delete the MessageQueue projects only after all dependencies are migrated.

## Current Usage
- Wolverine message handlers for distributed scenarios.
- Command/event messages for marketplace sync.

## Contact
If you need to reintroduce messaging, create an issue with the use case.