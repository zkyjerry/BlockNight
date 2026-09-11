# Repository Guidelines

Block Night is a Unity 2022.3.62f3c1 URP 2D combat demo. World objects live in the saved scenes so they can be hand-tuned. HUD and menus use Canvas; the playfield does not.

## Project Structure & Module Organization

- `Assets/BlockNight/Scripts/` — runtime (`GameDirector`, `CombatModel`, `ArenaPresentation`, `GameHUD`, `SceneFlow`)
- `Assets/BlockNight/Editor/` — QA menus, migrations, scene setup
- `Assets/BlockNight/Scenes/` — `MainMenu.unity`, `BlockNight.unity`, `GameOver.unity`
- `Assets/BlockNight/Data/` — ScriptableObjects (`Balance`, `SpawnSchedule`, `Enemies/`, `FeedbackSettings`)
- `Assets/BlockNight/Art/`, `Shaders/`, `Fonts/`, `Music/` — presentation assets
- `Docs/` — design, architecture, QA logs; `Builds/` — local exports

Tune numbers on SOs and scene objects. See `Docs/使用与微调.md` and `Docs/Architecture.md`.

## Build, Test, and Development Commands

Open Unity and Play from `Assets/BlockNight/Scenes/MainMenu.unity` (Build Settings order: MainMenu → BlockNight → GameOver).

| Command | Purpose |
|---|---|
| `Block Night/QA/Run Grid v4 Checks` | Edit-mode combat rules |
| `Block Night/QA/Run Runtime Checks` | Play-mode flow (enter combat first) |
| `Block Night/QA/Run v6 Feedback Checks` | Camera, telegraphs, clock, skill flash |
| `Block Night/QA/Capture 1600x1000` | Screenshot into `Docs/QA/` |

Historical `Apply v*` / `TMP` menus are one-shot migrations. Do not re-run them on current scenes.

## Coding Style & Naming Conventions

C# namespace `BlockNight`; types PascalCase; Inspector-wired fields stay public. Use 4-space indent; keep existing compact Editor-check style. All text is TMP (`TMP_Text` / `TextMeshProUGUI`); never add `UnityEngine.UI.Text`. Name scene objects descriptively (`BOARD — editable world sprites`, `DASH SHIELD — editable circle`).

## Testing Guidelines

Daily regression is the `Block Night/QA/*` menus, not Test Runner. New checks go in `Assets/BlockNight/Editor/*Checks.cs`, fail by throwing, and write `Docs/QA/*.txt`. Play-mode checks mutate the current run — restart Play afterward.

## Commit & Pull Request Guidelines

History uses Chinese numbered lists of player-facing changes. Match that: why the feel or rule changed, not a file dump. PRs should note SO/scene edits, link design notes, and attach QA screenshots when visuals change. Do not commit `Library/`, `Temp/`, `Logs/`, `.csproj`, or `.sln`.

## Scene Construction (Required)

Place gameplay objects directly in the scene: board tiles, enemy and attack pools, Light2D, particles, shield ring. Runtime may enable, move, or reuse those pooled objects. Do not attach a bootstrap script that instantiates the whole world on Play — that blocks later hand-tuning.

Do not build the playfield from UI Canvas or RectTransforms. UI is HUD and menus only. World sprites plus URP 2D lights are required so lighting and Scene-view editing stay usable.
