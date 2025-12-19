# Contributing to DotNetActorFramework

First off, thank you for considering contributing to DotNetActorFramework! It's people like you that make open source such a great community.

## Development Requirements

To build and run tests for this project, you will need:
- .NET 10.0 SDK

## How to Contribute

1. **Fork the Repository:** Start by forking the repository to your own GitHub account.
2. **Clone the Repository:** Clone the forked repository to your local machine.
3. **Create a Branch:** Create a new branch for your feature or bug fix (`git checkout -b feature/your-feature-name`).
4. **Make Changes:** Write your code, following the guidelines below.
5. **Run Tests:** Ensure all existing and new tests pass locally before submitting your changes.
6. **Submit a PR:** Push your branch to your fork and submit a Pull Request to the main repository.

## Code Style and Conventions

- **Follow existing conventions:** Please maintain the coding style currently used in the project.
- **XML Documentation:** Ensure all public APIs, methods, and classes include proper XML documentation comments.
- **Author Headers:** **KEEP ALL author headers** - DO NOT remove them from existing files. If you modify a file, leave the existing author headers intact.

## Testing Requirements

All contributions must include appropriate test coverage:

```bash
# Run the full test suite
dotnet test

# Run with code coverage reporting
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Run a specific test class
dotnet test --filter "FullyQualifiedName~ActorPathTests"
```

**Guidelines:**
- New public methods must have corresponding unit tests.
- Aim for a minimum of 80% code coverage on changed files.
- Tests should follow the Arrange-Act-Assert pattern.
- Use the `MockActorContext` from `DotNetActorFramework.Testing` for actor-level tests.

## Architecture Decisions

When proposing architectural changes, please document the reasoning in your PR description. Key areas that require careful consideration:

- **Thread safety**: All shared state in actors uses `lock` objects. Follow this pattern in new code.
- **Middleware ordering**: New middleware should specify an `Order` value that fits logically in the pipeline.
- **Message immutability**: Messages are `record` types and should remain immutable after construction.

## Reporting Issues

If you find a bug or have a feature request, please use GitHub Issues. 

When reporting a bug, please include:
- A clear and descriptive title.
- Detailed reproduction steps.
- Expected behavior vs. actual behavior.
- Version of the framework you are using.
- .NET SDK version (`dotnet --version`).
- Operating system and version.

### Feature Requests

For feature requests, describe:
- The problem you are trying to solve.
- Your proposed solution or API design.
- Any alternatives you have considered.
- Whether you are willing to implement it yourself.

## License

By contributing to DotNetActorFramework, you agree that your contributions will be licensed under its MIT License.
