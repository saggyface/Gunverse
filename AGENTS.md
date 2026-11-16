# AGENTS.md

**When generating code, follow the clean code guidlines in the folder CodeStyle/**

Unity 6 project "Gun Verse" (Editor `6000.6.7f1`, URP): a first-person multiplayer game built with Netcode for GameObjects, Unity Services sessions/relay, and the new Input System.

## Driving the Editor

This project has the `com.unity.pipeline` package installed, so the `unity` CLI can control the running Editor live. **Prefer live commands over hand-editing files** for anything touching scenes, GameObjects, prefabs, or assets — the Editor applies them to the active scene and keeps its in-memory state in sync. Never hand-edit `.unity`/`.prefab`/`.asset` YAML while an Editor is reachable.

```powershell
unity status --project-path "C:\Unity\UnityProjects\Gun Verse"   # confirm state "ready"
unity command script eval '<C# code>' --project-path "C:\Unity\UnityProjects\Gun Verse"
```

- Always pass `--project-path "C:\Unity\UnityProjects\Gun Verse"` (path has a space).
- **PowerShell quoting gotcha:** inside `script eval` string literals, PowerShell 5.1 strips plain `"`. Escape them with backslash-quote: `GameObject.Find(\"Cube\")`, not `GameObject.Find("Cube")`.
- If `unity command` won't connect, the Editor may be in Safe Mode from a compile error — run `unity status` to check.

## Build / test / lint

- No test framework, asmdefs, CI, or lint step is configured. Verify changes by compiling in the Editor with `unity command script recompile` and checking `unity command console status`.
- Windows build (profile `Windows`): `unity command build run --profileName Windows --outputPath "C:\Unity\UnityProjects\Gun Verse\Builds\Gun Verse.exe" --confirm true --project-path "C:\Unity\UnityProjects\Gun Verse"`. Poll `unity command build status`.
- Solution file is `Gun Verse.slnx` (Unity 6 `.slnx` format); `Assembly-CSharp.csproj` are generated — do not edit.
- Gun Game verification: `Tools > Gun Verse > Build Gun Game Progression` applies the ladder tuning, then `Tools > Gun Verse > Validate Gun Game Ladder` prints the 20 entries. Rerun the progression build after any prefab or content rebuild.

## Testing workflow

- **The user performs all gameplay and multiplayer testing.** Do not enter Play Mode or launch the build to test gameplay unless explicitly asked.
- End every change with a short **Test this** checklist: what to run, the exact in-game actions, and the expected result. Include the client log path and what to look for (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\Gun Verse\Player.log`) when the change affects networking, shutdown, or projectiles.
- For multiplayer checks, state the host/client setup explicitly and cover both directions where replication is involved.
- For Gun Game changes, test host and client for ladder grants, swap/draw lockout, throw and retrieve, level 18 (Tall Open Shelf), levels 1–2 semi-auto, and random spawns.

## Repo / source control

- **Not a git repo yet.** If initializing, never commit generated folders: `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`.

## Model sourcing and pickup content

- Use `Tools/AssetIntake/asset-intake.ps1` for Poly Pizza search/download. Accept CC0 only; never place the API key in the project or print it. The key is stored outside the project at `%USERPROFILE%\.config\gunverse\poly-pizza.key`.
- Downloaded source models go under `Assets/_GunVerse/Models/ThirdParty/`; full license/hash records go in `Tools/AssetIntake/asset-manifest.json`.
- Map imported models to existing pickups in `Tools/AssetIntake/item-models.json`, then run `GunVerse.EditorTools.ModelPickupPrefabBuilder.ApplyConfiguredModels()` and rebuild with `GroceryStoreSetupBuilder.Build()`.
- BopBuy electronics use `BopBuyExpandedGunPrefabBuilder` plus `item-models.json` entries; `BopBuyFixturePrefabBuilder` creates generated gun-able fixtures; `DepartmentSignPrefabBuilder` and `BopBuyDesignSignPrefabBuilder` create pickable department signs/banners; `BopBuyDesignPropPrefabBuilder` creates the design-specific generated props. `BopBuySetupBuilder.Build()` now delegates to `BopBuyDesignBuilder.Build()`, which reads the 292-block `MapDesignTemplate` and rebuilds BopBuy. The original 78 x 50 m layout is retained in `Assets/_GunVerse/Scenes/Backup/BopBuy_PreDesignBackup.unity` and `BopBuySetupBuilder.BuildLegacyLayout()`. Pickable merchandise and fixtures are gun-able, while only light supports, sign supports, walls, floor, and ceiling remain static; yellow holiday trees are removed. `BopBuyExpandedGunPrefabBuilder.BuildGun()` reconfigures existing prefabs in place to preserve GUIDs; content rebuilds can still change tuning, so rerun the Gun Game progression build afterward.
- All TV sizes use the Kenney `television-9trLeWoBek` model; do not reintroduce the old `tv-tdXTODKZlx`/`TV_2` model. Rebuild TV variants through `BopBuyExpandedGunPrefabBuilder` then `ModelPickupPrefabBuilder`.
- Generate item label textures with `Tools/AssetIntake/generate-item-labels.ps1`; `item-models.json` controls per-part colors and label placement.
- Normal item labels are 70%-opacity local hover-only visuals. Signs and welcome mats use `alwaysShowLabel`, stay visible at all times, and also use 70% opacity. After changing labels, run `GunItemLabelSetupBuilder.Apply()`.
- Test Mode uses `TestTargetPlayerPrefabBuilder`, `TestModeSpawnerSetupBuilder`, and `TestModeUiBuilder`. It is host-only, spawns five stationary score-counting targets, and must stay off for normal matches. Test target prefabs are registered separately from the gun registry.
- Burst/spread projectiles from the same trigger group ignore each other and spent projectiles; do not re-enable those collisions without checking Keyboard/Printer damage. The design build reports zero shelf overlaps through `BopBuyDesignBuilder.ValidateScene`; keep it that way. Map shelves are set `fixedInWorld` after the bake so products stay until the shelf is picked up or shot; products are placed using collider + renderer bounds.
- Approved manual sources: Kenney, Quaternius, Poly Haven. Do not use ripped or ambiguously licensed assets.

## Project layout

- `Assets/_GunVerse/Scenes/MainMenu.unity` — menu, `NetworkManager`, `UnityTransport`, `MultiplayerSessionManager`.
- `Assets/_GunVerse/Scenes/Lobby.unity` — session-preserving lobby used for returning from a match, changing mode, and starting the next match.
- `Assets/_GunVerse/Scenes/GroceryStore.unity` — gameplay scene with in-scene placed guns and `PlayerSpawnManager`.
- `Assets/_GunVerse/Scenes/BopBuy.unity` — design-driven map rebuilt from `MapDesignTemplate`; entrance center, checkout left front, Guysquad right front, mobile center, PC right middle, home-theatre/TV walls back, appliances left with local fog, warehouse back/right, and bathrooms right. The design build keeps sightlines short and validates shelf clearances. Add future maps to `GameplayMapCatalog` and the build settings.
- `Assets/_GunVerse/Scripts/Runtime/` — gameplay and networking code; `Assets/_GunVerse/Scripts/Editor/` — setup builders and prefab registration.
- `Assets/_GunVerse/Prefabs/` — guns, player, and other network prefabs; `Assets/_GunVerse/Settings/NetworkPrefabs.asset` — registered network prefab list.
- `Assets/_GunVerse/Settings/GunGameLadder.asset` — curated 20-slot Gun Game ladder. See **Gun Game** below for the order, tuning, and build pipeline.
- Running `MainMenuBuilder.Build()` recreates the MainMenu scene and removes `NetworkManager` and `MultiplayerSessionManager`; restore them afterward. Prefer patching the existing scene for small UI additions.
- `Assets/Settings/` — URP assets (separate `PC_RPAsset`/`Mobile_RPAsset` renderers, `UniversalRenderPipelineGlobalSettings`). Do not move these; URP references them by GUID.
- `Packages/manifest.json` — the dependency source of truth. Notable: `com.unity.inputsystem`, `com.unity.render-pipelines.universal`, `com.unity.netcode.gameobjects`, `com.unity.services.multiplayer`.

## Gun Game

- Ladder: `Assets/_GunVerse/Settings/GunGameLadder.asset` is a curated 20-slot ladder. Order and roles:
  1. Soup Can — semi-auto pistol
  2. Tomato Can — semi-auto pistol
  3. Detergent Bottle — semi-auto shotgun pistol
  4. Keyboard — semi-auto shotgun
  5. Printer — semi-auto shotgun
  6. Phone — automatic SMG
  7. Battery — automatic SMG
  8. Toaster — burst SMG
  9. Cereal Box — automatic AR
  10. Radio — burst AR
  11. Laptop — semi-auto AR
  12. Bread Loaf — automatic LMG
  13. Speaker — automatic LMG
  14. Lane Marker — semi-auto sniper
  15. Conveyor Belt — bolt sniper
  16. Checkout Counter — heavy sniper
  17. Shelf Run — throwable launcher
  18. Tall Open Shelf — throwable launcher
  19. Starlink Dish — single-shot
  20. Stanchion — one-shot skill weapon, 7 s reload, 100 damage
- Build: `Tools > Gun Verse > Build Gun Game Progression` applies `GunGameWeaponTuningBuilder` tuning, rebuilds the ladder from `GunGameSetupBuilder.CuratedLadderPaths`, assigns it to the GroceryStore `GameModeController`, and patches the lobby. Run it after any prefab or content rebuild; the ladder must never keep stale GUIDs.
- Validate: `Tools > Gun Verse > Validate Gun Game Ladder` prints the 20 entries. `SyncLadder` requires `GunItem`, `NetworkObject`, and at least one `Renderer` on every prefab; missing or invisible prefabs abort the build.
- `ShelfUnitGun.prefab` is an invisible map-builder proxy used by `BopBuyDesignBuilder`; never use it as a ladder weapon. Slot 18 is `TallOpenShelfGun.prefab`.
- Fire modes: `GunItem.GunFireMode` defaults to `Automatic` for all non-ladder guns. Ladder values live in `GunGameWeaponTuningBuilder`. Semi-auto and burst fire one trigger per click; automatic holds to fire. Slots 1–2 must stay semi-auto; do not restore full-auto or burst on Soup Can/Tomato Can.
- Swap: level-ups toss the current weapon, wait a weight-based toss delay (Light 0.25 s / Medium 0.45 s / Heavy 0.80 s), then grant the new weapon and play a weight-based draw (Light 0.35 s / Medium 0.65 s / Heavy 1.35 s). Firing is locked until the draw completes; the HUD shows `DRAWING...`. Self-projectile kills skip the toss. Tossed weapons stay on the floor for the rest of the match and are not pickable in Gun Game. Kills during a swap queue the latest level.
- Throwables: Shelf Run and Tall Open Shelf are self-projectiles with no reload. Retrieve them with E; only the player whose current ladder weapon matches can pick one up. Maintenance does not regrant while one is outstanding and regrants after the 30 s safety timeout. Pickup skips line-of-sight for the current throwable, and `GunProjectileBody` forces World state after 3 s if a large item will not settle.
- Spawning: `PlayerSpawnManager` chooses uniformly from safe spawn points (10 m distance + line-of-sight), excludes the previous point when possible, and randomly falls back when none are safe. TDM-specific spawn logic is not implemented yet.
- Known follow-ups: TDM spawn rules; Ceiling Light flash grenade; 3-throw disintegration for launchers; optional Stanchion projectile scaling; playtest tuning of swap durations and ladder stats.

## Hit Validation

- `ProjectileReplicationSettings` in `Assets/_GunVerse/Resources/ProjectileReplicationSettings.asset` selects the projectile path at match start. The server replicates the choice through `GameModeController`; toggle it with `Tools > Gun Verse > Toggle Client Visual Projectiles` and restart the match.
- When the flag is on, projectiles use client-side pooled visuals and the shared `ProjectileFlightSim`; no flight `NetworkTransform` is used. Phase A currently has no player-hit or pickup logic.
- World `GunItem`s in `World` state are projectile path obstacles, so shelves and counters block shots. `Held` and `Projectile` items do not alter the path.
- Gun colliders use layer 9 (`GunProp`). Large self-projectile hulls use layer 10 (`WorldHull`) in World state so they block players but do not collide with shelf products; `WorldHull` vs `GunProp` physics collisions are ignored. Raycasts/projectile sweeps still see both layers.
- Self-projectile visuals clone the actual held item as their source, not only the registered network-prefab list. The authoritative launch position removes the camera-right offset while preserving the gun's vertical and forward muzzle position; the local projectile visual starts at the held item's launch point and blends onto that path. Large self-projectiles launch from the held item centre; normal guns use the front-face muzzle. Muzzle flash VFX is currently broken and fully disabled.
- Item-visual projectiles use a box for thin/flat panels, a capsule for elongated props, and a sphere for near-cube shapes; the server derives the shape from collider bounds. Only world colliders already overlapping at launch are ignored during the `0.5 s` spawn grace; new geometry collides immediately. The firing player is ignored for the same `0.5 s`; other players bounce item-shaped projectiles. Do not apply this grace to normal guns.
- Self-projectiles apply collision-induced angular velocity and tumble on bounces; `maxAngularVelocity` is raised to 30 on gun props. Self-projectile ammo resets to `1` when retrieved (one throw per pickup for slots 17/18).
- If a held item's launch origin is more than `3 m` from the camera or more than `2 m` below it, the client falls back to the camera origin. This protects against bad/stale self-projectile transforms.
- Pickup hover and server pickup line-of-sight rays ignore the `WorldHull` layer so products inside a shelf can be targeted, while projectiles still hit the hull.
- Phase B is implemented: the server runs `ServerProjectileFlight`, normal shots spawn their real pickup at the settled pose, self-projectiles are hidden until the server reveal, props receive a single direct-hit impulse (fixed mounts are released on hit), and test targets take small-radius server-sim damage. Visual proxies remain colliderless while flying by design.
- When the flag is off, the original networked-Rigidbody projectile path runs unchanged.
- Movement and camera stay client-side; the server does not simulate movement.
- `NetworkConfig.TickRate` is 60 Hz. Keep it at 60 unless projectile smoothness is retested.
- Every weapon, including self-projectiles, spawns from the camera-center aim ray plus `FIRE_ORIGIN_OFFSET`, not the held item's world center.
- The server records a 120 Hz player hitbox history for rewind validation.
- There is exactly one visual projectile per shot: the authoritative server projectile. Do not reintroduce a second local predicted projectile visual or shooter-side hide/reveal logic; that caused disappearing objects and divergent bounces.
- The server records a short 120 Hz position history for every projectile (`ServerProjectileHistory`).
- The projectile owner reports hits from the projectile the player sees. `ClientProjectileHitDetector` sweeps a sphere from the last rendered position to the current rendered position each frame, including player triggers, and sends the projectile network object ID, victim, hit point, and rendered view time.
- The server validates against its own projectile history: the projectile's swept segment near the reported view time must be near the victim's rewound capsule. Client path length and bounce count are not used.
- The host uses the same path as a joined client; there is no host damage authority.
- Server projectiles retain their physics and prop interactions but have player damage disabled. Do not re-enable server projectile player damage without removing the corresponding hit-report damage path.
- Test targets still take server projectile damage directly so Test Mode remains usable.
