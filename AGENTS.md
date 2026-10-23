# AGENTS.md

Guidance for coding agents working in this repository. Supporting references live in `.agents/`.

## Project

"Mouse Trip": 2D puzzle platformer with space-bending mechanics (gravity direction changes, surface
traversal). Unity 6 (`ProjectSettings/ProjectVersion.txt`), URP 2D, Tilemap, legacy `Input` API.
Design notes: `Origami Levels.md`, `demo_levels.md`. No CI yet.

## Interacting with Unity

Never ask the user to open the Editor GUI. Everything runs through the `unity` CLI (installed,
`unity --version`) and the `uagent` helper from the `unity-headless` skill:

```bash
uagent() { uv run -q --project "C:/Users/ben/.claude/skills/unity-headless/scripts" uagent "$@"; }
```

Full workflow, decision tree and gaps: `.agents/unity-workflow.md`. Short form:

1. `uagent doctor` first, every session. Read `lockfile_present` / `resident_editor`.
2. Lockfile present and no resident editor → the user's GUI Editor has the project open. Do not
   launch batch Unity (lock). Drive the open editor with `unity command ...` if
   `unity pipeline list --format json` shows `hasPipelinePackage: true`; otherwise ask them to
   close it. Probe with `unity command --project-path .`, not `unity status` (batch editors are
   invisible to it).
3. No editor running → `uagent editor start` (resident batchmode) for iterative work;
   `uagent editor stop` before any one-shot (`unity test`, `unity build`, `uagent batch`).
4. After any `.cs` edit: `unity recompile --project-path .` (resident) or
   `uagent batch -- -executeMethod ClaudeAgent.Batch.Ping`. Zero `error CS` before anything else.
   Compile errors put the editor in Safe Mode and the pipeline package stops loading (exit 7).
5. Scene/prefab structural edits (add/remove objects or components) go through
   `uagent apply ops.json` or `unity command eval`, never hand-written YAML. Hand-edit YAML only
   for scalar values, then `uagent refs <file>`.
6. Every asset has a `.meta`; `git mv` both, never copy a `.meta`.
7. Append `--caller plugin --skill unity-headless` to every `unity command` call.

Commands:

```bash
uagent inspect Assets/Scenes/level_pieces.unity   # hierarchy from YAML, no Unity needed
uagent refs                                        # GUID/structure validation of all YAML
uagent check-meta                                  # missing/orphan .meta, duplicate GUIDs
uagent log Logs/uagent-editor.log                  # parsed compile errors / exceptions
unity command eval --caller plugin --skill unity-headless 'return UnityEngine.Application.unityVersion;'
unity test . --mode EditMode --output Logs/tests.xml && uagent tests Logs/tests.xml
unity test . --mode EditMode --test-filter 'Namespace.Class.Method' --output Logs/tests.xml
unity build . --profile "<name>" --output-path Builds/MouseTrip.exe
```

Domain knowledge comes from the official `unity:*` skills (`unity:create-2d-physics`,
`unity:create-tile-palette-or-rule-tiles`, `unity:2d-pixel-perfect`, `unity:unity-cli`, ...);
execution and verification follow `unity-headless`. When they disagree, `unity-headless` wins.
For Unity 6.5+ C# (removed/renamed APIs) read the skill's `references/csharp-unity66.md` and
`uagent docs <Type.member> --fetch` instead of writing APIs from memory.

## Architecture

Game code is the `MouseTrip` assembly (`Assets/Scripts/MouseTrip.asmdef`, everything under
`Assets/Scripts/`). Tests: `Assets/Tests/EditMode` and `Assets/Tests/PlayMode` (`MouseTrip.Tests.*`
asmdefs, NUnit via `com.unity.test-framework`). One scene `Assets/Scenes/level_pieces.unity`
(`Main Camera`, `Grid/Tilemap Layer 1`, `Player/Sprite`), the only entry in the build scene list.

- **Gravity frame.** `PlayerController` owns `gravityDir` (public `Vector2`). All movement is
  computed in a rotating orthonormal basis `Up = -gravityDir`, `RightAxis = Perpendicular(gravityDir)`:
  world velocity is decomposed into (along, up), platforming logic runs on scalars, then recomposed.
  `fetchGravity` copies `gravityDir * 9.81` into `Physics2D.gravity` every frame so non-player
  bodies fall the same way. Any new movement code must work in this frame, not world axes.
- **Ground/wall detection** uses `Rigidbody2D.GetContacts` normals dotted against `Up`/`RightAxis`,
  not raycasts. Contacts are fetched once per `FixedUpdate` into a cached array; `CheckContacts`
  handles ground + tile effects, `CheckWall` runs after `ApplyHorizontal` to clamp into-wall
  velocity. The `groundLayer` mask field exists but is unused.
- **Tiles.** `GameplayTile : Tile` (`Assets/Scripts/Tiles/`) carries gameplay flags (`solid`,
  `damagesPlayer`, `damage`, `oneWay`, ...). `PlayerController.TryGetGameplayTile` reads the tile
  under a contact via `Tilemap.WorldToCell`; non-tilemap colliders and empty cells are skipped.
  Kenney tiles/RuleTiles/AnimatedTiles are sub-assets of single `.asset` files with deterministic
  GUIDs (see `Assets/Sprites/tileset/KenneyPlatformerTiles/README.txt`).
- **Damage.** `DamageProfile` (ScriptableObject, `Create > Game > Damage Profile`) →
  `DamageContact2D` (collider/trigger, layer-masked, per-target cooldown) → `IDamageable.TakeDamage(DamageInfo)`
  (returns `bool`: applied or blocked) → `PlayerHealth` (invincibility flash, `onHealthChanged`/`onDeath`
  UnityEvents, scene reload). Tile damage: `PlayerController.tileDamageProfile` (scene uses
  `SpikeDamageProfile`) supplies cooldown/knockback/invincibility, `GameplayTile.damage` supplies the
  amount, and `PlayerController` applies knockback in the gravity frame when `TakeDamage` returns true.
- **Mechanisms.** `Button2D` (trigger, counts occupants) → `IActivationReceiver.SetActivated` →
  `Door2D`. Targets are serialized as `MonoBehaviour[]` and type-checked at runtime.
- **Interaction.** `PlayerInteract` keeps a trigger-overlap list of `IInteractable` and fires the
  first on `E`. `LevelPortal2D` loads next scene by build index.
- Interfaces live in `Assets/Scripts/interface/`; cross-system coupling goes through them.

## Conventions

- Input: legacy `Input.GetButton("Jump")` / `GetAxisRaw("Horizontal")` (axes in
  `ProjectSettings/InputManager.asset`). `activeInputHandler` is "Both", Input System package is
  installed but unused.
- Serialized fields: `[SerializeField] private` with `[Header]` groups. Renaming one requires
  `[FormerlySerializedAs]`.
- Layers: only Unity defaults exist in `TagManager.asset`; scripts take `LayerMask` fields, so
  add project layers there before relying on masks.
- `Packages/com.claudeagent.tools/` is the gitignored agent helper (`uagent install-tools`); the
  pipeline and test-framework packages are committed in `manifest.json`.
- `UnityEditor` must not be referenced from runtime scripts without `#if UNITY_EDITOR`.
- `UnityEvent` fields get inline initializers (`= new()`); they are null on a runtime `AddComponent`.
- New logic gets an EditMode test; MonoBehaviour/physics behaviour gets a PlayMode test. Set private
  serialized fields in tests via `SerializedObject` under `#if UNITY_EDITOR`.
- Never write scratch `.cs` under `Assets/` or `Packages/`; Unity compiles everything there.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx` are generated and ignored.

## Done means verified

Report completion with evidence: clean compile, `uagent refs` with 0 errors on touched YAML,
test counts from `uagent tests`, and a `uagent dump` excerpt for scene/prefab changes. State
explicitly what could not be verified headlessly (visual look, feel).
