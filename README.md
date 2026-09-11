# Pixel Unicorn Runner

A single-scene, keyboard-controlled endless runner for Unity **6000.6.0f1**. Open `Assets/Scenes/MainGameplay.unity` and press Play. The scene is also the only enabled build scene.

| Action | Keys |
| --- | --- |
| Jump / double jump | Space or Z |
| Shorter jump | Release the jump key early |
| Dash through a runestone | Left/Right Shift or X |
| Retry after a fall or collision | R |

Hold jump to clear larger gaps. Gold relics award 25 points; dashed runestones award 100; distance awards 10 points per world unit. One collision or fall ends the run. There are no menus, sounds, progression, or saved scores.

## Scene and tuning

- `Assets/Runner/RunnerSettings.asset` contains movement, dash, difficulty, generation distance, pool capacity, and rebase settings.
- `RunnerMotor` holds the movement rules; `RunnerPlayer` applies them through Rigidbody2D at 60 Hz. The player uses a private instance of the `Runner` input action map.
- `RunnerRun` owns score, distance, restart, and world rebasing. Score and run-state events drive the HUD.
- `SectionPlanner` creates seeded platform transitions with conservative single-jump bounds; the second jump is available for recovery. `RunnerWorld` recycles ten copies of `RuinsSection.prefab`.
- The camera uses URP Pixel Perfect at **320 × 180 / 16 PPU**, with integer windowboxing. Distant arches scroll at 15% and nearer ruins at 40% of camera travel.
- The saved opening platform is an Editor preview. `RunnerPreview` disables it before runtime pooling starts.
- The static 5×7 TMP font and generated sprites use point sampling. The HUD scales to the same centered integer viewport as the game.

Art provenance and generation instructions are in `Assets/Runner/Art/ART.md`.

## Rebuilding authored assets

The checked-in scene, prefab, art, settings, and font work without running a generator. The Editor builder is retained for reproducibility. **It replaces the generated main scene and platform prefab**, so preserve manual customizations before invoking it.

With this project open in Unity and Play mode stopped:

```powershell
unity command runner_build_scene --project-path <project-path>
```

Scene/prefab/import metadata are authored through Unity Editor APIs. No hand-edited scene YAML or runtime texture generation is needed.

## Tests

Use Unity Test Runner for `Runner.EditModeTests` and `Runner.PlayModeTests`. With the Editor closed, the equivalent batch commands are:

```powershell
unity test <project-path> --mode EditMode --filter RunnerRulesTests --output Logs/editmode.xml
unity test <project-path> --mode PlayMode --filter RunnerIntegrationTests --output Logs/playmode.xml
```

The suite checks jump limits and timing, variable height, dash timing, scoring, collision and fall deaths, restart, generation margins across 24,576 transitions, and three ten-minute physics simulations with input sampled at 30/60/120 FPS. The long runs exercise the speed cap, pooling, and repeated rebasing; they are deterministic accelerated simulations, not GPU performance benchmarks.

To capture the current scene plus its overlay HUD without changing the saved scene:

```powershell
unity command runner_capture --width 1280 --height 720 --output Logs/gameplay.png --project-path <project-path>
```

The capture helper renders world and UI separately and composites them because camera-only screenshots omit Screen Space Overlay canvases. Captures and test reports are excluded from source control under `Logs/`.

## Verified result

Validated on 2026-09-11 with Unity 6000.6.0f1:

- 17 EditMode tests and 10 PlayMode tests passed, including synthetic keyboard input and the three ten-minute physics simulations.
- Inspected captures at 320×180, 1280×720, 1920×1080, and 1366×768. The non-integer display uses a centered 1280×720 viewport with black borders.
- The Windows x64 build succeeded. Its headless startup smoke test and the final Editor gameplay run reported no runtime exceptions. Headless startup is not a graphics performance test.
- Local executable: `Build/PixelUnicornRunner.exe`. Keep the adjacent data folder and runtime DLLs with it.
- Final gameplay preview: `Logs/gameplay-final.png`; test results: `Logs/editmode-results.json` and `Logs/playmode-results.json`.

For another Windows build, stop Play mode and invoke `runner_build_player` through Unity CLI. Submit long builds as detached jobs to avoid the CLI's synchronous command timeout.
