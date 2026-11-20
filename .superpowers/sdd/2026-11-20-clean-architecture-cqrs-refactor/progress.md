# SDD ledger — plan: docs/superpowers/plans/2026-11-20-clean-architecture-cqrs-refactor.md

## Pre-flight
- Pre-flight: scanned shared interfaces across tasks 1-8.
- Target framework: net10.0, C# 14.
- Inward dependency flow: Domain -> Application -> Infrastructure -> API.
- Shared contract preservation for FEQuestionBank.Client.

## Tasks Progress
- [x] Task 1: Solution & Projects Scaffolding for .NET 10 & C# 14
- [ ] Task 2: Domain Layer Purity & Exceptions Hierarchy
- [ ] Task 3: Application Core (MediatR, FluentValidation, Pipeline Behaviors & Interfaces)
- [ ] Task 4: Infrastructure Persistence & Services Restructuring
- [ ] Task 5: Application Feature Slices - Auth, Khoa, MonHoc, Phan, NguoiDung
- [ ] Task 6: Application Feature Slices - CauHoi, DeThi, YeuCauRutTrich, File, Tool
- [ ] Task 7: API Layer Modernization & Controller Thinning
- [ ] Task 8: End-to-End Build, Frontend & Test Verification
