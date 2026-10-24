---
name: thermo-nuclear-code-generation
description: Generate code with an extremely strict focus on maintainability, structural simplicity, abstraction quality, modularity, and long-term codebase health. Use when implementing features or refactoring code where implementation quality matters as much as behavior.
disable-model-invocation: true
---

# Thermo-Nuclear Code Generation

Use this skill when generating or restructuring code. Its purpose is to prevent maintainability problems before they enter the codebase.

Above all, be ambitious about code structure. Do not merely produce code that works. Look for "code judo": implementations that preserve the required behavior while using fewer concepts, branches, layers, special cases, and moving pieces.

## Core Prompt

Start from this baseline:

> Implement the requested behavior with the simplest maintainable structure possible.
> Before coding, understand the existing architecture, ownership boundaries, canonical helpers, and related implementations.
> Prefer direct, modular, legible code over patches that merely make the feature work.
> Actively look for a "code judo" restructuring that removes complexity instead of moving it around.
> Keep behavior within the requested scope and avoid unrelated churn.
> Measure twice, cut once.

## Non-Negotiable Implementation Standards

### 0. Be ambitious about structural simplification

- Search for a design that makes entire branches, helpers, modes, conditionals, or layers unnecessary.
- Prefer changing the model or ownership boundary when that makes the implementation substantially simpler.
- If complexity can be deleted rather than reorganized, delete it.
- Do not settle for a clean-looking version of a fundamentally messy approach.
- Prefer solutions that feel inevitable in hindsight.

### 1. Respect healthy file boundaries

- Do not push a file from under 1000 lines to over 1000 lines without a very strong structural reason.
- Treat crossing 1000 lines as a design smell and decompose first.
- Extract focused helpers, components, modules, services, or local abstractions when that improves cohesion.
- Do not split files mechanically just to satisfy a line count; decomposition must improve ownership and readability.

### 2. Do not introduce spaghetti growth

- Do not bolt new feature checks, flags, or special cases into unrelated existing flows.
- Be suspicious of repeated `if`/`else` chains, scattered conditionals, and one-off branches.
- When branching is growing, ask whether the model, dispatcher, policy, state machine, helper, or module should own the behavior instead.
- Keep the main flow easy to scan and reason about.
- Avoid temporary branching that is likely to become permanent technical debt.

### 3. Prefer clean design over merely working code

- A working implementation is not sufficient if it unnecessarily makes the codebase harder to maintain.
- Prefer fewer moving pieces, fewer concepts, and clearer ownership.
- Avoid refactors that only relocate the same complexity into more files.
- When behavior can remain identical while structure becomes materially simpler, choose the simpler structure.

### 4. Prefer direct, boring, maintainable code

- Prefer explicit control flow over clever or magical mechanisms.
- Avoid generic abstractions that hide simple data-shape assumptions.
- Do not add wrappers, factories, adapters, or helper layers unless they remove meaningful complexity or establish a useful boundary.
- Reject identity wrappers and pass-through abstractions that only add indirection.
- Optimize for the next developer reading the code, not for abstraction density.

### 5. Keep types and boundaries explicit

- Prefer explicit models and contracts over loosely-shaped ad-hoc objects.
- Avoid unnecessary `any`, `unknown`, casts, nullable states, optional parameters, and silent fallbacks.
- Do not use weak typing to hide an unclear invariant.
- When a boundary is unclear, make the contract explicit so the control flow becomes simpler.
- Keep data transformations close to the layer that owns them.

### 6. Keep logic in the canonical layer

- First find the existing canonical utility, helper, service, module, or abstraction for the behavior.
- Reuse established project patterns instead of creating bespoke near-duplicates.
- Keep feature-specific logic out of shared/general-purpose paths unless the shared layer genuinely owns the concept.
- Put business logic in the layer responsible for that concept.
- Do not leak implementation details across API, service, domain, UI, or infrastructure boundaries.

### 7. Keep orchestration simple and state changes safe

- Separate orchestration from business logic when doing so makes the flow clearer.
- Do not serialize independent operations without a reason; use parallel execution when it simplifies the implementation and preserves correctness.
- Prefer atomic handling for related state changes when partial updates would create harder-to-reason-about states.
- Avoid orchestration code that becomes a chain of incidental steps and special cases.

### 8. Reuse before creating

Before adding a helper, abstraction, or utility:

- Search the codebase for an existing canonical implementation.
- Check nearby features for established patterns.
- Reuse it when it matches the responsibility.
- If it does not match, decide whether the existing abstraction should be extended rather than duplicated.
- Only create a new abstraction when there is a clear ownership or reuse reason.

## Primary Implementation Questions

Before and during implementation, ask:

- Is there a code-judo move that makes this dramatically simpler?
- Can the model be changed so branches disappear?
- Can the implementation use fewer concepts, states, or helper layers?
- Is this logic being added to the correct file and architectural layer?
- Am I increasing coupling or statefulness in a previously cohesive module?
- Will this change make an existing file excessively large?
- Are repeated conditionals signaling a missing abstraction or explicit model?
- Is the control flow direct and legible?
- Does every abstraction earn its indirection?
- Am I introducing casts, optionality, or weak object shapes because the boundary is unclear?
- Is there already a canonical helper for this?
- Am I moving complexity around instead of actually deleting it?
- Can independent work be orchestrated more simply?
- Can related updates be made more atomic?

## What to Prevent Aggressively

Do not generate or preserve:

- Complicated implementations when a simpler structural reframing is available.
- Large functions that combine orchestration, business rules, transformations, and side effects unnecessarily.
- Files that cross 1000 lines because of the new feature when decomposition is viable.
- New special-case branches inserted into unrelated flows.
- One-off booleans, nullable modes, or flags that complicate the state model.
- Feature logic leaking into generic modules.
- Magic or generic mechanisms hiding straightforward assumptions.
- Thin wrappers that only rename or forward calls.
- Unnecessary casts, `any`, `unknown`, or optional parameters.
- Copy-pasted logic that should be shared through a focused abstraction.
- Edge-case handling buried inside already busy functions.
- Duplicate helpers when a canonical utility already exists.
- Logic implemented in the wrong package, module, or layer.
- Sequential async orchestration when independent work can remain simple and parallel.
- Partial-update flows that unnecessarily expose intermediate invalid state.
- "Temporary" hacks or compatibility branches without a clear ownership boundary.

## Preferred Structural Patterns

When appropriate, prefer:

- A pure function over duplicated inline logic.
- A focused module over a growing monolithic file.
- A typed model over repeated condition chains.
- An explicit dispatcher over scattered feature checks.
- A dedicated abstraction for feature-specific rules.
- A single clear flow over duplicated branches.
- Direct code over unnecessary wrappers.
- Existing canonical utilities over bespoke helpers.
- A clear ownership boundary over cross-layer leakage.
- Explicit contracts over casts and weakly-shaped objects.
- Parallel orchestration for genuinely independent work.
- Atomic updates for related state transitions.

Do not introduce these patterns mechanically. Use them only when they make the structure simpler and clearer.

## Implementation Process

### 1. Understand before modifying

Inspect the relevant code paths, related features, existing helpers, and architectural conventions before deciding where the change belongs.

Do not scan or modify unrelated areas merely for completeness. Stay within the requested scope while understanding enough of the surrounding architecture to place the change correctly.

### 2. Identify the canonical home

Determine which file, module, service, component, or layer should own the new behavior.

Reuse existing abstractions wherever they already represent the concept.

### 3. Design the simplest model first

Before writing conditionals, identify the states, invariants, ownership, and data contracts involved.

Look specifically for ways to remove branches by improving the model.

### 4. Implement the smallest coherent structure

Prefer a small number of focused units with clear responsibilities.

Keep orchestration separate from business logic when that materially improves readability.

Avoid speculative abstractions for hypothetical future features.

### 5. Check for structural debt immediately

After implementation, review the code as if performing a strict maintainability audit:

- Did the file grow excessively?
- Did branching complexity increase?
- Did I duplicate existing logic?
- Did I introduce an abstraction that does not earn its keep?
- Did logic land in the wrong layer?
- Did I weaken a type boundary?
- Did I move complexity around instead of removing it?
- Is there a simpler code-judo version of what I just wrote?

### 6. Refactor before finishing

If a materially cleaner structure is available, use it before declaring the implementation complete.

Do not leave known structural problems simply because the current version already works.

## Scope Discipline

- Implement only the requested behavior and the structural changes required to implement it well.
- Avoid unrelated API, UI, database, or architecture churn.
- Do not perform broad cleanup solely because nearby code is imperfect.
- Structural refactoring is justified when it is directly required to keep the requested change maintainable.
- Preserve existing behavior unless the task explicitly requires changing it.

## Quality Bar

An implementation is not complete merely because tests pass or the feature works.

The expected bar is:

- No clear structural regression.
- No obvious missed opportunity for dramatic simplification.
- No unjustified file-size explosion.
- No unnecessary spaghetti growth.
- No hacky or magical abstraction that makes the design harder to reason about.
- No unnecessary wrapper, cast, optionality, or weak contract obscuring the real design.
- No obvious architecture-boundary leak.
- No duplicate canonical helper or misplaced logic when a clear existing home exists.
- No obvious decomposition opportunity that would materially improve maintainability.

## Final Self-Review

Before finishing, mentally review the implementation with these questions:

> Does this code merely work, or is it genuinely well-structured?
> Could I delete a branch, helper, mode, layer, or special case by changing the design?
> Did I add concepts that were not necessary?
> Did I reuse the project's canonical abstractions?
> Is ownership obvious from the code structure?
> Can another developer understand the main flow without tracing scattered conditions?
> Would this design still feel reasonable after the feature grows?
> Did I leave the codebase cleaner or at least no worse than I found it?

When a simpler design is clearly available, prefer it even when the current implementation is already functional.
