---
title: "Instruction Files Index & Priority Matrix"
description: "Complete roadmap of all instruction files, their purpose, priority, and cross-references. Use this to navigate documentation."
vectorDbKeywords: "index, file-hierarchy, priority, cross-reference, documentation-map"
---

# INSTRUCTION FILES INDEX

**Purpose**: Single source of truth for instruction file organization and priority.
**Last Updated**: 2026-01-11
**Format**: Vector DB optimized for navigation

---

## QUICK PRIORITY MATRIX

| Priority | File | Purpose | Who Should Read | Read Time |
|----------|------|---------|-----------------|-----------|
| 🔴 **CRITICAL** | [CORE-ARCHITECTURE-RULES.md](./CORE-ARCHITECTURE-RULES.md) | Foundation rules: layering, entities, DI | **EVERYONE** | 20 min |
| 🔴 **CRITICAL** | [copilot-instructions.md](./copilot-instructions.md) | Repository specifics: structure, conventions | AI agents, new devs | 15 min |
| 🟠 **HIGH** | [blazor.instructions.md](./instructions/blazor.instructions.md) | Blazor patterns & component structure | Blazor developers | 10 min |
| 🟠 **HIGH** | [code-quality.instructions.md](./instructions/code-quality.instructions.md) | Code modularity & readability | All developers | 10 min |
| 🟠 **HIGH** | [csharp.instructions.md](./instructions/csharp.instructions.md) | C# conventions & best practices | C# developers | 10 min |
| 🟡 **MEDIUM** | [mud-blazor-changelog.instructions.md](./instructions/mud-blazor-changelog.instructions.md) | MudBlazor version-specific API | Blazor UI developers | 15 min |
| 🟡 **MEDIUM** | [performance-optimization.instructions.md](./instructions/performance-optimization.instructions.md) | Performance best practices (all stacks) | Performance-conscious devs | 30 min |

---

## DETAILED FILE DESCRIPTIONS

### 🔴 CRITICAL: CORE-ARCHITECTURE-RULES.md

**Location**: `.github/CORE-ARCHITECTURE-RULES.md`

**Purpose**: Authoritative foundation document. Source of truth for:
- Layering architecture (UI → Business → DataAccess → Entity)
- Entity model complete reference (all properties, relationships)
- DI patterns and service registration
- Marketplace integration workflows (Import vs Matching)
- Messaging strategies (Event Channels vs Wolverine)
- Naming conventions and file organization
- Database patterns (EF Core, Repository, UnitOfWork)

**When to Read**:
- ✅ First thing any AI agent should read before coding
- ✅ When uncertain about entity properties
- ✅ When implementing cross-layer features
- ✅ When integrating marketplaces

**Cross-References**:
- Detailed in section 3: Entity models (ProductVariant, Category, Customer, etc.)
- Detailed in section 4: Marketplace workflows (Trendyol import)
- Detailed in section 2: DI orchestration
- Summarized in: copilot-instructions.md (Architecture Overview)

**Vector Tags**: `layering`, `entity-model`, `dependency-injection`, `authoritative`

---

### 🔴 CRITICAL: copilot-instructions.md

**Location**: `.github/copilot-instructions.md`

**Purpose**: Repository-specific conventions and architecture narrative:
- Solution structure and project organization
- Entity model overview (links to CORE-ARCHITECTURE-RULES)
- Layering principles (with enforcement rules)
- Marketplace integration specifics (Trendyol, category import/matching)
- Development workflows (Docker, Podman, build/run commands)
- Testing approach (xUnit, Moq, FluentAssertions)
- Common issues and solutions
- Critical notes for AI agents

**When to Read**:
- ✅ New session with this repository
- ✅ When implementing repository-specific patterns
- ✅ When debugging environment-specific issues

**Cross-References**:
- Refers to: CORE-ARCHITECTURE-RULES.md for detailed rules
- Expanded by: blazor.instructions.md, csharp.instructions.md
- Implements: performance-optimization.instructions.md guidelines

**Special Notes**:
- Contains Trendyol marketplace-specific API details
- Documents deprecated projects (WinForm, some MessageQueue handlers)
- Has crucial "Architecture & Layering" section with AI agent guidance
- Includes instructions maintenance policy

**Vector Tags**: `repository-specific`, `architecture-narrative`, `marketplace-trendyol`, `conventions`

---

### 🟠 HIGH: blazor.instructions.md

**Location**: `.github/instructions/blazor.instructions.md`

**Purpose**: Blazor-specific patterns and component best practices:
- Component file separation (3-file pattern: .razor, .razor.cs, .razor.css)
- Lifecycle methods (OnInitializedAsync, OnParametersSetAsync, ShouldRender)
- Naming conventions for Blazor components
- Error handling (ErrorBoundary, validation)
- Caching strategies (IMemoryCache, Blazored.LocalStorage)
- State management (Cascading Parameters, EventCallbacks)
- API integration patterns
- Performance optimization (lazy loading, minimize re-renders)
- MudBlazor component usage

**When to Read**:
- ✅ Before creating/modifying Blazor components
- ✅ When implementing component communication
- ✅ When optimizing Blazor performance

**Cross-References**:
- Uses: mud-blazor-changelog.instructions.md (component API details)
- Implements: code-quality.instructions.md (component size limits)
- Depends on: CORE-ARCHITECTURE-RULES.md (DI, Entity models)

**Critical Rules**:
- Component file separation is MANDATORY for >30 lines
- Use EventCallbacks for loose coupling
- Async methods MUST use await
- No cross-layer references from Blazor

**Vector Tags**: `blazor-components`, `component-lifecycle`, `mudblazor`, `3-file-separation`

---

### 🟠 HIGH: csharp.instructions.md

**Location**: `.github/instructions/csharp.instructions.md`

**Purpose**: C# development standards and best practices:
- Language version constraints (.NET 8.0, C# 13)
- Naming conventions (PascalCase, camelCase, interface naming)
- Formatting rules (file-scoped namespaces, using directives)
- Nullable reference types (`string?`, `is null`)
- Data access patterns (EF Core, Repository, Migrations)
- Authentication & authorization (JWT, OAuth, Identity)
- Validation & error handling (FluentValidation, global exception handling)
- Logging & monitoring (Serilog, Application Insights)
- Testing practices (xUnit, Moq, no "Arrange/Act/Assert" comments)
- Performance optimization (caching, async/await)
- Deployment & CI/CD

**When to Read**:
- ✅ Before writing any C# code
- ✅ When creating new classes or methods
- ✅ When working with EF Core

**Cross-References**:
- Architecture specifics: CORE-ARCHITECTURE-RULES.md (layering, DI, entity models)
- Code quality: code-quality.instructions.md (method/class size limits)
- Performance: performance-optimization.instructions.md (async, caching)

**Critical Rules**:
- Layering enforcement: Presentation → Business → DataAccess → Entity
- Nullable reference types required
- No blocking I/O (async mandatory)
- DI pattern mandatory

**Vector Tags**: `csharp`, `net8.0`, `nullable-types`, `architecture-layering`

---

### 🟠 HIGH: code-quality.instructions.md

**Location**: `.github/instructions/code-quality.instructions.md`

**Purpose**: Code modularity, readability, and maintainability:
- Component usage & modularity (SRP, composition over inheritance)
- Performance & optimization (lazy loading, debouncing, memoization)
- Readability rules (method limits <20 lines, class limits <300 lines)
- SOLID principles & DRY
- Linting & formatting
- Folder structure & organization
- Feature-based organization

**When to Read**:
- ✅ Before large refactors
- ✅ When components/methods exceed size limits
- ✅ During code reviews

**Cross-References**:
- Extends: blazor.instructions.md (component modularity)
- Extends: csharp.instructions.md (method size limits)
- Implements: performance-optimization.instructions.md (code-level optimizations)

**Method Size Rule**: No method >20 lines (extract to sub-methods)

**Class Size Rule**: No class >300 lines (split into managers/services)

**Component Size Rule**: No component >200 lines (split into sub-components)

**Vector Tags**: `code-quality`, `modularity`, `size-limits`, `readability`

---

### 🟡 MEDIUM: mud-blazor-changelog.instructions.md

**Location**: `.github/instructions/mud-blazor-changelog.instructions.md`

**Purpose**: MudBlazor version-specific API documentation and migration guides:
- Current version details (v8.15.0 features)
- Breaking changes v7→v8 (MudPopoverProvider, async methods, parameter renames)
- Version-specific component behaviors (MudDataGrid, MudMenu, MudAutocomplete, MudDialog)
- Migration paths and upgrade patterns
- Deprecation warnings
- Code examples for each version

**When to Read**:
- ✅ Before using MudBlazor components
- ✅ When upgrading MudBlazor versions
- ✅ When component API seems wrong

**Project MudBlazor Version**: Check `.csproj` for `<PackageReference Include="MudBlazor" Version="..." />`

**Critical Changes v8**:
- MudPopoverProvider required in App.razor
- .NET 7 support dropped (minimum net8.0)
- Many methods now have `Async` suffix
- Parameter names changed (DisableElevation → DropShadow, etc.)

**Vector Tags**: `mudblazor`, `component-api`, `version-migration`, `breaking-changes`

---

### 🟡 MEDIUM: performance-optimization.instructions.md

**Location**: `.github/instructions/performance-optimization.instructions.md`

**Purpose**: Comprehensive performance best practices across all stacks:
- General principles (measure first, common case optimization, avoid premature optimization)
- Frontend performance (rendering, DOM, assets, network, JavaScript)
- Backend performance (algorithms, concurrency, caching, logging)
- Database performance (queries, schema, transactions, replication)
- Code review checklist
- Advanced topics (profiling, memory management, scalability)
- Mobile & cloud performance
- Practical examples (debouncing, SQL optimization, caching, lazy loading)

**When to Read**:
- ✅ For performance-critical features
- ✅ When profiling slow code
- ✅ During code reviews
- ✅ Before optimization attempts

**Key Principle**: "Measure First, Optimize Second"

**Frontend Checklist**:
- Minimize DOM manipulations
- Virtual DOM frameworks (React, Vue patterns) in Blazor context
- Image compression & modern formats
- Lazy loading
- HTTP/2, CDNs
- Event debouncing/throttling

**Backend Checklist**:
- Choose right data structures
- Async I/O (no blocking)
- Caching with proper invalidation
- Batch processing
- Connection pooling

**Database Checklist**:
- Indexes on frequently queried columns
- Avoid SELECT *
- Parameterized queries
- Avoid N+1 queries
- Query plan analysis

**Vector Tags**: `performance`, `optimization`, `profiling`, `best-practices`

---

## FILE DEPENDENCY GRAPH

```
CORE-ARCHITECTURE-RULES.md
    ↑ (source of truth)
    |
    ├─ copilot-instructions.md (references & summarizes)
    ├─ blazor.instructions.md (implements)
    ├─ csharp.instructions.md (implements)
    ├─ code-quality.instructions.md (builds on)
    ├─ mud-blazor-changelog.instructions.md (implements)
    └─ performance-optimization.instructions.md (complements)
```

**Read Order for New Session**:
1. **Start**: CORE-ARCHITECTURE-RULES.md (20 min)
2. **Then**: copilot-instructions.md (15 min)
3. **Then**: Appropriate specialized files based on task (10-30 min)

---

## CONFLICT RESOLUTION

**If instructions contradict each other**:

1. **Check priority**: CRITICAL > HIGH > MEDIUM
2. **Check scope**: Repository-specific > Language-specific > General
3. **Ask maintainer**: If still unclear, use this matrix to propose clarification

**Resolution Order**:
1. CORE-ARCHITECTURE-RULES.md (wins all)
2. copilot-instructions.md (repository specifics)
3. Specialized files (blazor, csharp, code-quality)
4. General guidance (performance, mud-blazor)

---

## VECTOR DATABASE TAGGING STRATEGY

Each section in each file has semantic tags at the end:

**Example Tag Set**: `layering`, `entity-model`, `dependency-injection`

**Tag Categories**:

- **Architecture**: `layering`, `architecture`, `dependency-injection`, `services`
- **Entity Models**: `entity-model`, `properties`, `relationships`, `composite-keys`, `owned-entities`
- **Patterns**: `repository-pattern`, `manager-pattern`, `component-pattern`, `event-pattern`
- **Marketplace**: `marketplace-integration`, `category-import`, `trendyol-api`
- **Messaging**: `messaging`, `event-channels`, `wolverine`, `rabbitmq`
- **Blazor**: `blazor-components`, `component-lifecycle`, `mudblazor`
- **Code Quality**: `code-quality`, `modularity`, `size-limits`, `readability`
- **Performance**: `performance`, `optimization`, `profiling`, `caching`

**Chunking Strategy for Vector DB**:
- Split files by section (use `##` as chunk boundary)
- Each chunk gets multiple tags
- Code examples separate chunks with parent section context
- "CRITICAL" sections get higher embedding weight

---

## USING THIS INDEX

### For AI Agents

1. **Start New Session**: Read this index + CORE-ARCHITECTURE-RULES.md
2. **Find Information**: Use tags to search vector DB
3. **Conflicted**: Use matrix above to resolve
4. **Uncertain**: Check CORE-ARCHITECTURE-RULES.md (it wins)

### For Developers

1. **New to Repository**: Read copilot-instructions.md (15 min)
2. **Detailed Questions**: Go to specific file from table above
3. **Code Review**: Check code-quality + performance instructions
4. **Blazor Work**: Read blazor + mud-blazor-changelog instructions

### For Maintainers

1. **Propose Change**: Use "Document Maintenance Policy" in CORE-ARCHITECTURE-RULES.md
2. **Update Files**: Keep order consistent (CORE first, then others)
3. **Version Changes**: Update mud-blazor-changelog.instructions.md immediately
4. **New Rules**: Add to CORE-ARCHITECTURE-RULES.md if foundational

---

## NEXT STEPS: VECTOR DATABASE INTEGRATION

### Recommended Tool
- **Local**: Weaviate (Docker) + Ollama (embeddings)
- **Cloud**: Pinecone + OpenAI embeddings

### Chunking Approach

```
For each instruction file:

1. Split by section (##)
2. Each chunk = one section
3. Metadata:
   - filename
   - section_number
   - priority (CRITICAL/HIGH/MEDIUM)
   - tags (semantic categories)
   - filepath
   - reading_time_minutes

4. Embed with:
   - Section title as context
   - Full section text
   - Code examples separately (if >100 chars)

5. Query examples:
   - "How do I create a new entity?"
   - "What's the difference between import and matching?"
   - "Entity model for Product"
   - "Component lifecycle Blazor"
   - "How do I add a service?"
```

### Vector DB Schema (Proposed)

```json
{
  "id": "core-arch-001",
  "filename": "CORE-ARCHITECTURE-RULES.md",
  "section": "Layering Architecture",
  "section_number": 1,
  "priority": "CRITICAL",
  "tags": ["layering", "architecture", "dependency-injection"],
  "content": "...",
  "code_examples": [...],
  "filepath": ".github/CORE-ARCHITECTURE-RULES.md",
  "reading_time_minutes": 20
}
```

---

**Last Modified**: 2026-01-11
**Format Version**: 1.0
**Status**: Active & Complete
