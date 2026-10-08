# Theming Guide

This guide captures the visual language repeated across `musicApp`, `ghostwriterpp`,
`RNGesus`, and `badussyBoard`. Use it as a starting point for new projects, then
trim the palette to the roles the project actually needs.

## Core Preferences

- Prefer dark-first or dark-only themes.
- Build depth with several near-black and charcoal surfaces instead of pure black.
- Use purple as the default brand, action, selection, and focus accent.
- Keep primary text white or near-white, with a deliberate gray hierarchy for secondary text,
  metadata, disabled states, and borders.
- Make controls compact, flat, and quiet. Avoid glossy gradients, excessive shadows, and noisy
  decoration.
- Use square corners or restrained rounding. Typical rounding is `0` to `10px`; larger cards may
  use `20px`.
- Restyle or reduce framework-default chrome when it fights the design: ripples, redundant focus
  decoration, grid lines, heavy borders, and oversized controls. Every interactive control must keep a
  visible, high-contrast keyboard-focus indication.
- Use monospace text for code, file names, hotkeys, numeric values, and technical readouts.
- Preserve accessible contrast. Accent color is not a substitute for text or state labels.

## Default Color Roles

These are the most reusable values across the projects. They are references, not a requirement to
use every shade in one theme.

| Role | Preferred value | Alternate values |
| --- | --- | --- |
| App background | `#1B1B1B` | `#121417`, `#151719`, `#1E1E1E` |
| Deep surface | `#151515` | `#181818`, `#1A1A1A` |
| Primary panel | `#2D2D2D` | `#252A30`, `#282828`, `#2B2B2B` |
| Raised or row surface | `#2C2C2C` | `#292929`, `#2A2A2A` |
| Primary purple | `#765E98` | `#765D9A`, `#7D64A0`, `#705399` |
| Strong brand purple | `#523E6E` | `#4D3C64`, `#5E4B79` |
| Light purple accent | `#AC98C7` | `#9373C0`, `#9C89B0`, `#856CAA` |
| Primary text | `#FFFFFF` | `#F2F2F2` |
| Soft primary text | `#DCD2E6` | `#EAEAEA`, `#D0D0D0`, `#CCCCCC` |
| Muted text | `#888888` | `#7B6E6E`, `#666666`, `#575B5F` |
| Default border | `#525252` | `#505050`, `#464646`, `#404040` |
| Strong border | `#5E5E5E` | `#7E7E7E`, `#666380` |
| Selection fill | `#4D3C64` | `#523E6E`, `#074051` |
| Warning text | `#E6C200` | `#FFEB3B` |
| Error text | `#F44336` | `#E81123`, `#DA4453` |

## Purple System

Purple is the visual anchor. Use a small semantic range rather than inventing a new purple for
each component.

| Token | Use | Value |
| --- | --- | --- |
| `accentStrong` | Active navigation, selected rows, primary brand fill | `#523E6E` |
| `accentDark` | Selection background, pressed state, dark controls | `#4D3C64` |
| `accent` | Buttons, sliders, links, icons, active controls | `#765E98` |
| `accentLight` | Hover and pressed feedback, high-emphasis accent | `#9373C0` |
| `accentSoft` | Light text, icons, or user-selected theme base | `#AC98C7` |
| `accentBorder` | Selected or focused component border | `#8363B0` |

Interaction should usually move through neutral gray surfaces before becoming brighter purple:

- Hover surface: `#404040`
- Pressed surface: `#383838`
- Selected surface: `#505050` or `#4D3C64`
- Selected hover: `#585858`
- Focus border: `#5F5585`

## Text Hierarchy

Do not use one gray for every non-primary label. The projects consistently distinguish importance.

- Primary: `#FFFFFF`
- Secondary: `#EAEAEA` or `#DCD2E6`
- Tertiary: `#CCCCCC`
- Metadata and supporting labels: `#B3B3B3` or `#BDBDBD`
- Muted: `#888888`
- Dim or disabled: `#666666`
- Links: `#F2F2F2`, `#D0D0D0`, and `#AFAFAF`, with lighter hover values

## Surfaces And Borders

Layer surfaces from dark to light so panels remain distinguishable without shadows:

1. Overlay: `#0A0A0A` or transparent black
2. Deep background: `#151515` to `#1B1B1B`
3. Main background: `#1E1E1E`
4. Panel: `#252A30` to `#2D2D2D`
5. Raised control or row: `#2C2C2C`
6. Hover or active neutral: `#404040` to `#585858`

Use borders sparingly:

- Subtle separation: `#3F3F3F` or `#404040`
- Standard component border: `#505050` or `#525252`
- Strong field or grid border: `#5E5E5E` to `#7E7E7E`
- Hide borders entirely where spacing and surface contrast already separate elements.

## Project-Specific Palettes

### `pwsh72exe`

WPF GUI (`src/pwsh72exe/`). Apply colors and control styling via `App.xaml` / `MainWindow.xaml` resource dictionaries, not Avalonia themes.

### `musicApp`

- Brand and sidebar: `#523E6E`
- Background: `#1E1E1E`
- Panel: `#2D2D2D`
- Primary accent: `#705399`
- Accent hover: `#856CAA`
- Selected segment border: `#8363B0`
- Danger: `#E81123`
- Text ranges: `#FFFFFF`, `#EAEAEA`, `#CCCCCC`, `#888888`, `#666666`

This is the most granular text hierarchy and the clearest example of resource-based shared color
tokens.

### `ghostwriterpp`

Classic dark editor:

- Background: `#151719`
- Foreground: `#BDC3C7`
- Selection: `#074051`
- Cursor, links, and images: `#3DAEE9`
- Markup and dividers: `#575B5F`
- Error: `#DA4453`

Plainstraction dark editor:

- Background: `#1A1A1A`
- Foreground: `#D0D0D0`
- Selection and markup: `#074051`
- Cursor, links, and images: `#B31771`
- Headings and emphasis: `#009BC8`

These cyan and pink accents are valid alternatives when a project needs an editor-focused identity,
but purple remains the broader house default.

### `RNGesus`

- Main background: `#121417`
- Card background: `#252A30`
- Wager background: `#1A1720`
- Primary purple: `#7D64A0`
- Icon purple: `#9C89B0`
- Theme base: `#AC98C7`
- Light text: `#DCD2E6`
- Dark purple border: `#5E4B79`
- Legacy purple values: `#8B74AC`, `#C5B9D3`

The user-selectable theme base can derive nearby shades, but keep the resulting scheme dark and
preserve readable text contrast.

### `badussyBoard`

- Window and group background: `#1B1B1B`
- Data grid header: `#181818`
- Data grid: `#282828`
- Data grid rows: `#2C2C2C`
- Primary accent: `#765E98`
- Hover and pressed accent: `#9373C0`
- Selection: `#4D3C64`
- Picker and separator fill: `#525252`
- Grid border: `#5E5E5E`
- Warning: `#E6C200`

This is the clearest flat desktop treatment: square corners, transparent toolbar buttons, minimal
focus decoration, and compact Fluent controls.

## Semantic Colors

Use semantic colors only where the meaning requires them. Keep them secondary to the main accent.

- Info: `#03A9F4`
- Success: `#4CAF50`
- Warning: `#E6C200` or `#FFEB3B`
- Error: `#F44336` or `#DA4453`
- Destructive action: `#E81123` or `#E53935`

## Implementation Rules

- Define shared colors as named theme tokens or resources instead of scattering literals through
  components.
- Keep component states semantic: `accent`, `accentHover`, `surfaceHover`, `selection`, and
  `textMuted` are better than names tied to one screen.
- Start with the dark neutral surfaces and add purple only to communicate action, selection, focus,
  or identity.
- Keep icons white or purple unless the icon represents an external brand.
- Use `#FFFFFF` artwork on dark purple branding where high contrast is intended.
- Test text and interactive states against their actual surfaces, not against the page background.
- Preserve the platform's readable default font for interface text unless the project has a reason to
  change it. Use monospace selectively for technical content.

## Avoid

- Pure black backgrounds everywhere.
- A different accent color for every control.
- Bright gradients, excessive shadows, or decorative glass effects.
- Large rounded cards in otherwise flat desktop interfaces.
- Framework-default ripples, redundant focus decoration, grid lines, or padding when they visibly
  conflict with the surrounding design. Never remove the only visible keyboard-focus indication.
- Low-contrast gray text used for important content or state changes.

## Source Projects

- [musicApp](https://github.com/fosterbarnes/musicApp): [`musicApp/Theme/Colors.xaml`](https://github.com/fosterbarnes/musicApp/blob/main/musicApp/Theme/Colors.xaml)
- [ghostwriterpp](https://github.com/fosterbarnes/ghostwriterpp): [`src/theme/themerepository.cpp`](https://github.com/fosterbarnes/ghostwriterpp/blob/main/src/theme/themerepository.cpp), [`src/editor/colorscheme.h`](https://github.com/fosterbarnes/ghostwriterpp/blob/main/src/editor/colorscheme.h)
- [RNGesus](https://github.com/fosterbarnes/RNGesus): [`Colors.kt`](https://github.com/fosterbarnes/RNGesus/blob/main/app/src/main/java/com/fosterbarnes/rngesus/Colors.kt)
- [badussyBoard](https://github.com/fosterbarnes/badussyBoard): [`Toolbar.axaml`](https://github.com/fosterbarnes/badussyBoard/blob/main/badussyBoard/Themes/Toolbar.axaml), [`SoundDataGrid.axaml`](https://github.com/fosterbarnes/badussyBoard/blob/main/badussyBoard/Themes/SoundDataGrid.axaml)
- [brownNote](https://github.com/fosterbarnes/brownNote): [`Colors.xaml`](https://github.com/fosterbarnes/brownNote/blob/main/src/brownNote/Theme/Colors.xaml), [`Controls.xaml`](https://github.com/fosterbarnes/brownNote/blob/main/src/brownNote/Theme/Controls.xaml) (source of the `base` sample theme)
