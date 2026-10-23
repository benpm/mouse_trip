# Unity workflow for this project

Determined 2026-10-18 with `uagent doctor`, `unity pipeline list`, `unity editors --installed`;
tooling gaps fixed 2026-10-22.

## Current state

| Item | State |
|---|---|
| Project version | `6000.6.0f1` (installed at `C:\Program Files\Unity\Hub\Editor\6000.6.0f1`) |
| Other installed editors | 6000.5.1f1, 6000.7.0a5, 2021.3.45f1 |
| `com.unity.pipeline` | `0.9.0-exp.1` in manifest; `unity command`/`recompile`/`eval` work against any editor on this project |
| `com.unity.test-framework` | `1.4.6` in manifest; asmdefs `MouseTrip`, `MouseTrip.Tests.EditMode`, `MouseTrip.Tests.PlayMode` |
| Agent tools (`uagent dump/apply/validate`) | `Packages/com.claudeagent.tools` (gitignored; rerun `uagent install-tools` on a fresh clone) |
| Build scene list | `Assets/Scenes/level_pieces.unity` only |
| Non-standard editor path | set `UNITY_EDITOR=<path to Unity.exe>` if `uagent` cannot find the version |

If the user has the project open in the GUI editor, `Temp/UnityLockfile` exists and `uagent editor start`
fails. Since the pipeline package is installed, `unity command --project-path .` reaches their editor
once they focus the window after a refresh.

## Decision tree per session

```
uagent doctor
├─ resident_editor: <ours>        → use it: unity command / recompile / uagent apply|dump
├─ lockfile_present, no resident  → GUI editor open (check Library/EditorInstance.json pid)
│     └─ unity command --project-path . ...   (pipeline pkg is installed; user may need to focus the window)
└─ no lock                        → uagent editor start --nographics   (first boot after package changes: minutes)
                                    ... work ...
                                    uagent editor stop before unity test / unity build / uagent batch
```

One Unity process per project. A stale `Temp/UnityLockfile` after a crash can be deleted only
when no `Unity.exe` has the project open (`Get-Process Unity`).

## Loops

Compile: edit `.cs` → `unity recompile --project-path .` → `uagent log Logs/uagent-editor.log` → fix → repeat.
New files are not seen until an asset refresh; if `recompile` reports `up_to_date` suspiciously fast, run
`unity command eval ... 'UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);'`
then `recompile` again. A `400 Bad Request` from `recompile` during a domain reload is transient: retry.

Scene edit: write `ops.json` (schema in unity-headless `references/scenes-prefabs.md`) →
`uagent apply ops.json` → `uagent dump Assets/Scenes/level_pieces.unity --props` to confirm →
`uagent refs Assets/Scenes/level_pieces.unity`. Or `unity command eval` with `EditorSceneManager.OpenScene`
+ `SerializedObject` + `SaveScene` (how `tileDamageProfile` was wired).

Tests (one-shot, resident editor stopped):
```bash
unity test . --mode EditMode --output Logs/tests-edit.xml --timeout 900 --no-banner
unity test . --mode PlayMode --output Logs/tests-play.xml --timeout 900 --no-banner
uv run -q --project "C:/Users/ben/.claude/skills/unity-headless/scripts" uagent tests Logs/tests-play.xml
unity test . --mode PlayMode --test-filter 'TileDamageTests.DamagingTile_ReducesHealth_AndKnocksBack' --output Logs/t.xml
```
Exit 8 = failing tests (read `uagent tests`), other non-zero = run error (retryable). `--format json`
on `unity test` produced an empty stdout on beta.13; parse the XML instead.

Play mode verification: `unity command editor_play`, confirm frames advance with
`eval 'return UnityEngine.Time.frameCount;'` twice, screenshot via eval, read console; see
`unity:unity-cli` → `references/playmode-verification-loop.md`.

## Safe Mode

If `unity command` exits 7 or times out: `unity pipeline list --format json` →
`data.instances[].safeMode.detected`. Compile errors prevent the pipeline package from loading.
Read `uagent log`, fix C#, `uagent editor stop` then `start`.

## Flags

- Every `unity command`: `--caller plugin --skill <skill>` (analytics label only).
- `unity command` parses `--timeout` (s), `--project-path`, `--result-only`, `--detach` anywhere;
  same-named editor parameters go after `--`.
- `unity run|test|build` own `-batchmode -quit -projectPath` (and test flags); passing them after
  `--` fails with exit 6.
- Always `--format json` when parsing output; read failures from stdout `success:false`.
- `uagent log` "License problem" hint is a false positive from `[Licensing::...] Successfully connected` lines.

## Known gaps (as of 2026-10-22)

- `fetchGravity` sets `Physics2D.gravity` from one `PlayerController` every `Update`; clones (planned
  mechanic) need a different design.
- No project layers defined; `groundLayer`, `targetLayer`, `playerLayer` masks are all Default.
- Knockback is set as velocity and immediately fought by `ApplyHorizontal` deceleration (100 u/s²);
  tune or add a brief input lockout if the feel is wrong.
- `PlayerHealth.Die` reloads by build index; works only for scenes in the build list.
