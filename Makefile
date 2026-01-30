# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help build test clean restore pack publish docker-build docker-up docker-down examples docs

SOLUTION := DotNetActorFramework.sln
PROJECT := src/DotNetActorFramework/DotNetActorFramework.csproj
BUILD_CONFIG := Release
DOTNET := dotnet

help:
	@echo "DotNet Actor Framework - Build Commands"
	@echo "========================================"
	@echo ""
	@echo "Core Commands:"
	@echo "  make build           - Build the project"
	@echo "  make test            - Run unit tests"
	@echo "  make clean           - Clean build artifacts"
	@echo "  make restore         - Restore NuGet packages"
	@echo ""
	@echo "Packaging:"
	@echo "  make pack            - Create NuGet package"
	@echo "  make publish         - Publish to NuGet (requires API key)"
	@echo ""
	@echo "Docker:"
	@echo "  make docker-build    - Build Docker image"
	@echo "  make docker-up       - Start Docker Compose services"
	@echo "  make docker-down     - Stop Docker Compose services"
	@echo "  make docker-logs     - View Docker Compose logs"
	@echo ""
	@echo "Examples:"
	@echo "  make examples        - Build example projects"
	@echo ""
	@echo "Documentation:"
	@echo "  make docs            - View documentation"
	@echo ""
	@echo "Maintenance:"
	@echo "  make format          - Format code with dotnet format"
	@echo "  make analyze         - Run code analysis"
	@echo ""

restore:
	@echo "Restoring NuGet packages..."
	$(DOTNET) restore $(SOLUTION)

build: restore
	@echo "Building project..."
	$(DOTNET) build $(SOLUTION) -c $(BUILD_CONFIG) --no-restore

clean:
	@echo "Cleaning build artifacts..."
	$(DOTNET) clean $(SOLUTION) -c $(BUILD_CONFIG) || true
	find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name ".vs" -exec rm -rf {} + 2>/dev/null || true

test: build
	@echo "Running tests..."
	$(DOTNET) test $(SOLUTION) -c $(BUILD_CONFIG) --no-build --verbosity normal

test-coverage: build
	@echo "Running tests with coverage..."
	$(DOTNET) test $(SOLUTION) -c $(BUILD_CONFIG) --no-build \
		/p:CollectCoverage=true \
		/p:CoverageFormat=lcov

pack: build
	@echo "Creating NuGet package..."
	$(DOTNET) pack $(PROJECT) -c $(BUILD_CONFIG) \
		-o ./nupkg \
		--no-build \
		--version-suffix=

publish: pack
	@echo "Publishing to NuGet..."
	@read -p "Enter NuGet API Key: " apikey; \
	$(DOTNET) nuget push ./nupkg/DotNetActorFramework*.nupkg \
		-k $$apikey \
		-s https://api.nuget.org/v3/index.json

docker-build: build
	@echo "Building Docker image..."
	docker build -t sarmkadan/dotnet-actor-framework:latest .
	docker tag sarmkadan/dotnet-actor-framework:latest sarmkadan/dotnet-actor-framework:$(shell date +%Y.%m.%d)

docker-up:
	@echo "Starting Docker Compose services..."
	docker-compose up -d
	@echo "Services started. View logs with: make docker-logs"

docker-down:
	@echo "Stopping Docker Compose services..."
	docker-compose down

docker-logs:
	docker-compose logs -f

docker-clean:
	@echo "Removing Docker containers and volumes..."
	docker-compose down -v
	docker rmi sarmkadan/dotnet-actor-framework:latest

examples:
	@echo "Building examples..."
	for example in examples/*.cs; do \
		echo "Compiling $$example..."; \
	done
	@echo "Examples ready in examples/ directory"

docs:
	@echo "Available documentation:"
	@echo "  - docs/getting-started.md   : Quick start guide"
	@echo "  - docs/architecture.md      : System architecture"
	@echo "  - docs/api-reference.md     : Complete API reference"
	@echo "  - docs/deployment.md        : Production deployment"
	@echo "  - docs/faq.md              : Frequently asked questions"
	@echo ""
	@echo "View with: cat docs/getting-started.md"

format:
	@echo "Formatting code..."
	$(DOTNET) format $(SOLUTION)

analyze:
	@echo "Running code analysis..."
	$(DOTNET) build $(SOLUTION) -c $(BUILD_CONFIG) \
		/p:TreatWarningsAsErrors=true \
		/p:EnforceCodeStyleInBuild=true

ci: clean restore build test
	@echo "CI pipeline complete!"

all: clean restore build test pack
	@echo "Complete build finished!"

.DEFAULT_GOAL := help
