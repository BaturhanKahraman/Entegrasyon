---
applyTo: '**/*.razor, **/*.razor.cs, **/*.cs'
description: 'Code quality best practices for maintainable, performant, and readable code in Blazor and C# projects. Focuses on avoiding long/unreadable classes, functions, and components.'
---

# Code Quality Best Practices

## Introduction

This guide complements existing instructions (Blazor, C#, Performance) by emphasizing component usage, performance, and readability. The goal is to prevent long, unreadable classes, functions, or components. All code should be modular, efficient, and easy to understand.

## Component Usage and Modularity (Blazor-Focused)

- **Single Responsibility Principle**: Each component should have one responsibility. Limit components to 50-100 lines; split larger ones into sub-components.
- **Small Components**: Avoid complex, monolithic components. Use MudBlazor components (e.g., MudDataGrid, MudForm) to reduce boilerplate.
- **Composition Over Inheritance**: Build larger structures by composing components, not inheriting. Use EventChannels for loose coupling.
- **Rule**: If a component exceeds 200 lines, refactor into smaller parts with @inject for dependencies.

## Performance and Optimization

- **Lazy Loading and Code Splitting**: Lazy-load large components to improve initial load times and avoid bloated code blocks.
- **Memoization and Debouncing**: Use memoization for expensive computations; debounce/throttle event handlers (e.g., search inputs).
- **Minimize DOM Manipulation**: Batch updates and use virtual scrolling. Leverage MudDataGrid pagination for long lists.
- **Memory Leak Prevention**: Clean up event listeners in components; use dispose patterns in long-lived services.
- **Rule**: Use async/await in performance-critical code, but avoid callback hell. Test with Lighthouse or Chrome DevTools.

## Readability and Code Quality (C# and General)

- **Function/Method Limits**: No method should exceed 20 lines. Break long logic into named sub-methods (Extract Method refactoring).
- **Class Size**: Classes should not exceed 300 lines. Split large classes into managers/services (e.g., divide ProductManager into validation and display managers).
- **Naming and Commenting**: Use PascalCase; avoid overly long names. Add comments to performance-critical code (e.g., "// Optimized for N+1 avoidance").
- **SOLID and DRY**: Use Dependency Injection for loose coupling. Move repeated code to utility classes.
- **Linting and Formatting**: Enforce with Roslyn analyzers or StyleCop. Make long chains (e.g., LINQ) readable.
- **Rule**: In code reviews, if code isn't understandable in 5 minutes, refactor. Replace long switch/if blocks with strategy patterns.

## Integration and Testing

- **Unit Testing**: Write tests for small components easily. Include performance benchmarks.
- **CI/CD**: Use tools like SonarQube to reject overly complex code.
- **Rule**: For new features, test components and ensure performance budgets aren't exceeded.

## Folder Structure and Organization

- **Feature-Based Folders**: Organize code by features (e.g., Products/, Orders/). Place related components, services, and models together.
- **Separation of Concerns**: Keep Views, ViewModels, Services, and Utilities in separate subfolders. Avoid mixing UI logic with business logic.
- **Naming Conventions**: Use PascalCase for folders and files. Prefix with feature name (e.g., ProductList.razor, ProductService.cs).
- **Layered Structure**: Follow repository conventions: Blazor (UI) → Business → DataAccess → Entity. No cross-layer references.
- **Rule**: If a folder grows too large (>10 files), split into subfeatures. Document folder purposes in README files.

## References
- Extend from blazor.instructions.md, csharp.instructions.md, and performance-optimization.instructions.md.
- Use VS Code "Code Metrics" extension to monitor class/method sizes.

---

<!-- End of Code Quality Instructions -->
