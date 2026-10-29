# Eyes Speedrun Toolkit
![Unity](https://img.shields.io/badge/Unity-2021.3.25f1-blue)
![Target](https://img.shields.io/badge/Loader-MelonLoader-green)
![Game](https://img.shields.io/badge/Steam_AppID-2633640-black)

A MelonLoader modification for Eyes: The Horror Game developed as a utility for speedrunners and practice runners. The project provides a unified interface for managing visual settings, performance parameters, and practice-oriented telemetry. It is built on Unity 2021.3.25f1 and targets the Steam build of the game (AppID 2633640).

The toolkit is intentionally divided into two functional domains: Quality of Life adjustments, which are generally permissible on official leaderboards, and practice-oriented features that modify gameplay perception and are therefore restricted to offline use. This separation is enforced at the interface level through distinct menu tabs and explicit warnings.

## Table of Contents

- [Architecture](#architecture)
- [Changelog](#changelog)
- [Installation](#installation)
- [Usage](#usage)
- [Implemented Features](#implemented-features)
  - [Quality of Life](#quality-of-life)
  - [Practice and Restricted Features](#practice-and-restricted-features)
- [Planned but Not Implemented](#planned-but-not-implemented)
  - [Monster Behavior Analysis](#monster-behavior-analysis)
  - [Seed Generator](#seed-generator)
- [Configuration](#configuration)
- [Contributing](#contributing)

## Architecture

The modification follows a modular architecture where each subsystem operates independently and communicates through a shared configuration layer. The entry point is `Main.cs`, which inherits from `MelonMod` and delegates lifecycle events to the corresponding managers.

```text
┌─────────────────────────────────────────────────────────────┐
│                           Main.cs                           │
│                                                             │
│   OnApplicationStart ──► InitializeModules()                │
│   OnUpdate           ──► HUDManager.Update()                │
│                      ──► MonsterTracker.Update()            │
│                      ──► GraphicsModule.Update()            │
│   OnGUI              ──► HUDManager.OnGUI()                 │
│   OnSceneWasLoaded   ──► Context-aware module reset         │
└──────────────────────────────┬──────────────────────────────┘
                               │
          ┌────────────────────┼────────────────────┐
          │                    │                    │
          ▼                    ▼                    ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│  ConfigManager   │ │  GraphicsModule  │ │  MonsterTracker  │
│                  │ │                  │ │                  │
│ • Load/Save      │ │ • FOV override   │ │ • Entity search  │
│ • PlayerPrefs    │ │ • FarClipPlane   │ │ • Distance calc  │
│ • Defaults       │ │ • PostProcess    │ │ • Path history   │
│ • Reset          │ │ • VSync / FPS    │ │ • Position log   │
└────────┬─────────┘ └────────┬─────────┘ └────────┬─────────┘
         │                    │                    │
         └────────────────────┼────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │    HUDManager    │
                    │                  │
                    │ ┌────┬───────┬────┐
                    │ │QoL │Cheats │Sett│
                    │ └────┴───────┴────┘
                    │                  │
                    │ • Timer (draggable)│
                    │ • Monster overlay │
                    │ • Settings panel  │
                    └──────────────────┘
```

Data flows through the configuration layer. When a user modifies a value in the HUD, the corresponding `ConfigManager` field is updated, a change flag is set, and on the next update cycle `GraphicsModule.Reset()` is invoked to reapply all graphical parameters. This ensures that settings persist across scene transitions while avoiding redundant per-frame operations.

```text
User Input (F1 / UI interaction)
         │
         ▼
HUDManager.Update() / Draw*Tab()
         │
         ├── Toggle _menuOpen
         ├── Cursor.lockState / Cursor.visible
         └── CheckAndApplySettings()
                    │
                    ▼
          GraphicsModule.Reset()
                    │
      ┌─────────────┼─────────────┐
      ▼             ▼             ▼
ApplyVSync    ApplySettings TogglePostProcessing
      │             │             │
      │      ┌──────┴──────┐      │
      │      ▼             ▼      ▼
      │     FOV     FarClipPlane  PostProcessLayer.enabled
      │                           PostProcessVolume.enabled
      ▼
QualitySettings.vSyncCount
Application.targetFrameRate
```

[↑ Back to top](#table-of-contents)

## Changelog

This project adheres to [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.0.0] - 2026-10-30

### Added
- **UITheme System (`UITheme.cs`)** — Centralized theming layer for the entire IMGUI interface. Provides a unified palette (background, accent, text, hover states), cached textures and styles to eliminate per-frame `Texture2D`/`GUIStyle` allocations, automatic contrast-aware text color on accent plates, and drawing helpers (`Solid`, `Line`, `HexToColor`/`ColorToHex`). All HUD elements and the settings window now render through this system.
- **Custom Window Title Bar** — The settings window no longer uses the built-in Unity IMGUI titlebar. A custom 28px header strip is drawn instead: mod name in accent color, highlighted background with a bottom separator line and a `[ drag here ]` hint, plus a manually rendered `X` close button with hover feedback. The title turns white while dragging.
- **Manual Window Dragging** — Window movement is now implemented by hand (MouseDown detection inside the title strip + `Input.mousePosition` tracking in `Draw()`), instead of relying on `GUI.DragWindow`. This makes dragging reliable regardless of GUILayout content overlapping the drag zone. The window can no longer be dragged fully off-screen — the title bar always stays reachable. Position persists to config on release.
- **Styled Section Headers** — Menu sections (Visuals, Quality of Life, FOV, Display, Monster Tracker, etc.) now use `SectionHeader()`: a 3px accent vertical bar aligned with the text, extra breathing room above and below (Space 10 / 22px row / Space 14), so labels are no longer clipped.
- **Flat Tab Navigation** — Replaced the gray `SelectionGrid` with flat tab buttons; the active tab is highlighted with the accent color, inactive tabs get a subtle hover state.
- **Accent-Highlighted Checkboxes** — Toggles in all tabs tint with the accent color when enabled, making active settings instantly scannable.
- **Redesigned Speedrun Timer Plate** — Semi-transparent dark plate with an accent stripe on the left, large bold monospace digits (OS-font fallback via `Font.CreateDynamicFontFromOSFont`), green digits on `FINISHED`, and frame highlight while being dragged.
- **Redesigned Cheats Watermark Panel** — Dark red plate with a red accent stripe replacing bare text. Height is computed from the actual rendered text (`CalcSize().y`) instead of a hardcoded value, and the panel sits higher from the screen edge, so multi-line lists of active cheats are never vertically clipped. Auto-expansion when new cheats are toggled is preserved.
- **Stamina Timer Plate** — Compact plate sized to the text width with a top accent line.
- **Monster Tracker Card** — Dark card with an accent stripe in a compact `DIST | DIR` format.

### Changed
- **Overall Visual Style** — Complete redesign of the HUD and menu away from default gray IMGUI visuals toward a flat, modern dark theme (dark panels, accent highlights, consistent spacing).
- **2D ESP Box** — Now a semi-transparent red square with a crisp border instead of a solid filled rectangle.
- **All displayed text is pure ASCII** — Special characters and emoji (`⚠`, `↺`, `✕`, `·`, `◢`) were replaced with ASCII analogs (`[!]`, `Reset to Defaults`, `X`, `|`, `//`), fixing black-square glyphs in the game's font (Aileron lacks these codepoints).
- **Settings window content layout** — Tabs and content are wrapped in a `GUILayout.BeginArea` that starts strictly below the title strip, so nothing overlaps the drag zone anymore.

### Removed
- **Cheat Hotkeys** — All keyboard bindings for activating cheats (Space/RightArrow "activate all", and per-cheat keys) were removed. Cheats are now toggled exclusively through the GUI; the watermark reacts to the state of any active cheat (ESP, Bags Wallhack, Infinite Stamina, Freecam, No Fog, etc.).
- **Built-in IMGUI Titlebar & Resize Handle Glyph** — Replaced by the custom title strip and a clean ASCII resize grip.

### Fixed
- **`Font.GetDefault()` Compilation Error** — The method does not exist in the target Unity version. Replaced with a safe monospace OS-font helper that falls back gracefully when no font is available.
- **Black Squares Before Section Titles** — Caused by `GUILayout.Box("")` rendering a placeholder texture when drawing accent stripes. Section headers now use `UITheme.Solid()` directly.
- **Watermark Text Clipping** — The bottom half of the second line ("list of active cheats") was cut off due to a hardcoded panel height; height is now dynamic.
- **Window Not Draggable** — `GUI.DragWindow` zones were intercepted by GUILayout content and background boxes consuming MouseDown events. Resolved by manual drag logic and `GUI.DrawTexture` for non-interactive backgrounds.
- **Style Allocation Leaks** — Title/hint/close-button styles are cached in static fields instead of being constructed every frame.

### Technical Notes
- Drag detection works in window-local coordinates: `EventType.MouseDown` inside the callback checks the 28px header strip (excluding the close button), stores the cursor offset, then `Draw()` moves `WindowRect` from `Input.mousePosition` each frame until `MouseUp`. This path is independent of the Harmony `BlockClickPatch`, which only affects the uGUI `GraphicRaycaster` and cannot interfere with IMGUI.
- `UITheme` caches all generated textures once and rebuilds them only on palette changes (`ForceRebuild()`), keeping OnGUI overhead constant.
- `EyesSpeedrunToolkit.csproj` updated with `<Compile Include="UITheme.cs" />` (the project uses explicit compile items).

[↑ Back to top](#table-of-contents)

## Installation

The modification requires a working MelonLoader installation. MelonLoader must be installed into the root directory of Eyes: The Horror Game prior to using this toolkit. Once MelonLoader is operational, place the compiled `EyesSpeedrunToolkit.dll` file into the `Mods` subdirectory. On the first launch, the mod will create its configuration entries in the Unity PlayerPrefs registry under the `EST_` prefix. No additional configuration files are required.

## Usage

The interface is accessed by pressing the assigned hotkey, which defaults to and is currently hardcoded as `F1` (changing the key binding is not supported at this time). When the menu is opened, the cursor lock state is released and the cursor is made visible; when the menu is closed, the game's native cursor management is restored without interference from the mod.

The Speedrun Timer, when enabled, appears in the upper-left corner of the screen by default. It can be repositioned by clicking and dragging with the left mouse button while the game is paused (or while the cursor is visible), even if the menu itself is closed. The position is automatically saved to configuration upon release. The timer automatically starts when a run begins and displays the final time upon completion.

## Implemented Features

### Quality of Life

The following features are designed to improve visual clarity and performance stability without altering gameplay mechanics. They are generally considered acceptable for use on official leaderboards, though runners should verify current rules with the relevant moderation team.

* **Custom Speedrun Timer:** A lightweight on-screen timer rendered via Unity's IMGUI system. It tracks elapsed time from the moment the run begins and preserves the final time upon completion. The timer is fully draggable and its position persists across sessions.
* **Post-Processing Toggle:** Disables the `PostProcessLayer` component on the player camera and all `PostProcessVolume` instances in the active scene. This removes vignette, bloom, and color grading effects. The change is reversible: toggling the option off restores the original post-processing state.
* **V-Sync and Frame Rate Limiter:** Provides three modes of frame rate control: synchronization with the monitor's refresh rate via `QualitySettings.vSyncCount`, a custom frame rate cap via `Application.targetFrameRate`, or an unlimited mode. The limiter is applied every frame in `GraphicsModule.Update()`, which ensures it remains effective across all scenes including the main menu and loading screens. This feature was added primarily to address coil whine caused by unstable frame pacing.
* **Field of View Adjustment:** Overrides the camera's `fieldOfView` property each frame, preventing the game from resetting the value during sprint animations. The slider ranges from 60 to 120 degrees.

[↑ Back to top](#table-of-contents)

### Practice and Restricted Features

The following features modify gameplay perception and are restricted to offline practice. Their use invalidates a run for leaderboard submission.

* **Monster Tracker:** Locates the active monster entity in the scene by searching for `GameObjects` with the `Enemy_` prefix (such as `Enemy_Krasue`, `Enemy_Charlie`, `Enemy_Dog`, `Enemy_Witch`). Once located, it continuously computes the Euclidean distance between the monster's transform and the player's position. The distance is displayed in the lower-right corner of the screen during gameplay and in the Cheats tab of the menu. The display turns red when the monster is within 15 meters. The tracker also maintains a rolling history of the last 100 recorded positions.

## Planned but Not Implemented

Two features were considered during development but were ultimately excluded from the current release.

### Monster Behavior Analysis

A system for logging and visualizing monster pathing decisions, state transitions, and vision cone activity was considered. However, predicting monster behavior is fundamentally impossible due to non-deterministic randomness. There is no hard-coded control or predictable logic to map out, making such a tool practically useless.

### Seed Generator

The idea for a seed generator was borrowed from community member T300850. While it could technically be added (especially given the existence of the Custom Spawns category), the author refuses to implement it on principle. The vanilla RNG is entirely non-deterministic, and there is no practical sense in creating such a tool.

Furthermore, the Eyes speedrunning community and Speedrun.com moderation explicitly prohibit tools that automate or guarantee specific random outcomes. Providing a seed generator would violate the foundational rules of the leaderboard and compromise the integrity of submitted runs.

Seed evaluation and the decision to reroll constitute a core skill of the speedrun. The ability to recognize a favorable layout and commit to it, or to abandon an unfavorable one, is an integral part of run execution. Automating this decision removes the human element of risk assessment and route adaptation, which are essential components of a legitimate run.

From a technical perspective, implementing a reliable seed generator for a Unity 2021 game requires reverse-engineering the internal state of the game's pseudo-random number generator. Unity's RNG is typically coupled to internal frame counts, memory addresses, and physics simulation steps. A generator that reads or writes this state would be highly fragile and likely to break with every game update, creating an unsustainable maintenance cycle.

This toolkit therefore focuses exclusively on execution quality, routing practice, and visual adjustments, leaving all random number generation entirely under the player's control.

[↑ Back to top](#table-of-contents)

## Configuration

All settings are persisted through Unity's `PlayerPrefs` system under keys prefixed with `EST_`. The configuration is saved automatically when the "Save Settings" button is pressed in the menu, when the timer position is changed via dragging, or when a practice feature is toggled. To perform a complete reset to default values, use the "Reset to Defaults" button in the Settings tab, or manually clear the `EST_*` keys from the Unity registry.

The default configuration is as follows:

| Parameter | Default Value | Description |
| :--- | :--- | :--- |
| `MenuKey` | `F1` | Hotkey for opening the menu (hardcoded) |
| `Freecam` | `F3` | Hotkey for Free Camera (fly) |
| `DisablePostProcessing` | `false` | Removes vignette, bloom, color grading |
| `DisableVSync` | `false` | Enables V-Sync with monitor refresh rate |
| `CustomFPS` | `0` | `0` = unlimited, `>0` = custom frame rate cap |
| `TargetFOV` | `60` | Camera field of view in degrees |
| `TimerEnabled` | `true` | Displays the speedrun timer |
| `MonsterTrackerEnabled` | `false` | Displays monster distance overlay, and where he is |
| `Infinite Stamina` | `false` | Infinite stamina! |
| `Stamina Regen Timer` | `false` | Displays regeneration of the stamina, and after how long will the bar be 100% |
| `HarmonyClickBlock` | `Left Click Mouse` | Doesn't click on the game when you're in pause |
| `Bags Wallhack` | `false` | Shows bags 2D ESP |
| `2DBox` | `false` | Displays ESP of Monster. Shows where is he (Same with 3D WallHack |
| `TimerX`, `TimerY` | `10`, `10` | Timer position in pixels from top-left |

## Building from Source

- **IDE:** Visual Studio
- **Target Framework:** .NET Framework 4.7.2
- **References:** `MelonLoader.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.IMGUIModule.dll` From the directory of game.

## Contributing

The project is **fully open-source**, and the author welcomes anyone to use, modify, or distribute the code in whatever way they see fit. Contributions are accepted through the standard GitHub workflow. Bug reports, feature requests, and pull requests should be submitted through the repository's issue tracker. When submitting a pull request, please ensure that the code compiles against the current MelonLoader release and that any new features include appropriate configuration persistence.

## Acknowledgements

This project wouldn't be possible without the help and feedback from the community:

* **Matrix** — Bug hunter and tester. Thanks for finding and reporting critical issues like portal visibility, particle system conflicts, and shader replacement bugs. Your detailed feedback helped make the mod more stable and reliable.
* **T300830** — Creator of the original Assembly-CSharp modification for Eyes. His work inspired some features of this toolkit, and his feedback on MelonLoader compatibility helped shape the project's direction.
* **Speedrun.com Eyes Community** — For providing feedback, feature requests, and testing early versions of the mod.

If you find any bugs or have suggestions, feel free to open an issue on GitHub!
_You can also reach out to me via private message — whichever way is most convenient for you:_
* Discord: w3ntr
* Telegram: @w3ntr

[↑ Back to top](#table-of-contents)
