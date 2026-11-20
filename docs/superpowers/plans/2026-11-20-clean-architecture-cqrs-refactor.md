# Clean Architecture & CQRS Refactoring (.NET 10 / C# 14) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `BeQuestionBank` into a modular 4-tier Clean Architecture (`API`, `Application`, `Infrastructure`, `Domain`) with MediatR CQRS and FluentValidation, upgraded to .NET 10.0 and C# 14, while preserving full database and Blazor frontend compatibility.

**Architecture:** Inward dependency flow where `Domain` is pure C#, `Application` encapsulates business use-cases as MediatR Commands/Queries with pipeline validation, `Infrastructure` handles PostgreSQL EF Core persistence and specialized file/export engines, and `API` dispatches HTTP requests through MediatR. `BeQuestionBank.Shared` is maintained to share contracts and DTOs with `FEQuestionBank.Client` (Blazor WASM).

**Tech Stack:** .NET 10.0, C# 14, ASP.NET Core, EF Core 10 / Npgsql PostgreSQL, MediatR 12.x+, FluentValidation, Mapster, Serilog, StackExchange.Redis, Syncfusion DocIO, QuestPDF, Aspose.Words, Blazor WebAssembly.

**Spec:** [2026-11-20-clean-architecture-refactor-design.md](file:///c:/Users/DELL/Documents/00-Project/be-question-bank/docs/superpowers/specs/2026-11-20-clean-architecture-refactor-design.md)

## Global Constraints

- **Platform & Language**: All projects target `net10.0` with `<LangVersion>14.0</LangVersion>`.
- **Dependency Inversion**: `Domain` depends on nothing external. `Application` depends only on `Domain` and `Shared`. `Infrastructure` depends on `Application` and `Domain`. `API` depends on `Application`, `Infrastructure`, and `Shared`.
- **Frontend Compatibility**: `FEQuestionBank.Client` must build without warnings or breaking contract/DTO changes.
- **Database Schema**: Preserve existing PostgreSQL schema and migration history (`AppDbContextModelSnapshot`).
- **Response Format**: Preserve standard API response contract (`ApiResponse<T>` and `PagedResult<T>`).

## Review Focus

1. **Enum Contract Breakage**: `FEQuestionBank.Client` references `BeQuestionBank.Shared.Enums`. Moving enums to `Domain` must not break Blazor code importing `BeQuestionBank.Shared.Enums`.
2. **OpenXML Leaks into Application**: `Application` must not reference `DocumentFormat.OpenXml` or `Microsoft.AspNetCore.Http.IFormFile`. File streams and byte arrays must be used across the boundary.
3. **Transaction Abstraction**: EF Core's `IDbContextTransaction` must not leak into `Application` interfaces; use `IUnitOfWork` or `IApplicationDbContext`.
4. **Validation Pipeline Bypass**: All MediatR commands must pass through `ValidationBehavior` to throw `ValidationException` on invalid inputs before hitting handlers.
5. **PostgreSQL Migration Assembly Configuration**: `options.UseNpgsql(..., x => x.MigrationsAssembly("BeQuestionBank.Infrastructure"))` must match the new infrastructure assembly name.

---

### Task 1: Solution & Projects Scaffolding for .NET 10 & C# 14

**Files:**
- Create: `BeQuestionBank.Application/BeQuestionBank.Application.csproj`
- Modify: `BeQuestionBank.sln`
- Modify: `BeQuestionBank.Domain/BeQuestionBank.Domain.csproj`
- Modify: `BeQuestionBank.Shared/BeQuestionBank.Shared.csproj`
- Modify: `BeQuestionBank.API/BeQuestionBank.API.csproj`
- Modify: `BeQuestionBank.Test/BeQuestionBank.Test.csproj`
- Modify: `FEQuestionBank.Client/FEQuestionBank.Client.csproj`
- Rename/Modify: `BEQuestionBank.Core/BEQuestionBank.Core.csproj` -> `BeQuestionBank.Infrastructure/BeQuestionBank.Infrastructure.csproj`

**Interfaces:**
- Consumes: Existing project files
- Produces: `BeQuestionBank.Application.dll`, `BeQuestionBank.Infrastructure.dll` targeting `net10.0` and C# 14

- [ ] **Step 1: Write verification test for framework and reference consistency**

Add a unit test in `BeQuestionBank.Test/Architecture/ArchitectureBootstrapTests.cs`:
```csharp
[Fact]
public void Solution_Assemblies_ShouldTargetNet10()
{
    var domainAssembly = typeof(BeQuestionBank.Domain.Common.ModelBase).Assembly;
    var targetFramework = domainAssembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
                                        .OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
                                        .FirstOrDefault()?.FrameworkName;
    Assert.Contains(".NETCoreApp,Version=v10.0", targetFramework);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BeQuestionBank.Test --filter Solution_Assemblies_ShouldTargetNet10`
Expected: FAIL (projects currently target net9.0)

- [ ] **Step 3: Create `BeQuestionBank.Application` and update all csproj to net10.0**

1. Create `BeQuestionBank.Application/BeQuestionBank.Application.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\BeQuestionBank.Domain\BeQuestionBank.Domain.csproj" />
    <ProjectReference Include="..\BeQuestionBank.Shared\BeQuestionBank.Shared.csproj" />
  </ItemGroup>
</Project>
```
2. Rename folder `BEQuestionBank.Core` to `BeQuestionBank.Infrastructure` and rename `BEQuestionBank.Core.csproj` to `BeQuestionBank.Infrastructure.csproj`.
3. Update `TargetFramework` to `net10.0` and `LangVersion` to `14.0` in:
   - `BeQuestionBank.Domain/BeQuestionBank.Domain.csproj`
   - `BeQuestionBank.Shared/BeQuestionBank.Shared.csproj`
   - `BeQuestionBank.Infrastructure/BeQuestionBank.Infrastructure.csproj` (add reference to `BeQuestionBank.Application.csproj`)
   - `BeQuestionBank.API/BeQuestionBank.API.csproj` (reference `BeQuestionBank.Application.csproj` and `BeQuestionBank.Infrastructure.csproj`)
   - `BeQuestionBank.Test/BeQuestionBank.Test.csproj` (reference `BeQuestionBank.Application.csproj`, `BeQuestionBank.Infrastructure.csproj`)
   - `FEQuestionBank.Client/FEQuestionBank.Client.csproj`
4. Update `BeQuestionBank.sln` to include `BeQuestionBank.Application` and point to `BeQuestionBank.Infrastructure`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build BeQuestionBank.sln`
Run: `dotnet test BeQuestionBank.Test --filter Solution_Assemblies_ShouldTargetNet10`
Expected: PASS with 0 build errors.

- [ ] **Step 5: Commit scaffolding changes**

```bash
git add .
git commit -m "chore: upgrade solution to .NET 10 / C# 14 and scaffold Clean Architecture projects"
```

---

### Task 2: Domain Layer Purity & Exceptions Hierarchy

**Files:**
- Create: `BeQuestionBank.Domain/Exceptions/DomainException.cs`
- Create: `BeQuestionBank.Domain/Exceptions/NotFoundException.cs`
- Create: `BeQuestionBank.Domain/Exceptions/ValidationException.cs`
- Create: `BeQuestionBank.Domain/Exceptions/ConflictException.cs`
- Move: `BeQuestionBank.Shared/Enums/EnumCLO.cs` -> `BeQuestionBank.Domain/Enums/EnumCLO.cs`
- Move: `BeQuestionBank.Shared/Enums/EnumRole.cs` -> `BeQuestionBank.Domain/Enums/EnumRole.cs`
- Move: `BeQuestionBank.Shared/Enums/FileType.cs` -> `BeQuestionBank.Domain/Enums/FileType.cs`
- Modify: `BeQuestionBank.Shared/Enums/EnumsForwarding.cs` (type forward / namespace forwarding)
- Modify: `BeQuestionBank.Domain/BeQuestionBank.Domain.csproj` (remove direct EF Core dependencies)

**Interfaces:**
- Consumes: None (pure Domain)
- Produces: `BeQuestionBank.Domain.Exceptions.*`, `BeQuestionBank.Domain.Enums.*`

- [ ] **Step 1: Write unit tests for Domain Exceptions & Enums**

Create `BeQuestionBank.Test/Domain/DomainExceptionTests.cs`:
```csharp
namespace BeQuestionBank.Test.Domain;

using BeQuestionBank.Domain.Exceptions;
using Xunit;

public class DomainExceptionTests
{
    [Fact]
    public void NotFoundException_ShouldSetCorrectMessage()
    {
        var ex = new NotFoundException("CauHoi", 123);
        Assert.Equal("Entity \"CauHoi\" (123) was not found.", ex.Message);
    }

    [Fact]
    public void ValidationException_ShouldStoreErrors()
    {
        var errors = new Dictionary<string, string[]> { { "TenKhoa", new[] { "Required" } } };
        var ex = new ValidationException(errors);
        Assert.True(ex.Errors.ContainsKey("TenKhoa"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BeQuestionBank.Test --filter DomainExceptionTests`
Expected: FAIL (Exceptions do not exist yet)

- [ ] **Step 3: Implement Domain Exceptions, move Enums and maintain backward compatibility**

1. Create `BeQuestionBank.Domain/Exceptions/DomainException.cs`, `NotFoundException.cs`, `ValidationException.cs`, `ConflictException.cs`.
2. Move Enums into `BeQuestionBank.Domain/Enums/` with namespace `BeQuestionBank.Domain.Enums`.
3. In `BeQuestionBank.Shared/BeQuestionBank.Shared.csproj`, reference `BeQuestionBank.Domain.csproj`.
4. Create `BeQuestionBank.Shared/Enums/EnumsForwarding.cs` to alias types:
```csharp
namespace BeQuestionBank.Shared.Enums;
global using EnumRole = BeQuestionBank.Domain.Enums.EnumRole;
global using EnumCLO = BeQuestionBank.Domain.Enums.EnumCLO;
global using FileType = BeQuestionBank.Domain.Enums.FileType;
```
5. Remove `Microsoft.EntityFrameworkCore` package references from `BeQuestionBank.Domain.csproj`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BeQuestionBank.Test --filter DomainExceptionTests`
Run: `dotnet build FEQuestionBank.Client/FEQuestionBank.Client.csproj`
Expected: PASS (Domain tests pass and Blazor WebAssembly compiles without enum errors).

- [ ] **Step 5: Commit Domain changes**

```bash
git add BeQuestionBank.Domain BeQuestionBank.Shared BeQuestionBank.Test
git commit -m "feat(domain): add domain exception hierarchy, pure enums and maintain shared contracts"
```

---

### Task 3: Application Core (MediatR, FluentValidation, Pipeline Behaviors & Interfaces)

**Files:**
- Modify: `BeQuestionBank.Application/BeQuestionBank.Application.csproj`
- Create: `BeQuestionBank.Application/Common/Behaviors/ValidationBehavior.cs`
- Create: `BeQuestionBank.Application/Common/Behaviors/LoggingBehavior.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IApplicationDbContext.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IUnitOfWork.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/ICauHoiRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/IDeThiRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/IKhoaRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/IMonHocRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/INguoiDungRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/IPhanRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/IYeuCauRutTrichRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/IRepositories/IFileRepository.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/Services/IJwtService.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/Services/IRedisCacheService.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/Services/IImportService.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/Services/IExportService.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/Services/IEncryptionService.cs`
- Create: `BeQuestionBank.Application/Common/Interfaces/Services/IFileStorageService.cs`
- Create: `BeQuestionBank.Application/DependencyInjection.cs`

**Interfaces:**
- Consumes: `BeQuestionBank.Domain.*`, `BeQuestionBank.Shared.*`
- Produces: `IApplicationDbContext`, `IUnitOfWork`, `ValidationBehavior`, `DependencyInjection.AddApplicationServices(IServiceCollection)`

- [ ] **Step 1: Write test for ValidationBehavior**

Create `BeQuestionBank.Test/Application/ValidationBehaviorTests.cs`:
```csharp
namespace BeQuestionBank.Test.Application;

using BeQuestionBank.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Xunit;

public class ValidationBehaviorTests
{
    public record TestCommand(string Name) : IRequest<string>;
    public class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task ValidationBehavior_ShouldThrow_WhenValidationFails()
    {
        var validators = new List<IValidator<TestCommand>> { new TestValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("");

        await Assert.ThrowsAsync<BeQuestionBank.Domain.Exceptions.ValidationException>(() =>
            behavior.Handle(command, () => Task.FromResult("ok"), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BeQuestionBank.Test --filter ValidationBehaviorTests`
Expected: FAIL

- [ ] **Step 3: Implement Application packages, behaviors, interfaces, and DI**

1. In `BeQuestionBank.Application.csproj`, add package references:
   - `MediatR` (12.4.1+)
   - `FluentValidation` (11.11.0+)
   - `FluentValidation.DependencyInjectionExtensions` (11.11.0+)
   - `Microsoft.EntityFrameworkCore` (10.0.0-rc+ or 9.0.8 compatibility)
   - `Mapster` (7.4.0)
2. Implement `ValidationBehavior<TRequest, TResponse>` executing all registered validators and throwing `BeQuestionBank.Domain.Exceptions.ValidationException` on failure.
3. Implement `LoggingBehavior<TRequest, TResponse>`.
4. Define `IApplicationDbContext`:
```csharp
namespace BeQuestionBank.Application.Common.Interfaces;
public interface IApplicationDbContext
{
    DbSet<CauHoi> CauHois { get; }
    DbSet<CauTraLoi> CauTraLois { get; }
    DbSet<ChiTietDeThi> ChiTietDeThis { get; }
    DbSet<DeThi> DeThis { get; }
    DbSet<File> Files { get; }
    DbSet<Khoa> Khoas { get; }
    DbSet<MonHoc> MonHocs { get; }
    DbSet<NguoiDung> NguoiDungs { get; }
    DbSet<Phan> Phans { get; }
    DbSet<YeuCauRutTrich> YeuCauRutTriches { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```
5. Define `IUnitOfWork`, clean Repository interfaces, and Service interfaces (`IImportService`, `IExportService`, `IEncryptionService`, `IRedisCacheService`, `IJwtService`, `IFileStorageService`).
6. Implement `DependencyInjection.AddApplicationServices(this IServiceCollection services)`:
   - `services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()); cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); cfg.AddOpenBehavior(typeof(LoggingBehavior<,>)); });`
   - `services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());`

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BeQuestionBank.Test --filter ValidationBehaviorTests`
Expected: PASS

- [ ] **Step 5: Commit Application Core**

```bash
git add BeQuestionBank.Application BeQuestionBank.Test
git commit -m "feat(application): add MediatR pipeline behaviors, core abstractions, and DI registration"
```

---

### Task 4: Infrastructure Persistence & Services Restructuring

**Files:**
- Modify: `BeQuestionBank.Infrastructure/Configurations/AppDbContext.cs`
- Modify: `BeQuestionBank.Infrastructure/Repositories/*.cs`
- Modify: `BeQuestionBank.Infrastructure/Services/RedisService.cs` -> `RedisCacheService.cs`
- Modify: `BeQuestionBank.Infrastructure/Services/EzpEncryptionService.cs`
- Modify: `BeQuestionBank.Infrastructure/Services/ImportService.cs`
- Modify: `BeQuestionBank.Infrastructure/Services/FileService.cs`
- Modify: `BeQuestionBank.Infrastructure/Helpers/JwtHelper.cs`
- Modify: `BeQuestionBank.Infrastructure/Services/DeThiExport*.cs`
- Create: `BeQuestionBank.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `BeQuestionBank.Application.Common.Interfaces.*`
- Produces: `DependencyInjection.AddInfrastructureServices(IServiceCollection, IConfiguration)`

- [ ] **Step 1: Write test for Infrastructure DI registration**

Create `BeQuestionBank.Test/Infrastructure/InfrastructureDITests.cs`:
```csharp
namespace BeQuestionBank.Test.Infrastructure;

using BeQuestionBank.Application.Common.Interfaces;
using BeQuestionBank.Application.Common.Interfaces.IRepositories;
using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class InfrastructureDITests
{
    [Fact]
    public void AddInfrastructureServices_ShouldRegisterExpectedServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            {"ConnectionStrings:PostgresConnection", "Host=localhost;Database=dummy"},
            {"ConnectionStrings:Redis", "localhost:6379,abortConnect=false"}
        }).Build();

        services.AddLogging();
        services.AddInfrastructureServices(config);

        Assert.Contains(services, s => s.ServiceType == typeof(IKhoaRepository));
        Assert.Contains(services, s => s.ServiceType == typeof(IImportService));
        Assert.Contains(services, s => s.ServiceType == typeof(IEncryptionService));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BeQuestionBank.Test --filter InfrastructureDITests`
Expected: FAIL (`AddInfrastructureServices` not defined yet)

- [ ] **Step 3: Implement Infrastructure implementations & DI**

1. Make `AppDbContext` implement `IApplicationDbContext` and `IUnitOfWork`.
2. Update Repositories (`KhoaRepository`, `MonHocRepository`, `PhanRepository`, `DeThiRepository`, `CauHoiRepository`, `NguoiDungRepository`, `FileRepository`, `YeuCauRutTrichRepository`) to implement Application repository interfaces. Remove any direct dependency on `Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction` in public signatures (encapsulated in `IUnitOfWork`).
3. Make `ImportService` implement `IImportService`, `EzpEncryptionService` implement `IEncryptionService`, `RedisService` implement `IRedisCacheService`, `JwtHelper` implement `IJwtService`, `FileService` implement `IFileStorageService`, and export services implement `IExportService`.
4. Create `BeQuestionBank.Infrastructure/DependencyInjection.cs`:
```csharp
namespace BeQuestionBank.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgresConnection"),
                x => x.MigrationsAssembly("BeQuestionBank.Infrastructure")
            ));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Repositories
        services.AddScoped<IKhoaRepository, KhoaRepository>();
        services.AddScoped<IMonHocRepository, MonHocRepository>();
        services.AddScoped<IPhanRepository, PhanRepository>();
        services.AddScoped<IDeThiRepository, DeThiRepository>();
        services.AddScoped<ICauHoiRepository, CauHoiRepository>();
        services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IYeuCauRutTrichRepository, YeuCauRutTrichRepository>();

        // Services
        services.AddScoped<IJwtService, JwtHelper>();
        services.AddScoped<IRedisCacheService, RedisService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IEncryptionService, EzpEncryptionService>();
        services.AddScoped<IFileStorageService, FileService>();
        services.AddScoped<IExportService, DeThiExportService>();

        return services;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BeQuestionBank.Test --filter InfrastructureDITests`
Expected: PASS

- [ ] **Step 5: Commit Infrastructure changes**

```bash
git add BeQuestionBank.Infrastructure BeQuestionBank.Test
git commit -m "feat(infrastructure): implement application abstractions, repositories, and DI registration"
```

---

### Task 5: Application Feature Slices - Auth, Khoa, MonHoc, Phan, NguoiDung

**Files:**
- Create: `BeQuestionBank.Application/Features/Auth/Commands/Login/LoginCommand.cs`
- Create: `BeQuestionBank.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- Create: `BeQuestionBank.Application/Features/Auth/Commands/Login/LoginCommandValidator.cs`
- Create: `BeQuestionBank.Application/Features/Auth/Commands/Register/RegisterCommand.cs`
- Create: `BeQuestionBank.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs`
- Create: `BeQuestionBank.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs`
- Create: `BeQuestionBank.Application/Features/Khoa/Queries/GetKhoaPagedQuery.cs`
- Create: `BeQuestionBank.Application/Features/Khoa/Commands/CreateKhoaCommand.cs`
- Create: `BeQuestionBank.Application/Features/Khoa/Commands/UpdateKhoaCommand.cs`
- Create: `BeQuestionBank.Application/Features/Khoa/Commands/DeleteKhoaCommand.cs`
- Create: `BeQuestionBank.Application/Features/MonHoc/Queries/GetMonHocPagedQuery.cs`
- Create: `BeQuestionBank.Application/Features/MonHoc/Commands/CreateMonHocCommand.cs`
- Create: `BeQuestionBank.Application/Features/Phan/Queries/GetPhanPagedQuery.cs`
- Create: `BeQuestionBank.Application/Features/Phan/Commands/CreatePhanCommand.cs`
- Create: `BeQuestionBank.Application/Features/NguoiDung/Queries/GetNguoiDungPagedQuery.cs`
- Create: `BeQuestionBank.Application/Features/NguoiDung/Commands/UpdateNguoiDungCommand.cs`

**Interfaces:**
- Consumes: `IKhoaRepository`, `IMonHocRepository`, `IPhanRepository`, `INguoiDungRepository`, `IJwtService`
- Produces: MediatR Commands/Queries returning `ApiResponse<T>` or `PagedResult<T>`

- [ ] **Step 1: Write unit tests for Auth & Khoa CQRS Handlers**

Create `BeQuestionBank.Test/Features/AuthAndKhoaHandlerTests.cs`:
```csharp
namespace BeQuestionBank.Test.Features;

using BeQuestionBank.Application.Features.Khoa.Commands;
using BeQuestionBank.Application.Common.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Moq;
using Xunit;

public class AuthAndKhoaHandlerTests
{
    [Fact]
    public async Task CreateKhoaCommandHandler_ShouldCallRepositoryAdd()
    {
        var repoMock = new Mock<IKhoaRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Khoa>())).ReturnsAsync((Khoa k) => k);

        var handler = new CreateKhoaCommandHandler(repoMock.Object);
        var command = new CreateKhoaCommand { TenKhoa = "Cong Nghe Thong Tin", MaKhoa = "CNTT" };

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Khoa>()), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BeQuestionBank.Test --filter AuthAndKhoaHandlerTests`
Expected: FAIL

- [ ] **Step 3: Implement CQRS Handlers and Validators for Auth, Khoa, MonHoc, Phan, NguoiDung**

1. `Features/Auth`:
   - `LoginCommand`, `LoginCommandHandler`, `LoginCommandValidator` (validates password, checks user, generates tokens via `IJwtService`).
   - `RegisterCommand`, `RegisterCommandHandler`, `RegisterCommandValidator`.
   - `RefreshTokenCommand`, `RefreshTokenCommandHandler`.
2. `Features/Khoa`:
   - `GetKhoaPagedQuery` & Handler (returns `PagedResult<KhoaDto>`).
   - `CreateKhoaCommand`, `UpdateKhoaCommand`, `DeleteKhoaCommand` & Handlers + FluentValidators.
3. `Features/MonHoc`:
   - `GetMonHocPagedQuery`, `GetMonHocByIdQuery`, `CreateMonHocCommand`, `UpdateMonHocCommand`, `DeleteMonHocCommand`.
4. `Features/Phan`:
   - `GetPhanPagedQuery`, `CreatePhanCommand`, `UpdatePhanCommand`, `DeletePhanCommand`.
5. `Features/NguoiDung`:
   - `GetNguoiDungPagedQuery`, `UpdateNguoiDungCommand`, `ChangePasswordCommand`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BeQuestionBank.Test --filter AuthAndKhoaHandlerTests`
Expected: PASS

- [ ] **Step 5: Commit Feature Slices**

```bash
git add BeQuestionBank.Application BeQuestionBank.Test
git commit -m "feat(application): implement CQRS handlers and validators for Auth, Khoa, MonHoc, Phan, NguoiDung"
```

---

### Task 6: Application Feature Slices - CauHoi, DeThi, YeuCauRutTrich, File, Tool

**Files:**
- Create: `BeQuestionBank.Application/Features/CauHoi/Commands/CreateCauHoi/CreateCauHoiCommand.cs`
- Create: `BeQuestionBank.Application/Features/CauHoi/Commands/CreateCauHoi/CreateCauHoiCommandHandler.cs`
- Create: `BeQuestionBank.Application/Features/CauHoi/Commands/UpdateCauHoi/UpdateCauHoiCommand.cs`
- Create: `BeQuestionBank.Application/Features/CauHoi/Commands/DeleteCauHoi/DeleteCauHoiCommand.cs`
- Create: `BeQuestionBank.Application/Features/CauHoi/Queries/GetCauHoiPagedQuery.cs`
- Create: `BeQuestionBank.Application/Features/CauHoi/Queries/GetCauHoiByIdQuery.cs`
- Create: `BeQuestionBank.Application/Features/DeThi/Commands/CreateDeThiCommand.cs`
- Create: `BeQuestionBank.Application/Features/DeThi/Commands/ExportDeThiCommand.cs`
- Create: `BeQuestionBank.Application/Features/DeThi/Queries/GetDeThiPagedQuery.cs`
- Create: `BeQuestionBank.Application/Features/YeuCauRutTrich/Commands/CreateYeuCauRutTrichCommand.cs`
- Create: `BeQuestionBank.Application/Features/YeuCauRutTrich/Queries/CheckYeuCauRutTrichQuery.cs`
- Create: `BeQuestionBank.Application/Features/Import/Commands/ImportWordCommand.cs`
- Create: `BeQuestionBank.Application/Features/File/Commands/UploadFileCommand.cs`
- Create: `BeQuestionBank.Application/Features/Tool/Commands/ConvertLatexCommand.cs`

**Interfaces:**
- Consumes: `ICauHoiRepository`, `IDeThiRepository`, `IYeuCauRutTrichRepository`, `IImportService`, `IExportService`, `IEncryptionService`
- Produces: CQRS Handlers for core exam & question bank operations

- [ ] **Step 1: Write test for CauHoi and DeThi Handlers**

Create `BeQuestionBank.Test/Features/CauHoiAndDeThiHandlerTests.cs`:
```csharp
namespace BeQuestionBank.Test.Features;

using BeQuestionBank.Application.Features.CauHoi.Commands.CreateCauHoi;
using BeQuestionBank.Application.Common.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Moq;
using Xunit;

public class CauHoiAndDeThiHandlerTests
{
    [Fact]
    public async Task CreateCauHoiCommandHandler_ShouldPersistQuestion()
    {
        var repoMock = new Mock<ICauHoiRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<CauHoi>())).ReturnsAsync((CauHoi q) => q);

        var handler = new CreateCauHoiCommandHandler(repoMock.Object);
        var command = new CreateCauHoiCommand { NoiDung = "Test question?", MaMonHoc = 1 };

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
        repoMock.Verify(r => r.AddAsync(It.IsAny<CauHoi>()), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BeQuestionBank.Test --filter CauHoiAndDeThiHandlerTests`
Expected: FAIL

- [ ] **Step 3: Implement CauHoi, DeThi, YeuCauRutTrich, Import, File, and Tool Handlers**

1. Implement `Features/CauHoi`: `CreateCauHoiCommand`, `UpdateCauHoiCommand`, `DeleteCauHoiCommand`, `GetCauHoiPagedQuery`, `GetCauHoiByIdQuery`.
2. Implement `Features/DeThi`: `CreateDeThiCommand`, `UpdateDeThiCommand`, `DeleteDeThiCommand`, `ExportDeThiCommand` (calls `IExportService` / `IEncryptionService`), `GetDeThiPagedQuery`, `GetDeThiByIdQuery`.
3. Implement `Features/YeuCauRutTrich`: `CreateYeuCauRutTrichCommand`, `CheckYeuCauRutTrichQuery`, `GetYeuCauRutTrichPagedQuery`.
4. Implement `Features/Import`: `ImportWordCommand`, `ImportZipCommand` (calls `IImportService` passing `Stream` and filename).
5. Implement `Features/File`: `UploadFileCommand` (calls `IFileStorageService`), `GetFileQuery`.
6. Implement `Features/Tool`: `ConvertLatexCommand`, `UploadImageCommand`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BeQuestionBank.Test --filter CauHoiAndDeThiHandlerTests`
Expected: PASS

- [ ] **Step 5: Commit CauHoi and DeThi Feature Slices**

```bash
git add BeQuestionBank.Application BeQuestionBank.Test
git commit -m "feat(application): implement CQRS handlers for CauHoi, DeThi, YeuCauRutTrich, Import, File and Tool"
```

---

### Task 7: API Layer Modernization & Controller Thinning

**Files:**
- Modify: `BeQuestionBank.API/Controllers/AuthController.cs`
- Modify: `BeQuestionBank.API/Controllers/KhoaController.cs`
- Modify: `BeQuestionBank.API/Controllers/MonHocController.cs`
- Modify: `BeQuestionBank.API/Controllers/PhanController.cs`
- Modify: `BeQuestionBank.API/Controllers/NguoiDungController.cs`
- Modify: `BeQuestionBank.API/Controllers/CauHoiController.cs`
- Modify: `BeQuestionBank.API/Controllers/DeThiController.cs`
- Modify: `BeQuestionBank.API/Controllers/YeuCauRutTrichController.cs`
- Modify: `BeQuestionBank.API/Controllers/FileController.cs`
- Modify: `BeQuestionBank.API/Controllers/ImportController.cs`
- Modify: `BeQuestionBank.API/Controllers/ToolController.cs`
- Modify: `BeQuestionBank.API/Middlewares/GlobalExceptionMiddleware.cs`
- Modify: `BeQuestionBank.API/Program.cs`
- Remove: `BeQuestionBank.API/Extensions/ServiceCollectionExtensions.cs` (superseded by Application/Infrastructure DI)

**Interfaces:**
- Consumes: MediatR `ISender`, `AddApplicationServices`, `AddInfrastructureServices`
- Produces: Slim HTTP Controllers mapping endpoints to MediatR Requests

- [ ] **Step 1: Write integration test for Controller dispatch via MediatR**

Create `BeQuestionBank.Test/API/KhoaControllerTests.cs`:
```csharp
namespace BeQuestionBank.Test.API;

using BeQuestionBank.API.Controllers;
using BeQuestionBank.Application.Features.Khoa.Commands;
using BeQuestionBank.Shared.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class KhoaControllerTests
{
    [Fact]
    public async Task Create_ShouldSendCreateKhoaCommand()
    {
        var senderMock = new Mock<ISender>();
        senderMock.Setup(s => s.Send(It.IsAny<CreateKhoaCommand>(), default))
                  .ReturnsAsync(ApiResponseFactory.Success("Created"));

        var controller = new KhoaController(senderMock.Object);
        var command = new CreateKhoaCommand { TenKhoa = "CNTT", MaKhoa = "CNTT" };

        var actionResult = await controller.Create(command);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        senderMock.Verify(s => s.Send(command, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test BeQuestionBank.Test --filter KhoaControllerTests`
Expected: FAIL (Controller currently injects KhoaService, not ISender)

- [ ] **Step 3: Refactor Controllers, Middlewares, and Program.cs**

1. Update `KhoaController`, `MonHocController`, `PhanController`, `AuthController`, `NguoiDungController`, `CauHoiController`, `DeThiController`, `YeuCauRutTrichController`, `FileController`, `ImportController`, `ToolController` to inject `ISender` and dispatch MediatR Commands/Queries.
2. In `ImportController`, read file upload as stream (`file.OpenReadStream()`) and dispatch to `ImportWordCommand` / `ImportZipCommand`.
3. Update `GlobalExceptionMiddleware` to intercept:
   - `ValidationException` (from Domain/FluentValidation) -> HTTP 400 with validation errors dictionary.
   - `NotFoundException` -> HTTP 404 with error message.
   - `DomainException` / `ConflictException` -> HTTP 409 / 400.
   - Unhandled exceptions -> HTTP 500 formatted as `ApiResponseFactory.ServerError()`.
4. In `Program.cs`:
   - Replace old service registration with:
     ```csharp
     builder.Services.AddApplicationServices();
     builder.Services.AddInfrastructureServices(builder.Configuration);
     ```
   - Retain Serilog, CORS, JWT authentication, Swagger, and QuestPDF/Syncfusion license setup.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test BeQuestionBank.Test --filter KhoaControllerTests`
Expected: PASS

- [ ] **Step 5: Commit API modernization**

```bash
git add BeQuestionBank.API BeQuestionBank.Test
git commit -m "feat(api): modernize controllers to dispatch MediatR requests and update global exception middleware"
```

---

### Task 8: End-to-End Build, Frontend & Test Verification

**Files:**
- Modify: `BeQuestionBank.Test/*.cs` (update test references to new namespaces)
- Verify: All solution files

**Interfaces:**
- Consumes: Complete refactored solution
- Produces: Zero compile errors, passing test suites across all projects

- [ ] **Step 1: Run comprehensive solution build**

Run: `dotnet build BeQuestionBank.sln -c Debug`
Expected: `0 Error(s)`, all projects build successfully targeting `net10.0`.

- [ ] **Step 2: Run all unit and integration tests**

Run: `dotnet test BeQuestionBank.Test --logger "console;verbosity=detailed"`
Expected: All tests pass.

- [ ] **Step 3: Verify Blazor Frontend compilation**

Run: `dotnet build FEQuestionBank.Client/FEQuestionBank.Client.csproj -c Release`
Expected: Build succeeds with 0 errors, confirming zero contract regressions.

- [ ] **Step 4: Audit for stale references or dead code**

Run: `git status` and search for any dangling references to `BEQuestionBank.Core` across the repository. Remove unused files.

- [ ] **Step 5: Final commit**

```bash
git add .
git commit -m "refactor: complete Clean Architecture and CQRS MediatR refactoring on .NET 10"
```
