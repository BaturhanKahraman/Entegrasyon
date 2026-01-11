# Entegrasyon E-commerce Platform - Developer & AI Instructions

> **⚠️ CRITICAL SOURCE OF TRUTH**: This file contains the definitive rules for the project.
> **MAINTENANCE**: Developers and AI Agents MUST keep this file updated. If a new pattern is adopted or an architectural change is made, update this file immediately.
> **AI AGENTS**: Read this file at the start of every session.

## 1. Project Identity
- **Name**: Entegrasyon
- **Architecture**: Pure Blazor Server (.NET 8)
- **Status**: Modernized (Old MVC/API projects are Deprecated/Deleted)

## 2. Solution Structure & Naming
The solution follows a strict Layered Architecture.

```
Application/
├── Entegrasyon.Blazor          # UI Layer (Presentation / StartUp)
├── Entegrasyon.ApplicationBootstrap # DI Registration (Formerly DependencyResolver)
├── Entegrasyon.Business        # Business Logic
├── Entegrasyon.DataAccess      # Data Access
├── Entegrasyon.Entity          # Domain Models
```

### 2.1. Naming & Folder Rules
- **Dependency Injection**: Use `Entegrasyon.ApplicationBootstrap` project for all DI registrations. (`DependencyResolver` is deprecated/renamed).
- **Business Layer (Strict)**:
    - **Interfaces**: MUST be in `Application/Entegrasyon.Business/Abstract/`.
        - Naming: `I{Entity}Service.cs` (e.g., `IProductService.cs`).
    - **Implementations**: MUST be in `Application/Entegrasyon.Business/Concrete/`.
        - Naming: `{Entity}Manager.cs` (e.g., `ProductManager.cs`).
        - Implementations must assume `Manager` suffix.
- **UI Components (Blazor)**:
    - **Code-Behind**: Any component with logic (>30 LOC) MUST have a `.razor.cs` file.
    - **Scoped CSS**: Any custom style MUST be in `.razor.css`.
    - **Injection**: Inject Interfaces (`IProductService`), NOT Concrete classes (`ProductManager`).

## 3. Best Practices (Mandatory)

### 3.1. General C# & .NET
- **Async/Await**: All I/O operations (DB, File, Network) MUST be `async`. Never use `.Result` or `.Wait()`.
- **Result Pattern**: Business methods must return a wrapper generic (e.g., `IDataResult<T>` or `Result<T>`). **Never return raw entities directly** from business logic if validation/status is needed.
- **Validation**: Use **FluentValidation** within the Business layer. Fail fast.
- **DTOs**: Use Data Transfer Objects (records preferred) for complex UI data to decouple from Database Entities.
- **Records**: Prefer `public record` for DTOs and immutable data structures.
- **Null Safety**: Enable nullable reference types where possible (`<Nullable>enable</Nullable>`).

### 3.2. Blazor Specific
- **MudBlazor Only**: Do not introduce Bootstrap or raw CSS grid unless absolutely necessary. Use `MudGrid`, `MudItem`, `MudStack`.
- **State Management**: Use **State Container Pattern** (Scoped Services with C# Events/Actions). Do NOT use Channels for UI state.
    - *Recommendation*: Create a Scoped Service (e.g., `CartStateContainer`) that holds the state and exposes an `OnChange` event. Components subscribe to this event to trigger `StateHasChanged()`.
- **Event Handling**: Use `System.Threading.Channels` **ONLY** for backend/background event processing workflows (e.g., triggering a background job after `ProductAddedEvent`).
- **Parent-Child**: Use `EventCallback` for simple component communication.

## 4. Domain Workflows (Do Not Confuse)
1.  **Category Import** (Sync from Marketplace):
    - Creates **NEW** categories from a source (e.g., Trendyol).
    - Sets `IsImported = true`.
2.  **Marketplace Matching** (Manual Mapping):
    - Links **EXISTING** manual categories to a marketplace via `CategoryMarketplace` table.

## 5. Deployment & Environment
- **Operating System**: Fedora Linux.
    - **Package Management**: Use `dnf` or `flatpak` for system dependencies.
- **Infra Containerization**: The project uses **Podman** instead of Docker for managing core services (PostgreSQL, Redis).
- **Run Workflow**:
  1. Start core services: `podman-compose up -d`
  2. Run the Blazor application: `cd Application/Entegrasyon.Blazor && dotnet run`
- **Config**: `docker-compose.yml` (used by Podman) environment variables invoke the correct connection strings.

## 6. How to Read This File
- **AI Agents**: You have permission to read this file at any time. It is located at the root (`/INSTRUCTIONS.md`). If you are unsure about a rule, check here first.

## 7. References
- **MudBlazor API**: See `.agent/rules/MUDBLAZOR_GUIDE.md` for v7+ best practices (including Dialogs, DataGrids, and Forms).
