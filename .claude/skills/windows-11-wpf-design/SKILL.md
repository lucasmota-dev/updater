---
name: windows-11-wpf-design
description: Design and review modern Windows desktop interfaces built with WPF, including Windows 11-inspired layout, theme modes, accent colors, accessibility, and UI states.
---

# Windows 11 WPF Design

## Goal

Create a polished and practical desktop UI that feels at home on Windows 11. Prefer clarity, hierarchy, consistency, and fast everyday use over decoration.

## Visual direction

- Use Windows 11 as a reference, not as an instruction to copy every system screen.
- Use generous but controlled spacing, rounded corners, quiet surfaces, subtle borders, clear typography, and restrained depth.
- Use translucency/acrylic only when supported and readable. Never make body text depend on a busy background.
- Keep icon sizes, control heights, spacing, corner radii, and typography consistent.
- Keep primary actions obvious. Use destructive styling only for destructive actions.
- Provide a clear empty state, loading state, error state, success state, and disabled state.
- Avoid decorative charts, excessive cards, gradients everywhere, animations that delay work, and noisy dashboards.

## Themes and colors

- Implement `Sistema`, `Claro`, and `Escuro`.
- In system mode, follow the Windows setting where practical and document any WPF limitation.
- Centralize color resources. Do not hardcode repeated color values through XAML.
- Support a curated accent palette and a custom hexadecimal color with validation.
- Apply accent color selectively to primary buttons, selection, focus, progress, and interactive highlights.
- Provide `Restaurar padrão`.
- Persist appearance preferences per user under LocalAppData.
- Make sure text/background, disabled controls, error messages, selection, and focus remain distinguishable in all combinations.
- Never rely on color alone to communicate status.

## Accessibility and behavior

- Support keyboard navigation and visible focus.
- Use meaningful accessible names for icon-only buttons.
- Check contrast and do not disable selection indication in high-contrast environments.
- Respect DPI scaling, window resizing, and text scaling as far as the framework supports.
- Do not block the UI thread for process execution, discovery, or file I/O.
- Respect reduced-motion preferences where supported. Keep animations short and optional.
- Never use network-fetched images as a requirement for UI rendering.

## WPF implementation

- Inspect the current .NET/WPF documentation before choosing Fluent theme APIs or third-party libraries.
- Check whether built-in Fluent/theme APIs are stable for the selected target framework. Do not rely on an experimental API without documenting the risk.
- Prefer shared resource dictionaries and reusable styles over copy/paste control templates.
- Use bindings and commands for view state. Put business and process logic outside code-behind.
- Avoid a large monolithic XAML file. Extract only meaningful views/resources; do not create empty abstractions.
- Ensure theme changes update existing controls without restart when practical.

## Review checklist

- Is the primary action apparent within seconds?
- Can the user understand current state and what is safe to do next?
- Does every theme have readable text, borders, icons, selection and focus?
- Does the accent color work on both light and dark surfaces?
- Does the layout remain usable at smaller and larger window sizes?
- Are loading and error states informative without overwhelming the user?
