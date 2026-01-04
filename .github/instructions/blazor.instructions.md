---
description: 'Blazor component and application patterns'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css'
---

## Blazor Code Style and Structure

- Write idiomatic and efficient Blazor and C# code.
- Follow .NET and Blazor conventions.
- Use Razor Components appropriately for component-based UI development.
- Prefer inline functions for smaller components but separate complex logic into code-behind or service classes.
- Async/await should be used where applicable to ensure non-blocking UI operations.

## Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).

## Blazor and .NET Specific Guidelines

- Utilize Blazor's built-in features for component lifecycle (e.g., OnInitializedAsync, OnParametersSetAsync).
- Use data binding effectively with @bind.
- Leverage Dependency Injection for services in Blazor.
- Structure Blazor components and services following Separation of Concerns.
- Always use the latest version C#, currently C# 13 features like record types, pattern matching, and global usings.
 - Use a C# language version compatible with the project's TargetFramework (this repository targets `net8.0`). Before adopting newer C# language features, get approval from the project owner or maintainers.

## Error Handling and Validation

- Implement proper error handling for Blazor pages and API calls.
- Use logging for error tracking in the backend and consider capturing UI-level errors in Blazor with tools like ErrorBoundary.
- Implement validation using FluentValidation or DataAnnotations in forms.

## Blazor API and Performance Optimization

- Utilize Blazor server-side or WebAssembly optimally based on the project requirements.
- Use asynchronous methods (async/await) for API calls or UI actions that could block the main thread.
- Optimize Razor components by reducing unnecessary renders and using StateHasChanged() efficiently.
- Minimize the component render tree by avoiding re-renders unless necessary, using ShouldRender() where appropriate.
- Use EventCallbacks for handling user interactions efficiently, passing only minimal data when triggering events.

## Caching Strategies

- Implement in-memory caching for frequently used data, especially for Blazor Server apps. Use IMemoryCache for lightweight caching solutions.
- For Blazor WebAssembly, utilize localStorage or sessionStorage to cache application state between user sessions.
- Consider Distributed Cache strategies (like Redis or SQL Server Cache) for larger applications that need shared state across multiple users or clients.
- Cache API calls by storing responses to avoid redundant calls when data is unlikely to change, thus improving the user experience.

## State Management Libraries

- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing across components.
- Implement advanced state management solutions using libraries like Fluxor or BlazorState when the application grows in complexity.
- For client-side state persistence in Blazor WebAssembly, consider using Blazored.LocalStorage or Blazored.SessionStorage to maintain state between page reloads.
- For server-side Blazor, use Scoped Services and the StateContainer pattern to manage state within user sessions while minimizing re-renders.

## API Design and Integration

- Use HttpClient or other appropriate services to communicate with external APIs or your own backend.
- Implement error handling for API calls using try-catch and provide proper user feedback in the UI.

## Testing and Debugging in Visual Studio

- All unit testing and integration testing should be done in Visual Studio Enterprise.
 - Use a suitable test environment/IDE (Visual Studio, VS Code, Rider) and CI runners. Tests should run in CI via `dotnet test`.
 - Test Blazor components and services using xUnit, NUnit, or MSTest.
- Use Moq or NSubstitute for mocking dependencies during tests.
- Debug Blazor UI issues using browser developer tools and Visual Studio's debugging tools for backend and server-side issues.
- For performance profiling and optimization, rely on Visual Studio's diagnostics tools.

## Security and Authentication

- Implement Authentication and Authorization in the Blazor app where necessary using ASP.NET Identity or JWT tokens for API authentication.
- Use HTTPS for all web communication and ensure proper CORS policies are implemented.

## API Documentation and Swagger

- Use Swagger/OpenAPI for API documentation for your backend API services.
- Ensure XML documentation for models and API methods for enhancing Swagger documentation.

## Architecture & Layering (Blazor-specific)

- Follow the repository's layered architecture: `Entegrasyon.Blazor` is the presentation layer and must not reference `Entegrasyon.DataAccess` or other lower layers directly. Use `Entegrasyon.Business` (or `Shared` abstractions) to access data and business logic.
- Place UI-specific helpers and view models in the Blazor project; business rules and validations must live in the Business layer.
- When adding new services to be used by Blazor, register them via the DI registration project (see Dependency Resolver guidance). Keep Blazor registrations scoped to UI concerns.

## AI Model Guidance for Blazor

- AI agents must respect layering rules and avoid generating code that creates cross-layer references. If a generated change would violate layering, provide a refactor suggestion (e.g., create `IProductService` in `Entegrasyon.Business` and implement it in the Business project).
- If uncertain, AI should create a short design note and open an issue/PR for maintainers rather than making breaking structural changes.


## Repo-specific notes

- Follow repository-specific conventions and critical rules described in `copilot-instructions.md`. When repository guidance conflicts with generic instructions here, prefer the repository guidance and check with maintainers.

## Code Quality Enhancements (Added for Component Usage, Performance, and Readability)

- **Component Size Limits**: Keep components under 100 lines. Split larger ones into sub-components to maintain readability and modularity.
- **Avoid Long Methods**: No method in code-behind should exceed 20 lines. Extract logic into services or smaller methods.
- **Performance-First Rendering**: Use lazy loading for heavy components and memoization (e.g., via custom logic) to prevent unnecessary re-renders.
- **Readability Rules**: Prefer composition over complex inheritance. Add comments to performance-critical sections. Refactor if code isn't understandable quickly.
- See `code-quality.instructions.md` for detailed best practices on avoiding long/unreadable code.
