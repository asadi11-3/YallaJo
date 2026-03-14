# YallaJo — AI Agent Instructions

> This file is auto-detected by AI coding tools (Claude Code, Cursor, Copilot, Windsurf, etc.)

## Before ANY Work

**Read these files in order. This is mandatory, not optional.**

1. `Agents/agent-context.md` — Project rules, conventions, architecture, gotchas, what's built, what's next
2. `Agents/error-log.md` — Mistakes previous agents made. Do NOT repeat them.
3. `Agents/guide.md` — Code patterns bible. Every line of code you write must match these patterns.

## When Implementing New Features

4. `Agents/YallaJo.md` — What to build: all endpoints, business logic, module specs
5. `Agents/YallaJo Business Rules & Edge Cases.pdf` — Every business rule and edge case
6. `Agents/Endpoints.pdf` — Endpoint definitions (visual reference)

## Scaffolding New Entities

- **Templates**: `Agents/templates/` — Copy-paste-ready code files for Entity, Command, Query, Handler, Validator, EF Config, Endpoints, DI
- **Script**: `Agents/scaffold.ps1` — Run to auto-generate all boilerplate files for a new entity

```powershell
# Example: scaffold a Place entity in ContentPlaces module
.\Agents\scaffold.ps1 -Module ContentPlaces -Entity Place -Schema content_places
```

**CRITICAL: Scaffold output is NEVER production-ready.** After running the script, you MUST review and customize EVERY generated file. See `Agents/agent-context.md` section "Scaffold & Template Usage Rules" for the mandatory review checklist. Skipping review = incomplete work that must be redone.

## Architecture Decisions

- `Agents/decisions/` — Why key decisions were made. Read before questioning existing patterns.

## After Completing Work

1. Update `Agents/agent-context.md` — Work Tracker, Module Status, What Needs To Be Done, Build State
2. Update `Agents/error-log.md` — Log any errors encountered with root cause and prevention rule
3. Verify `dotnet build` passes with 0 errors

## Key Rules (Summary)

- **Code patterns**: Follow `Agents/guide.md` exactly. No exceptions.
- **Security**: Every endpoint needs explicit auth. Never hardcode secrets. Validate all input.
- **Performance**: Always pass CancellationToken. Use projections. Paginate list endpoints.
- **Testing**: Write testable code. No static coupling. No hidden dependencies.
- **Git**: Don't commit without being asked. Don't push without being asked.
- **Errors**: Log every error to `Agents/error-log.md` with root cause and prevention rule.
