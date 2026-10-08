# Style Guide

This file defines the shared file, code, documentation, and design style for new projects. Project-specific rules may narrow or extend these rules when the stack or existing code requires it.

## Philosophy

- Prefer the smallest correct solution.
- Keep code clean, compact, readable, efficient, modular, and maintainable.
- Add complexity only when it solves a real problem.
- Prefer clear names and simple structure over comments explaining unclear code.
- Optimize for the relevant scenario, but do not sacrifice readability for trivial savings.
- Do not add abstractions, helpers, patterns, or indirection without a practical reason.
- Prefer immutable values and limited mutable state unless mutability has a clear purpose.
- Treat existing successful project patterns as stronger evidence than abstract rules.
- Record useful new patterns as examples over time instead of trying to define every case in advance.

## File And Directory Names

- Use `camelCase` for ordinary file names unless the language convention requires another form (Rust sources use `snake_case.rs`; keep existing C header/source names in a C tree).
- Use lowercase file extensions, such as `.md`, `.ps1`, `.cs`, `.xaml`, `.axaml`, `.rs`, `.c`, `.h`, and `.json`.
- Preserve established uppercase names for conventional files such as `README.md`, `AGENTS.md`, `CHANGELOG.md`, and `VERSION`.
- Windows project launch shortcuts use the conventional root-level name `run.lnk`.
- Use the exact project name for the project root directory.
- Use `camelCase` for ordinary project directories such as `source`, `tests`, and `documentation`.
- C# sample folders follow language and framework names: `windows` (window markup), `ViewModels`, `Assets`. Do not name window markup `Views`.
- Allowed dotted layout directories: `.scripts`, `.version`, `.installer`, `.res`, `.config`.
- Leading-dot file names apply only inside `.scripts`. The only allowed dotted script file is `.run.ps1`. Other operator and step scripts use ordinary `camelCase.ps1` names.
- A file name should describe its primary responsibility without unnecessary prefixes, suffixes, or abbreviations.
- Documentation files use `camelCase.md` unless they are established conventional files.

## Naming

- Use `camelCase` for functions, methods, variables, parameters, properties, and ordinary members.
- Use `HTTPclient`-style casing for names containing acronyms: preserve the acronym in uppercase and begin the following word with lowercase.
- Use `UPPER_SNAKE_CASE` for public or shared constants.
- Use `_UPPER_SNAKE_CASE` for private constants.
- Use `_camelCase` for private members.
- Use `camelCase` for types unless the language or framework convention requires another form. Follow the language rather than forcing generic casing: C# types PascalCase, C# methods/properties PascalCase, C# parameters and locals camelCase, C# private fields `_camelCase`; Rust types `PascalCase`, Rust functions/modules `snake_case`; PowerShell functions camelCase (`SCRIPTS.md`); C identifiers follow the existing tree.
- Use names that state intent and behavior. Avoid vague names such as `data`, `thing`, `stuff`, or `helper` when a more precise name is practical.
- Do not shorten names merely to save a few characters. Common, obvious abbreviations are acceptable when they improve clarity.

## File Modularity

- Keep related code together when it is only used in one place.
- Extract code into a new module when it is used in multiple places, prevents copy/paste, or forms a coherent reusable responsibility.
- Do not split files solely to meet an arbitrary line count.
- Keep each module focused on a clear responsibility.
- Avoid circular dependencies and unnecessary layers between callers and the code they use.
- Prefer one implementation of shared behavior rather than several nearly identical versions.
- Keep public surfaces small. Expose only what other modules need.

## Structure And Formatting

- Keep code compact when it remains readable and fits reasonably on one line without excessive horizontal scrolling.
- Use a soft line-length target of 100 characters. User-edited content is a valid exception when wrapping it would make the content worse or alter its intended presentation.
- Do not force one-line formatting when it makes a condition, expression, object, collection, or block difficult to scan.
- Braces are optional for short, obvious blocks when the language permits it. Use braces when a block is long, nested, easy to misread, or likely to grow.
- Keep related declarations and operations together.
- Order imports, declarations, and members according to the most logical and efficient structure for the relevant language and file. Do not apply arbitrary alphabetical ordering when grouping by dependency or execution responsibility is clearer.
- C# usings: `System*` first, then third-party, then project namespaces; keep blank-line groups when the file already uses them. Put the type's public surface before private helpers unless the file already groups by feature.
- Rust `use`: crate/std, then external crates, then `crate::` / `super::`. Keep `mod` declarations at the top of the file that owns them.
- C includes: the matching header first (in a `.c` file), then system headers, then project headers. Keep related functions together rather than sorting A-Z.
- Keep public entry points easy to find and supporting implementation details nearby.
- Avoid blank lines that fragment a small piece of logic. Use them to separate meaningful stages or responsibilities.

## Functions And Flow

- Give each function one clear responsibility.
- Split a function when it contains multiple responsibilities, repeated logic, reusable logic, or a section that is difficult to understand in context.
- Do not use a strict line limit. Judge function size by responsibility, readability, nesting, and reuse.
- Prefer guard clauses and early returns when they reduce nesting and make the main path clear.
- Do not add guard clauses that make simple code longer or harder to follow.
- Prefer `switch`, loops, tables, or other direct structures over long repeated conditionals when they improve readability, speed, or efficiency.
- Do not compact logic merely to reduce line count.
- Keep the normal flow obvious and avoid unnecessary state transitions.
- Validate at the boundary of a function or module when practical, then keep deeper logic focused on its valid inputs.
- Handle errors at the level that can make a meaningful decision. Do not silently ignore failures or add unexplained fallbacks.

## Comments And Work Markers

- Do not use comments for ordinary narration, obvious operations, or documentation that belongs in a separate file.
- Use comments only to explain genuinely complex behavior that clear code cannot adequately express.
- Use `WIP:` only for work currently in progress. Remove it when the work is complete.
- Use `TODO:` only for work that has not started and still needs to be done.
- Keep comments short, factual, and specific.
- Do not leave stale comments after changing the code they describe.

## Performance And Complexity

- Consider performance when choosing between otherwise readable approaches.
- Prefer an efficient approach when it does not make the code meaningfully harder to understand.
- Do not optimize speculative problems or add abstractions with no practical benefit.
- Replace long repeated branching with a loop, lookup, switch, or other structure when that improves the relevant code rather than merely shortening it.
- Preserve correctness and clear behavior before pursuing micro-optimizations.

## Documentation And Markdown

- Keep documentation concise, direct, and useful to its intended reader.
- Use conventional Markdown headings and formatting without adding decorative structure unnecessarily.
- Use fenced code blocks with language identifiers when showing code.
- Use short paragraphs and focused lists when they improve scanning.
- Follow successful existing documentation examples in the project.
- Add new documentation conventions only when repeated project usage shows that they are useful.
- Keep documentation files named with `camelCase.md`, except for conventional names such as `README.md` and `AGENTS.md`.

## Unresolved Rules

The following areas remain intentionally open and should be refined with project examples:

- Async function naming and when asynchronous code is appropriate.
- Debug output, logging, and temporary diagnostics. Do not add debug output unless explicitly requested or an active debug agent mode grants temporary permission; remove temporary probes before completion.
- More detailed import ordering beyond the C# / Rust / C grouping already listed.
- More detailed declaration and member ordering beyond the notes already listed.
- Additional examples for function decomposition and compact formatting.
