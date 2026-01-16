---
description: 'Guidelines for building C# applications'
applyTo: '**/*.cs'
---

# C# DEVELOPMENT GUIDELINES

**📚 RELATED**: See [CORE-ARCHITECTURE-RULES.md](../CORE-ARCHITECTURE-RULES.md) for architecture, layering, and entity models.

## C# Instructions
- Use the C# language version compatible with the project's TargetFramework (this repository targets `net8.0`). Before adopting newer C# language features, get approval from the project owner or maintainers.
- Write clear and concise comments for each function.

## General Instructions
- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.

## Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).

## Formatting

- Apply code-formatting style defined in `.editorconfig`.
- Prefer file-scoped namespace declarations and single-line using directives.
- Follow the project's `.editorconfig` for brace and formatting rules; do not hard-enforce an unrelated brace placement style in these instructions.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

## Project Setup and Structure

- Guide users through creating a new .NET project with the appropriate templates.
- Explain the purpose of each generated file and folder to build understanding of the project structure.
- Demonstrate how to organize code using feature folders or domain-driven design principles.
- Show proper separation of concerns with models, services, and data access layers.
 - Explain the Program.cs and configuration system in ASP.NET Core (align examples with the repository's target framework, e.g., .NET 8) including environment-specific settings.

## Nullable Reference Types

- Declare variables non-nullable, and check for `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

## Data Access Patterns

- Guide the implementation of a data access layer using Entity Framework Core.
- Explain different options (SQL Server, SQLite, In-Memory) for development and production.
- Demonstrate repository pattern implementation and when it's beneficial.
- Show how to implement database migrations and data seeding.
- Explain efficient query patterns to avoid common performance issues.

## Authentication and Authorization

- Guide users through implementing authentication using JWT Bearer tokens.
- Explain OAuth 2.0 and OpenID Connect concepts as they relate to ASP.NET Core.
- Show how to implement role-based and policy-based authorization.
- Demonstrate integration with Microsoft Entra ID (formerly Azure AD).
- Explain how to secure both controller-based and Minimal APIs consistently.

## Validation and Error Handling

- Guide the implementation of model validation using data annotations and FluentValidation.
- Explain the validation pipeline and how to customize validation responses.
- Demonstrate a global exception handling strategy using middleware.
- Show how to create consistent error responses across the API.
- Explain problem details (RFC 7807) implementation for standardized error responses.

## API Versioning and Documentation

- Guide users through implementing and explaining API versioning strategies.
- Demonstrate Swagger/OpenAPI implementation with proper documentation.
- Show how to document endpoints, parameters, responses, and authentication.
- Explain versioning in both controller-based and Minimal APIs.
- Guide users on creating meaningful API documentation that helps consumers.

## Logging and Monitoring

- Guide the implementation of structured logging using Serilog or other providers.
- Explain the logging levels and when to use each.
- Demonstrate integration with Application Insights for telemetry collection.
- Show how to implement custom telemetry and correlation IDs for request tracking.
- Explain how to monitor API performance, errors, and usage patterns.

## Testing

- Always include test cases for critical paths of the application.
- Guide users through creating unit tests.
- Do not emit "Act", "Arrange" or "Assert" comments.
- Copy existing style in nearby files for test method names and capitalization.
- Explain integration testing approaches for API endpoints.
- Demonstrate how to mock dependencies for effective testing.
- Show how to test authentication and authorization logic.
- Explain test-driven development principles as applied to API development.

## Performance Optimization

- Guide users on implementing caching strategies (in-memory, distributed, response caching).
- Explain asynchronous programming patterns and why they matter for API performance.
- Demonstrate pagination, filtering, and sorting for large data sets.
- Show how to implement compression and other performance optimizations.
- Explain how to measure and benchmark API performance.

## Deployment and DevOps

- Guide users through containerizing their API using .NET's built-in container support (`dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`).
- Explain the differences between manual Dockerfile creation and .NET's container publishing features.
- Explain CI/CD pipelines for NET applications.
- Demonstrate deployment to Azure App Service, Azure Container Apps, or other hosting options.
- Show how to implement health checks and readiness probes.
- Explain environment-specific configurations for different deployment stages.

## Architecture & Layering (Repository Conventions)

- Enforce a layered architecture: Presentation (Blazor) → Business → DataAccess → Entity/Shared. Dependencies must only point "down" the stack. For example, `Entegrasyon.Blazor` can depend on `Entegrasyon.Business`, `Entegrasyon.DependencyResolver`, and `Shared`, but it must never reference types from `Entegrasyon.DataAccess` or `Entegrasyon.Entity` directly.
- Communication between layers must use interfaces and service abstractions defined in the appropriate layer (e.g., business-facing interfaces in `Entegrasyon.Business` or `Shared`). If a different layering is required, create a design proposal and notify maintainers.
- If code needs to call into another layer not allowed by the rules, add a clear justification comment and open an issue/PR describing the rationale.

## AI Model Guidance for Repo Rules

- These repository rules are authoritative for any AI assistant or automated tool operating on this codebase. When generating code or suggestions, ensure the output adheres to the layering rules above.
- Actions AI should take when encountering a layering violation:
	- Halt code generation that would introduce a cross-layer reference.
	- Suggest adding a service/interface in the appropriate layer instead, and provide a small template.
	- If the user insists that cross-layer access is required, create a PR with an explicit design note and label it for architecture review.

## Dependency Resolver Naming Guidance

- The current `Entegrasyon.DependencyResolver` was intended to register services and orchestrate DI for the application. If the project's `DependencyResolver` contains code beyond simple DI registration (e.g., configuration, environment wiring, bootstrap logic), rename it to better reflect its role. Suggested names by scope:
	- DI-only registrars: `Application.DependencyRegistration` or `ServiceRegistration`
	- Bootstrap + config: `Application.Bootstrap` or `Application.StartupHelpers`
	- If it is an IoC composition root with multiple responsibilities: `Application.CompositionRoot`
- Document the chosen name and update all project references and README notes.

## Examples

- Run EF Core migrations (from the project that contains the DbContext):

```bash
dotnet ef migrations add AddMyEntity -p ../Entegrasyon.DataAccess/ -s ../Application/Entegrasyon.Blazor/
dotnet ef database update -p ../Entegrasyon.DataAccess/ -s ../Application/Entegrasyon.Blazor/
```

- Build and publish a Linux container image (example):

```bash
dotnet publish -c Release -o out
docker build -t entegrasyonblazor:local -f Dockerfile .
```

- Run the application in watch mode:

```bash
dotnet watch run --project Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj
```

## Code Quality Enhancements (Added for Readability and Maintainability)

- **Class and Method Size Limits**: Classes should not exceed 300 lines; methods should not exceed 20 lines. Use Extract Method refactoring for long logic.
- **SOLID Principles**: Ensure single responsibility; use Dependency Injection for loose coupling. Avoid deep inheritance hierarchies.
- **Readability Rules**: Add comments to complex logic; use meaningful names. Refactor switch/if blocks into strategies if they grow long.
- **Performance with Clarity**: Optimize queries and caching, but keep code readable. Avoid premature optimization that complicates maintenance.
- See `code-quality.instructions.md` for detailed best practices on avoiding long/unreadable code.

