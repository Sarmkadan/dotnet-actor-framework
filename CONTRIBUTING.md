# Contributing to DotNetActorFramework

Thanks for contributing. This guide covers building, testing, and submitting changes.

## Prerequisites

- .NET 10 SDK. The projects target `net10.0` and there is no `global.json`, so any 10.0 SDK works.
- Git.

Check your setup with `dotnet --version`.

## Build

```bash
dotnet restore DotNetActorFramework.sln
dotnet build DotNetActorFramework.sln -c Release --no-restore
```

`make build` runs the same steps.

## Test

```bash
dotnet test DotNetActorFramework.sln -c Release
dotnet test --filter "FullyQualifiedName~ActorPathTests"   # one test class
```

Tests live in `tests/dotnet-actor-framework.Tests/` (xUnit, FluentAssertions, Moq). Put new tests there.
`src/DotNetActorFramework.Tests/` holds stray files that are not part of the solution.

## Coding Conventions

- 4-space indentation, UTF-8, lines up to 120 columns (see `.editorconfig`).
- `Nullable` and `ImplicitUsings` are enabled. Keep new code free of nullable warnings.
- Use file-scoped namespaces, as most existing files do.
- Every public type and member needs an XML doc comment (`GenerateDocumentationFile` is on).
- Naming: PascalCase for types and members, `I` prefix for interfaces, and the suffixes `*Service`,
  `*Middleware`, `*Options`, and `*Extensions` where they fit.
- Keep the author header block at the top of any file you edit. Do not remove it.
- Tests: one `<Type>Tests.cs` file per type, method names like `Method_Scenario_ShouldExpected`,
  Arrange-Act-Assert structure, and FluentAssertions `.Should()` assertions.
- Run `make format` before committing to apply `dotnet format`.

## Pull Requests

1. Branch from `main`, for example `git checkout -b feat/short-name`. If you do not have push access,
   fork the repository first.
2. Keep each PR focused on one feature, fix, or refactor.
3. Use conventional commit prefixes: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, or `chore:`.
4. Build and run the tests locally before pushing.
5. Make sure CI is green. `.github/workflows/ci.yml` runs restore, build, and test on .NET 10 for pushes
   and pull requests targeting `main`.
6. Describe the change in the PR body: what changed, why, how you tested it, and any impact on the public
   API or runtime behavior. For architectural changes, explain the reasoning (thread safety, middleware
   ordering, message immutability).

## Reporting Issues

Use GitHub Issues. For bugs, include reproduction steps, expected and actual behavior, the framework
version, the output of `dotnet --version`, and your OS. For feature requests, describe the problem,
your proposed API, and any alternatives you considered.

## License

By contributing, you agree that your changes are licensed under the MIT License (see `LICENSE`).
