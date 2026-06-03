# Repository Guidelines

## Project Structure & Module Organization

This is a fresh ASP.NET Core MVC project. The solution file is `MimoShop.slnx`, and the application project lives in `MimoShop/`.

- `MimoShop/Program.cs` contains application startup and middleware wiring.
- `MimoShop/Controllers/` contains MVC controllers.
- `MimoShop/Models/` contains view models and domain models as they are introduced.
- `MimoShop/Views/` contains Razor views, including shared layout files under `Views/Shared/`.
- `MimoShop/wwwroot/` contains static assets such as CSS, JavaScript, Bootstrap, jQuery, and images.
- `repair-shop-prd-v1.md` is the product requirements source for feature work.

## Build and Development Commands

The repository owner runs all .NET commands manually. Agents must provide commands for the owner to execute and continue with code changes only.

- `dotnet restore MimoShop.slnx` restores NuGet dependencies.
- `dotnet build MimoShop.slnx` compiles the solution.
- `dotnet run --project MimoShop` starts the web app locally.

For schema changes, do not hand-write EF Core migrations. Ask the owner to scaffold them, for example:

```bash
dotnet ef migrations add <MigrationName> --project MimoShop --startup-project MimoShop
```

## Coding Style & Naming Conventions

Use standard C# conventions: four-space indentation, PascalCase for public types and members, camelCase for locals and parameters, and async method names ending in `Async`. Keep controllers focused on HTTP flow and move business rules into services as the project grows.

Name controllers with the `Controller` suffix, Razor views after their action names, and view models with a `ViewModel` suffix.

## Manual Validation Guidelines

All validation is manual. Do not add automated test projects or run `dotnet test`. For each change, provide the owner with clear manual checks, including pages to open, forms to submit, expected results, and any edge cases from `repair-shop-prd-v1.md`.

## Commit & Pull Request Guidelines

No Git history is available in this workspace, so use clear, imperative commit messages such as `Add repair order intake model` or `Wire customer lookup view`.

Pull requests should include a summary, validation performed, linked issue or PRD section, screenshots for UI changes, and notes for migration or configuration steps.

## Agent-Specific Instructions

Agents must not run package installs, `dotnet restore`, `dotnet build`, `dotnet run`, or `dotnet test` in this container. Do not add automated tests. Provide exact commands and manual validation steps for the owner instead.

Maintain `TASKS.md` as the durable PRD-driven task list. Maintain `SESSION_HANDOFF.md` at the end of each session with current state, completed work, next steps, blockers, owner commands needed, and manual validation needed.
