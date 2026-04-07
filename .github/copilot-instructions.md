# Copilot Instructions

## Core Runtime Guardrails
- Keep all gameplay/runtime-facing components compatible with `.NET 6.x`.
- Do not retarget runtime projects beyond `net6.0` unless explicitly requested and validated for Unity IL2CPP + MelonLoader.

## Mandatory System Architecture Prompt
- Apply `.github/instructions/gregframework_system_architecture.instructions.md` to all implementation and design decisions.
- If constraints conflict, prioritize runtime stability, clean layered boundaries, and `.NET 6` compatibility.

## SonarQube MCP Rules
- Apply `.github/instructions/sonarqube_mcp.instructions.md` whenever SonarQube MCP tooling is used.

## Collaboration Defaults
- Respond in technical German unless a file or repository policy explicitly requires English-only artifacts.
- Summarize intent before code changes.
- Keep refactors minimal and architecture-safe.

## Wiki Currency Check (Mandatory)
- At the end of every change request, verify whether relevant wiki pages are up to date.
- If updates are required, list the pages and include them in follow-up recommendations.
