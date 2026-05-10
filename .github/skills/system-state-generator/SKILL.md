---
name: system-state-generator
description: "Create or update architecture schema/diagram documentation from real code wiring. Use for requests like update architecture schema, update architecture diagram, update Mermaid flowchart, refresh system architecture docs, or generate current system state. For complex projects, generate a high-level document first, then ask whether to split into per-subsystem versions."
metadata:
  sourceUrl: https://github.com/github/awesome-copilot/tree/main
---

# System State Generator

Use this skill when the user asks for:
- current system state
- architecture snapshot from code
- runtime flow documentation
- Mermaid flowchart of the implemented system
- update architecture schema
- update architecture diagram
- update architecture flowchart
- refresh architecture docs
- update system diagram

Your task is to read the relevant code and generate a Markdown document that reflects the current implemented behavior.

## Output Target

- Prefer an existing project convention if present.
- If a similar file already exists (for example, src/SYSTEM_STATE.md), update it.
- If no convention exists, create SYSTEM_STATE.md at repository root.

## Required Content

Create these sections in the output file:
1. Scope
2. Main Runtime Components
3. Command or Request Lifecycle (or equivalent execution lifecycle)
4. Mermaid Flowchart
5. Operational Notes or Current Constraints

## Accuracy Rules

- Source of truth is current code wiring.
- Use README and other docs only as secondary hints.
- Do not invent components or flows.
- If a component exists but is not wired into active runtime flow, call that out explicitly.
- Keep claims short and verifiable.

## Mermaid Rules

- Include exactly one Mermaid flowchart in a fenced code block.
- Use GitHub-compatible Mermaid syntax.
- Use parser-safe labels (quote labels when needed).
- Reflect real control flow and component interactions.

## Complexity Decision Rule

Determine project complexity after quick architecture scan:
- Simple or medium complexity: produce one complete SYSTEM_STATE document.
- Large or complex architecture (multiple bounded contexts, services, major layers, or separate runtime surfaces):
  - First produce only a high-level general SYSTEM_STATE document.
  - Then ask the user this exact question:

"This project has multiple complex parts. Do you want me to generate separate SYSTEM_STATE files per subsystem (for example: API, background workers, UI, data layer, integrations)?"

  - Wait for user confirmation before generating split subsystem documents.

## Quality Bar

- Prefer concise implementation-oriented language.
- Focus on current runtime behavior, lifecycle, and boundaries.
- Avoid historical migration detail unless it affects current runtime state.
