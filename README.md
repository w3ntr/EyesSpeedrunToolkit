# Eyes Speedrun Toolkit
![Unity](https://img.shields.io/badge/Unity-2021.3.25f1-blue)
![Target](https://img.shields.io/badge/Loader-MelonLoader-green)
![Game](https://img.shields.io/badge/Steam_AppID-2633640-black)

A MelonLoader modification for Eyes: The Horror Game developed as a utility for speedrunners and practice runners. The project provides a unified interface for managing visual settings, performance parameters, and practice-oriented telemetry. It is built on Unity 2021.3.25f1 and targets the Steam build of the game (AppID 2633640).

The toolkit is intentionally divided into two functional domains: Quality of Life adjustments, which are generally permissible on official leaderboards, and practice-oriented features that modify gameplay perception and are therefore restricted to offline use. This separation is enforced at the interface level through distinct menu tabs and explicit warnings.

## Table of Contents

- [Architecture](#architecture)
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

* **Far Clip Plane Adjustment:** Modifies the camera's `farClipPlane` property. The vanilla value is restricted to 11, which causes distant objects to be culled from rendering. The slider uses a normalized 0.01–1.00 range that maps linearly up to a hard cap of 1000 (beyond which rendering breaks due to shader limitations). This provides fine-grained control, allowing runners to observe bag and door placements from a distance during route practice.
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
| `DisablePostProcessing` | `false` | Removes vignette, bloom, color grading |
| `DisableVSync` | `false` | Enables V-Sync with monitor refresh rate |
| `CustomFPS` | `0` | `0` = unlimited, `>0` = custom frame rate cap |
| `TargetFOV` | `60` | Camera field of view in degrees |
| `TimerEnabled` | `true` | Displays the speedrun timer |
| `MonsterTrackerEnabled` | `false` | Displays monster distance overlay |
| `FarClipPlane` | `0.01` | Normalized value (`0.01` = 11m, `1.00` = 1000m) |
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
