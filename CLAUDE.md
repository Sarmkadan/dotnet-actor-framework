# CLAUDE.md

## Overview
Lightweight actor-model framework for .NET 10 (mailboxes, supervision trees, clustering, persistence), published as NuGet package `Zaiets.dotnet.actor.framework`.

## Build
```bash
dotnet restore DotNetActorFramework.sln
dotnet build DotNetActorFramework.sln -c Release --no-restore
make build            # same via Makefile
make pack             # NuGet package -> ./nupkg
make docker-build     # Docker image sarmkadan/dotnet-actor-framework
```
Requires .NET 10.0 SDK. CI (`.github/workflows/ci.yml`) builds and tests on 8.0.x (allowed to fail) and 10.0.x.

## Test
```bash
dotnet test DotNetActorFramework.sln -c Release
dotnet test --filter "FullyQualifiedName~ActorPathTests"    # single class
make test-coverage                                           # lcov coverage
```
Stack: xunit 2.9, FluentAssertions 7, Moq. Benchmarks: `benchmarks/DotNetActorFramework.Benchmarks` (BenchmarkDotNet).

## Lint / Format
```bash
make format      # dotnet format DotNetActorFramework.sln
make analyze     # build with TreatWarningsAsErrors + EnforceCodeStyleInBuild
```
Style is defined in `.editorconfig` (4-space indent, 120 col, Allman braces, block-scoped namespaces, `var` only when type is apparent).

## Key directories
- `src/DotNetActorFramework/` - the library (`Program.cs` is a demo entry point)
  - `Models/` - `Actor`, `ActorSystem`, `ActorRef`, `ActorPath`, `Envelope`, `Message`
  - `Services/` - `ActorRegistry`, `MailboxService`, `BoundedMailbox`, `MessageDispatcher`, `SupervisionService`, `ClusterActorRegistry`
  - `Messaging/` - Ask/request-response; `Middleware/` - pipeline (auth, rate limit, metrics, errors)
  - `Persistence/`, `Events/`, `Routing/`, `Caching/`, `Configuration/`, `Options/`, `Diagnostics/`, `Api/`, `Testing/` (`MockActorContext`)
- `tests/dotnet-actor-framework.Tests/` - xunit test project (the one in the .sln)
- `src/DotNetActorFramework.Tests/` - stray test files, not in the solution
- `benchmarks/`, `examples/` (numbered standalone `.cs` samples), `docs/` (per-type markdown + guides)
- Loose `.cs` files in repo root (exceptions, `LoadBasedRouter.cs`, `Test.cs`, a file named `}`) are leftovers; do not treat them as part of the build.

## Conventions
- Every `.cs` file starts with the author header block (`Author: Vladyslav Zaiets | https://sarmkadan.com`) - never remove it.
- `Nullable` and `ImplicitUsings` enabled; all public APIs need XML doc comments (`GenerateDocumentationFile=true`).
- Naming: PascalCase types/members, `I`-prefixed interfaces, `*Extensions` for extension classes, `*Service` for services, `*Middleware` for pipeline stages, `*Options`/`*Configuration` for settings.
- Tests: one `<Type>Tests.cs` per type, method names `Method_Scenario_ShouldExpected`, Arrange-Act-Assert, FluentAssertions `.Should()`.
- Commits: conventional prefixes (`docs:`, `chore:`, `feat:`, `fix:`).
