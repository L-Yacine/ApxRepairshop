# Session Handoff

## Current State

The repository is an ASP.NET Core MVC project for the Algerian phone repair shop PRD. Slice 1 has been implemented and needs owner manual validation. Git has been initialized with a first commit.

## Last Completed

- Implemented cookie authentication for staff-only access.
- Added temporary in-memory staff accounts:
  - Owner: username `owner`, password `owner123`
  - Worker 1: username `worker1`, password `worker123`
  - Worker 2: username `worker2`, password `worker123`
- Added Arabic RTL login page and authenticated app shell.
- Added owner-only `DashboardController` and placeholder owner dashboard.
- Replaced the default home page content with an Arabic staff landing page.
- Updated `TASKS.md` to mark Slice 1 implemented pending owner validation.
- Added `.gitignore` for .NET build output and user-specific IDE files.
- Initialized Git and created the first repository commit.

## Next Steps

1. Owner runs restore/build/run commands and manually validates Slice 1.
2. If validation passes, start Slice 2: Create Repair Ticket With Customer Lookup and Receipt.
3. Before production, replace temporary in-memory staff credentials with persisted accounts and owner-managed password setup.

## Blockers

- No current coding blocker.
- Owner manual validation is still needed.

## Owner Commands Needed

```bash
dotnet restore MimoShop.slnx
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

## Manual Validation Needed

Open the local URL shown by `dotnet run --project MimoShop`.

1. Unauthenticated redirect:
   - Open `/`.
   - Expected: browser redirects to `/Account/Login`.

2. Invalid login:
   - Enter username `owner` and password `wrong`.
   - Expected: Arabic validation message says the username or password is incorrect.

3. Owner login and dashboard:
   - Log in with `owner` / `owner123`.
   - Expected: Arabic RTL home page appears, staff name is `صاحب المحل`, and nav includes `لوحة المالك`.
   - Open `/Dashboard`.
   - Expected: owner dashboard placeholder loads.

4. Worker login and owner restriction:
   - Log out.
   - Log in with `worker1` / `worker123`.
   - Expected: Arabic RTL home page appears, staff name is `العامل 1`, and nav does not include `لوحة المالك`.
   - Open `/Dashboard`.
   - Expected: access denied page appears.

5. Mobile shell:
   - Resize browser to a phone-sized viewport.
   - Expected: navbar collapses, Arabic RTL layout remains readable, and logout still works.

## Important Rules

- Follow `AGENTS.md`.
- Do not run package installs or `dotnet` restore/build/run/test commands in the container.
- Do not add automated tests.
- For schema changes, ask the owner to run the EF Core migration scaffold command instead of hand-writing migrations.
