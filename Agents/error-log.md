# YallaJo — Agent Error Log

> **Purpose**: Every AI agent MUST read this file at the start of each session and append to it when errors occur during work. This is how agents learn from each other's mistakes.

> **Rules**: Append-only. Never delete entries. Never edit past entries. Number sequentially.

---

## How to Use This File

1. **Read ALL entries below before writing any code** — these are mistakes previous agents made
2. **When you hit an error during work**, add a new entry IMMEDIATELY (before fixing)
3. **After fixing**, update your entry with the root cause and prevention rule
4. **Pay special attention to the "Prevention Rule"** field — these are the rules that matter most

---

## Error Log

*No errors logged yet. First agent to encounter an error should add ERR-001 below.*

<!-- 
### ERR-001: {Short descriptive title}
- **Date**: {YYYY-MM-DD}
- **Module**: {Which module were you working on}
- **What Happened**: {What you did that caused the error — be specific}
- **Error Message**: {Exact error message or symptom}
- **Root Cause**: {WHY it happened — the actual underlying reason}
- **Fix Applied**: {What you did to fix it}
- **Prevention Rule**: {A concrete rule future agents must follow to avoid this}
-->
