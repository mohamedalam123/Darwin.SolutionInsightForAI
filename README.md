# Darwin.SolutionInsightForAI

**Solution Insight for AI** — A .NET 9 console tool that scans a .NET solution/repo and exports a structured map of files, types, and members to help AI assistants (e.g., ChatGPT) understand the codebase and accelerate coding tasks.

## Features (Phase 1)
- **Project Mapping (JSON)**  
  Lists important files (`.cs`, `.cshtml`, `.html`, `.htm`, `.js`, `.css`).  
  For `.cs` files, extracts:
  - Class/Record name **and full signature**
  - Optional cleaned leading comments (XML `<summary>` and `///` removed; `//` supported)
  - Method/Constructor **full signature line** (up to but not including `{`), plus optional cleaned comments
- **Full Code Extract**  
  Aggregates all `.cs` files under a given path into a single text file (with per-file separators).

## Why
Large solutions are hard for LLMs to navigate. This tool builds a machine-friendly map so your assistant can quickly jump to the right files, classes, and members.

## Getting Started
```bash
dotnet build
dotnet run --project src/Darwin.SolutionInsightForAI.App/Darwin.SolutionInsightForAI.App.csproj
