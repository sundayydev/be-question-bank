# Clean Architecture & CQRS Refactoring Design: BeQuestionBank

**Date**: 2026-11-20  
**Status**: Ready for Approval  
**Target Solution**: `BeQuestionBank.sln` (.NET 10.0, C# 14, PostgreSQL, Blazor WASM Frontend)

---

## 1. Intent and the Why

The current backend repository bundles business logic, data access (EF Core DbContext, Repositories), database migrations, and external service integrations together into a single project: `BEQuestionBank.Core`. This violates the Separation of Concerns, causes tight coupling between persistence mechanisms and business rules, and makes adding new features or writing isolated unit tests cumbersome ("khó code", khó bảo trì).

The goal of this refactoring is to transition `BeQuestionBank` into a strict, enterprise-grade **Clean Architecture** with **CQRS (MediatR)** and **FluentValidation**, inspired by the reference architecture document while preserving 100% of the existing business logic, database schema, and frontend (`FEQuestionBank.Client`) contract compatibility.

---

## 2. Goals, Non-Goals, and Anti-Goals

### Goals
- **4-Tier Clean Architecture**: Establish clear project separation:
  - `BeQuestionBank.Domain`: Pure business domain (Entities, Enums, Domain Exceptions, Value Objects).
  - `BeQuestionBank.Application`: CQRS Commands/Queries, MediatR Handlers, FluentValidation, Pipeline Behaviors, Interfaces.
  - `BeQuestionBank.Infrastructure`: Data access (EF Core `AppDbContext`, Repositories, Migrations), Redis, File I/O, OpenXML import, and Word/PDF export engines.
  - `BeQuestionBank.API` (WebApi): Minimal thin Controllers, Middlewares, Dependency Injection, Swagger & Serilog.
  - `BeQuestionBank.Shared`: Shared DTOs, Enums, ApiResponse, PagedResult (used by API & Blazor).
- **Full CQRS Transition via MediatR**:
  - Convert Controller operations into discrete MediatR Commands and Queries.
  - Implement pipeline behaviors for automatic request validation (`ValidationBehavior`) and structured logging (`LoggingBehavior`).
  - Specialized low-level engines (Word/ZIP OpenXML parsing, complex matrix extraction algorithms, Word/PDF document rendering, EZP AES encryption) are maintained in `Infrastructure` behind clear interfaces (`IImportService`, `IExportService`, `IEncryptionService`) and consumed cleanly by MediatR Handlers.
- **Strict Dependency Inversion**:
  - `Domain` depends on nothing external (pure .NET standard / C#).
  - `Application` depends only on `Domain` and `Shared` contracts.
  - `Infrastructure` implements abstractions defined in `Application` and `Domain`.
  - `API` acts as an entry point orchestrator dispatching HTTP requests to MediatR.
- **Zero Business Logic Regressions**: All existing functionality (exam generation, word/pdf export, question bank management, user auth, matrix extraction) continues functioning without breaking.
- **Frontend Compatibility**: `FEQuestionBank.Client` continues building and communicating with API without any contract or DTO breakage.

### Non-Goals
- Adding complex external infrastructure distributed services mentioned in the sample document that are not currently in the project (e.g. OpenIddict server migration, Quartz.NET persistent scheduler, MassTransit distributed service bus) unless explicitly requested later.
- Rewriting the Blazor frontend client (`FEQuestionBank.Client`).
- Altering existing PostgreSQL database tables or running breaking schema migrations.

### Anti-Goals
- "Big Bang" breaking refactoring that leaves the solution in an uncompilable state.
- Leaking EF Core dependencies, DbContext, or SQL logic into the `Domain` or `Application` layers.
- Breaking existing API routes or HTTP response structures (`ApiResponse<T>`).

---

## 3. Constraints

1. **Framework & Runtime**: .NET 10.0, C# 14, PostgreSQL with EF Core / Npgsql.
2. **Existing Specialized Libraries**: Syncfusion DocIO (licensed), QuestPDF Community, Aspose.Words, DocX, CSharpMath.SkiaSharp, StackExchange.Redis, BCrypt.Net-Next.
3. **Database Compatibility**: Must retain current PostgreSQL schema and existing migration history (`AppDbContextModelSnapshot`).
4. **Shared Project Contract**: `BeQuestionBank.Shared` is referenced by `FEQuestionBank.Client` (Blazor WebAssembly). DTO names, namespaces, and properties must remain compatible to prevent breaking the frontend build.
5. **Target Framework Upgrade**: All projects in the solution (`Domain`, `Application`, `Infrastructure`, `API`, `Shared`, `FEQuestionBank.Client`, `Test`) will target `net10.0` with `LangVersion` 14.0.

---

## 4. Architectural Blueprint & Project Structure

### 4.1. Solution Structure Overview

```
be-question-bank/
├── BeQuestionBank.Domain/              # Core business entities, enums, domain exceptions, base models
├── BeQuestionBank.Application/         # CQRS Commands/Queries, Handlers, FluentValidation, Interfaces
├── BeQuestionBank.Infrastructure/      # EF Core DbContext, Repositories, Migrations, Redis, File/Export/Import engines
├── BeQuestionBank.API/                 # Controllers, Middlewares, Dependency Injection, Swagger/Serilog
├── BeQuestionBank.Shared/              # Shared DTOs, Enums, ApiResponse, PagedResult (used by API & Blazor)
├── BeQuestionBank.Test/                # Unit & Integration Tests
└── FEQuestionBank.Client/              # Blazor WebAssembly Frontend
```

### 4.2. Dependency Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      BeQuestionBank.API                     │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
               ▼                               ▼
┌──────────────────────────────┐ ┌─────────────────────────────┐
│   BeQuestionBank.Application │ │ BeQuestionBank.Infrastructure │
└──────────────┬───────────────┘ └─────────────┬───────────────┘
               │                               │
               ▼                               │
┌──────────────────────────────┐               │
│     BeQuestionBank.Domain    │◄──────────────┘
└──────────────▲───────────────┘
               │
┌──────────────┴───────────────┐
│     BeQuestionBank.Shared    │ (Contracts & DTOs shared with Blazor)
└──────────────────────────────┘
```

---

## 5. Architectural Decisions (Builder Calls)

The following architectural calls were established to resolve review findings:

1. **CQRS & Service Architecture (User Approved)**:
   - All Controller actions will dispatch MediatR `IRequest<ApiResponse<T>>` Commands and Queries.
   - Core CRUD and domain workflows (Auth, Khoa, MonHoc, Phan, NguoiDung, CauHoi, DeThi, YeuCauRutTrich) are refactored into dedicated MediatR Handlers with FluentValidation validators.
   - Heavy technical processors (`ImportService` with 2,200 lines of OpenXML parsing, `DeThiExportService`, `DeThiTuLuanExportService`, `EzpEncryptionService`) will reside in `Infrastructure` implementing clean abstractions (`IImportService`, `IExportService`, `IEncryptionService`) in `Application`. Handlers orchestrate these services cleanly without code bloat.

2. **Data Access & Repositories**:
   - Repositories (`GenericRepository`, `CauHoiRepository`, `DeThiRepository`, etc.) remain in `Infrastructure/Persistence`.
   - Repository interfaces (`ICauHoiRepository`, `IDeThiRepository`, etc.) reside in `Application/Common/Interfaces` (or `Domain`). Leaked EF Core types (such as `IDbContextTransaction`) are removed from interfaces and replaced by a pure transaction abstraction (`IUnitOfWork`).
   - MediatR handlers utilize repositories and `IApplicationDbContext` according to use-case requirements.

3. **Domain Enums & Shared Contract**:
   - Domain enums (`EnumCLO`, `EnumRole`, `FileType`) reside in `Domain/Enums` (pure C#).
   - `BeQuestionBank.Shared` references `BeQuestionBank.Domain` (pure .NET Standard/Core assembly with zero external dependencies).
   - `FEQuestionBank.Client` continues referencing `BeQuestionBank.Shared` with zero build or namespace breakage.

4. **Document & Media Processing (`ImportService`)**:
   - `ImportService` and OpenXML dependencies reside in `Infrastructure`.
   - `Application` defines `IImportService` operating with `Stream` or DTO parameters.
   - MediatR commands receive `Stream` / filenames, keeping `Application` completely decoupled from ASP.NET Core HTTP (`IFormFile`) and OpenXML SDKs.

---

## 6. Layer-by-Layer Detailed Layout

### Layer 1: `BeQuestionBank.Domain`
- **Entities**: `CauHoi`, `CauTraLoi`, `ChiTietDeThi`, `DeThi`, `File`, `Khoa`, `MonHoc`, `NguoiDung`, `Phan`, `YeuCauRutTrich`.
- **Common**: `ModelBase` (Auditable entity base: `Id`, `CreatedDate`, `UpdatedDate`, `IsDeleted`).
- **Enums**: `EnumRole`, `EnumCLO`, `FileType`.
- **Exceptions**: `DomainException`, `NotFoundException`, `ValidationException`.
- **Dependencies**: Pure .NET standard/Core C# (no EF Core or Web dependencies).

### Layer 2: `BeQuestionBank.Application` (New Project)
- **Common**:
  - `Behaviors`: `ValidationBehavior<TRequest, TResponse>`, `LoggingBehavior<TRequest, TResponse>`.
  - `Interfaces`: `IApplicationDbContext`, `IUnitOfWork`, `ICauHoiRepository`, `IDeThiRepository`, `IKhoaRepository`, `IMonHocRepository`, `INguoiDungRepository`, `IPhanRepository`, `IYeuCauRutTrichRepository`, `IFileRepository`, `IJwtService`, `IRedisCacheService`, `IImportService`, `IExportService`, `IEncryptionService`.
- **Features (CQRS Modules)**:
  - `Auth`: Commands (`LoginCommand`, `RegisterCommand`, `RefreshTokenCommand`), Handlers, Validators.
  - `CauHoi`: Commands (`CreateCauHoiCommand`, `UpdateCauHoiCommand`, `DeleteCauHoiCommand`), Queries (`GetCauHoiByIdQuery`, `GetCauHoiPagedQuery`), Handlers, Validators.
  - `DeThi`: Commands (`CreateDeThiCommand`, `UpdateDeThiCommand`, `DeleteDeThiCommand`, `ExportDeThiCommand`), Queries (`GetDeThiByIdQuery`, `GetDeThiPagedQuery`), Handlers.
  - `Khoa`: Commands & Queries (`GetKhoaPagedQuery`, `CreateKhoaCommand`, `UpdateKhoaCommand`, `DeleteKhoaCommand`).
  - `MonHoc`: Commands & Queries (`GetMonHocPagedQuery`, `CreateMonHocCommand`, etc.).
  - `Phan`: Commands & Queries (`GetPhanPagedQuery`, `CreatePhanCommand`, etc.).
  - `NguoiDung`: Commands & Queries (`GetNguoiDungPagedQuery`, `UpdateNguoiDungCommand`, etc.).
  - `YeuCauRutTrich`: Commands & Queries (`CreateYeuCauRutTrichCommand`, `CheckYeuCauQuery`, etc.).
  - `Tool`: Commands (`ConvertLatexCommand`, `UploadImageCommand`).
  - `File`: Commands (`UploadFileCommand`, `DeleteFileCommand`), Queries (`GetFileQuery`).
- **Dependency Injection**: `DependencyInjection.AddApplicationServices()`.
- **Dependencies**: `BeQuestionBank.Domain`, `BeQuestionBank.Shared`, `MediatR`, `FluentValidation`, `Mapster`.

### Layer 3: `BeQuestionBank.Infrastructure` (Transformed from `BEQuestionBank.Core`)
- **Persistence**:
  - `AppDbContext`: Implements `IApplicationDbContext` and `IUnitOfWork`.
  - `Configurations`: EntityTypeConfiguration mappings for PostgreSQL.
  - `Repositories`: EF Core Repository implementations.
  - `Migrations`: PostgreSQL EF Core migrations preserved from current solution.
- **Services (Engines & External Integrations)**:
  - `RedisCacheService`: Redis caching implementation.
  - `JwtService`: JWT creation and validation.
  - `FileStorageService`: Disk attachment handling.
  - `EzpEncryptionService`: Exam encryption/decryption utilities.
  - `ImportService`: OpenXML Word/ZIP processing engine implementing `IImportService`.
  - `Export`: `DeThiExportService`, `DeThiExportForStudentService`, `DeThiTuLuanExportService`, `RutTrichTuLuanService` implementing `IExportService`.
- **Dependency Injection**: `DependencyInjection.AddInfrastructureServices(IConfiguration configuration)`.
- **Dependencies**: `BeQuestionBank.Application`, `BeQuestionBank.Domain`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `StackExchange.Redis`, `Syncfusion`, `QuestPDF`, `Aspose.Words`, `DocX`, `CSharpMath.SkiaSharp`, `BCrypt.Net-Next`.

### Layer 4: `BeQuestionBank.API`
- **Controllers**: Thin controllers that inject `ISender` and dispatch requests via MediatR.
- **Middlewares**: `GlobalExceptionMiddleware` mapping exceptions to standard `ApiResponse<T>`.
- **Program.cs**: Wires up `AddApplicationServices()`, `AddInfrastructureServices()`, Serilog logging, Swagger, CORS, and Syncfusion/QuestPDF licensing.
- **Dependencies**: `BeQuestionBank.Application`, `BeQuestionBank.Infrastructure`, `BeQuestionBank.Shared`.

---

## 7. Step-by-Step Implementation Strategy

1. **Phase 1: Project Scaffolding & Setup**
   - Create `BeQuestionBank.Application` (.NET 10.0 Class Library, C# 14) and add to `BeQuestionBank.sln`.
   - Upgrade existing projects to `net10.0` and `<LangVersion>14.0</LangVersion>`.
   - Rename `BEQuestionBank.Core` to `BeQuestionBank.Infrastructure` (and update references).
   - Configure project references according to the dependency rules.
2. **Phase 2: Domain Cleanup**
   - Move Enums to `Domain/Enums`.
   - Remove EF Core direct package references from `Domain`.
   - Verify `BeQuestionBank.Shared` and `FEQuestionBank.Client` compile cleanly.
3. **Phase 3: Application Layer Implementation**
   - Install `MediatR` and `FluentValidation` in `Application`.
   - Add Behaviors (`ValidationBehavior`, `LoggingBehavior`).
   - Define interfaces for repositories and infrastructure services.
   - Implement CQRS Commands, Queries, Handlers, and Validators for each feature slice.
4. **Phase 4: Infrastructure Layer Refactoring**
   - Move DbContext, Configurations, Migrations, and Repositories to `Infrastructure/Persistence`.
   - Refactor Repositories to implement interfaces from `Application`.
   - Wire up `IImportService`, `IExportService`, and external services in `Infrastructure/Services`.
   - Register all services in `AddInfrastructureServices`.
5. **Phase 5: API Layer Refactoring**
   - Refactor Controllers to inject `ISender` and route requests through MediatR.
   - Update `Program.cs` to register Application and Infrastructure services.
   - Ensure global exception handling returns `ApiResponse`.
6. **Phase 6: Verification & Validation**
   - Run `dotnet build BeQuestionBank.sln` to guarantee 0 errors across all projects.
   - Verify `BeQuestionBank.Test` passes.
   - Verify `FEQuestionBank.Client` compiles without warnings or errors.
