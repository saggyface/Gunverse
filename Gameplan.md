# GunVerse Prealpha Gameplan

## Game concept

GunVerse is a casual online multiplayer game where **everything in the store can become a gun**. Players pick up ordinary objects, fire them as projectiles, and use the environment itself as a weapon.

The game should feel fast, readable, physical, and funny. The goal is not merely to add systems; it is to make the grocery-store sandbox fun enough that players immediately want another round.

## Design pillars

1. **Everything is a gun.** Every non-structural prop should be pickable, holdable, fireable, and droppable.
2. **Readable chaos.** The game should be chaotic but each action should remain understandable through movement, sound, visuals, and timing.
3. **Physical comedy.** Oversized shelves, cash registers, falling products, and awkward objects are part of the fun.
4. **Fast return to action.** Short deaths, safe respawns, and clear combat feedback keep players playing.
5. **Simple to learn, surprising to master.** Picking up and firing is easy; learning which objects work best in which situations creates depth.

## Locked prealpha decisions

- **Fixed architecture:** walls, floor, ceiling, and entrance doors are static, non-networked scenery.
- **Gun-able props:** shelf runs, every decorative product, checkout counters, cash registers, belts, lane markers, light fixtures, signs, mats, and similar objects.
- **Shelf rule:** one shelf run is one gun. Each product on the shelf remains individually gun-able.
- **Loaded shelves:** picking up a shelf is allowed; products lose support and fall naturally.
- **Checkout rule:** each register and counter is a separate gun.
- **Players:** other players are not gun-able during this prealpha.
- **Players per session:** up to 8.
- **Hosting:** private host/client sessions with short join codes. No dedicated servers or host migration.
- **Game modes:** Team Deathmatch is the first new mode. Gun Game is the planned second mode.
- **Map plan:** polish and rework the grocery store before adding a second map.
- **Performance target:** 60 FPS at 1080p with 8 players and high projectile/prop activity.
- **Testing:** the user performs all gameplay and multiplayer testing. The agent implements, validates statically, and produces builds.

## Working agreement

- Execute one numbered step at a time.
- After finishing a step, stop and report what changed and how to test it.
- The user tests the result and explicitly approves moving to the next step.
- Do not begin later steps early or add unrelated features.
- Mark a step complete only after the user accepts it.
- If a step exposes a problem, fix that problem before continuing.
- **Testing waiver:** on 2026-11-04 the user directed that the deferred checks for Steps 9, 10, 11, Test Mode, and the performance hotfix be treated as done. They are accepted for planning purposes without an active playtest. Real playtest findings still count as regression fixes.

## Core technical rules

1. Every non-structural prop is a prefab with one root `GunItem` component and may contain multiple visual and collider children.
2. Fixed architecture has no `NetworkObject`, no `GunItem`, and no continuous synchronization.
3. A shelf run is one networked gun; its products are separate networked props.
4. Large props use bounds-aware pickup validation so players can grab a shelf by its edge rather than only from its center.
5. Held items use a configured pickup pose and scale. Dropped items return to their full world scale and are released physically from the rear-edge-at-muzzle clearance pose.
6. Every fired item is a full-size physical duplicate with its own colliders, Rigidbody, mass, and gravity; the server owns its physics and damage.
7. The server owns item state, shooting, damage, scoring, and respawning.
8. Projectiles are server-spawned and server-authoritative. Muzzle flash, fire audio, and trails are local cosmetic effects.
9. Keep game-mode rules out of `GunItem`; damage permission, team rules, and scoring belong to the active game mode.
10. All fixed UI and scene content must remain saved, editable scene objects.
11. Use only licensed assets: CC0 first, with Kenney, Quaternius, Poly Haven, and Poly Pizza as approved sources.
12. Do not add speculative performance complexity. The low-risk pooled AudioSource set is the only pre-profiling pooling; projectile pooling remains gated by Step 13 profiling.
13. All gun props are dynamic physics bodies with meaningful mass. Do not anchor them or teleport them after release.

## Current status

- The previous alpha foundation is complete and is preserved as the baseline: networking, sessions, main menu, player movement, GunItem pickups, firing, projectiles, ammunition, reload, health, death, respawning, FFA scoring, late joins, disconnect recovery, the grocery-store environment, CC0 model intake, and generated item labels.
- Step 1 is complete and accepted: this Gameplan defines the pillars, locked prealpha decisions, technical rules, measurable 8-player/60 FPS target, and out-of-scope list. `PLAYTEST_CHECKLIST.md` defines the fun evaluation.
- Step 2 (movement and camera feel) is complete and accepted.
- Step 3 (gun feel and combat feedback) is complete and accepted.
- Step 4 (everything-as-a-gun foundation) is complete and accepted, including all four shelf runs and their products.
- Step 5 (physical projectiles, bullet duels, and mass-driven impacts) is complete and accepted.
- Step 6 (remaining grocery-store contents) is complete and accepted.
- Step 7 (Grocery Store 2.0) is complete and accepted: a 72 x 49 m store, 5.1 m walls, five connected combat zones, safe spawn selection, and a 1-second spawn shield.
- Step 8 (game-mode framework and Team Deathmatch) is complete and accepted.
- Step 9 (match flow) is complete and accepted by user direction. It adds the 5-minute timer, TDM/FFA score limits, results screen, rematch-by-scene-reload, and the session-preserving Lobby.
- Step 10 (Gun Game) is complete and accepted by user direction. It adds the strict auto-granted 25-entry ladder, per-kill progression, level HUD, ladder-completion win, and timer fallback.
- Step 11 (content expansion and BopBuy) is complete and accepted by user direction. It adds the design-driven BopBuy layout, host map selection, the expanded electronics/appliance arsenal, fixture gun props, Guysquad/BopBuy named props, 70%-opacity local hover-only labels, and burst/spread/gravity/bouncy behavior.
- BopBuy is now design-driven from `MapDesignTemplate` and stabilized: **847 gun props**, **28 spawns**, zero remaining scaled/unbaked `GunItem` offenders, and **76 props baked** into dedicated prefabs. The active BopBuy scene is the manually adjusted live scene; full rebuilds must preserve or reapply those adjustments.
- Test Mode is complete and accepted by user direction: host-only lobby toggles in MainMenu and Lobby, five stationary player-shaped targets, death/respawn, and scoreboard/kill-feed integration.
- The pre-playtest performance hotfix is complete and accepted by user direction: runtime sleeping for world props, reduced NetworkTransform scale/threshold traffic, cached projectile/fire-feedback data, allocation-free submesh lookup, pooled audio sources, HUD/ammo caching, and spawn-shield push protection. Full 8-player profiling remains part of Step 13.
- Latest stabilization pass is complete and built: rematch player-spawn recovery, all-players countdown gate with `WAITING FOR PLAYERS...` then `3 / 2 / 1`, active BopBuy ceiling, subtle host-tunable fog, solid collision proxies on large props, projectile collisions against players/props/other bullets, and one-shot self-projectile shelves.
- Latest Windows build: `build_04a133faa575`, succeeded with 0 errors.
- **Next active step: Step 12 — Audio, UI, and visual polish.**
- **Active technical rework before Step 12: client-visual projectile flight and hit registration.**

## Active technical rework — projectile flight and hit registration

**Status:** Phase A, the A.1 hotfix, and Phase B are implemented and built (`build_606d032b4445`); awaiting playtest. Phase C has not started. Step 12 is paused until the projectile rework is accepted.

**A.1 hotfix:** self-projectile visuals now clone the actual held item (shelves/counters), world `GunItem`s in World state are static path obstacles for projectiles, and the launch point/flash are horizontally centered in camera space while retaining the gun's vertical and forward offset.

**Phase B:** the server runs the shared flight sim for every accepted shot; normal shots spawn a real pickup at the settled pose, self-projectiles stay hidden until the server settles and reveals them, props receive manual impulses, test targets take direct server-sim damage, and visual proxies persist until the server reveal event instead of disappearing at settle.

**Problem**

- The projectile everyone sees is the server's networked `Rigidbody`, so the shooter sees it only after a server round trip and from a stale position.
- Remote clients have no `NetworkTransform` history when a projectile spawns, so it pauses at the muzzle until the second update.
- The current client hit report is attached to the late server projectile, so it reports where the server projectile was rendered instead of where the shooter fired.

**Locked design decisions**

- Spent shots still become pickups. The real `GunItem` stays server-side and hidden during flight; a server-driven reliable reveal event makes it visible and retrievable after the shared sim settles.
- Projectile origin is the held item's front-face muzzle. Direction aims from that muzzle toward the camera crosshair point. Camera-center spawning is removed for this path.
- The server projectile uses the same fixed-step flight simulation as the visuals. It has no `Rigidbody` flight, no renderers, and no `NetworkTransform`. Props are pushed with manual impulses; the server ignores players for damage.
- A projectile stops on its first player hit.
- Visual-only proxies are built once from stripped weapon prefabs and pooled. Proxies are never stripped per shot.

**Architecture**

- Shooter builds `ProjectileShotData`, spawns the visual immediately, and sends the shot to the server.
- Server validates ammo, reload, fire rate, gameplay state, pellet cap, and weapon stat bounds; records the shot; starts the invisible server sim; sends the shot event to every client except the shooter.
- Remote clients spawn the visual from the shooter's rendered muzzle, fast-forward by shot age, and decay the muzzle offset over roughly `0.15 s`.
- One `ProjectileFlightSim` drives client visuals and the server sim.
- The sim's path collisions use static world geometry only. Dynamic props never alter the path.
- Visual proxies pass through dynamic props; the server applies impulses with a separate non-path overlap pass.
- Client visuals detect player hits with a swept sphere and report the bounce path. The server validates the path and rewound victim capsule.
- Validated hits apply damage and client-authoritative knockback through the victim's controller.
- Test targets still take direct server-projectile damage.

**Feature flag**

- `ProjectileReplicationSettings` in `Assets/_GunVerse/Resources/ProjectileReplicationSettings.asset`.
- The flag is read by the server at match start and replicated through `GameModeController`.
- When false, the old networked-Rigidbody path runs unchanged.
- The flag is switched between matches, not mid-flight.

**Phases**

- **Phase A:** shot data, fire RPCs, shared flight sim, pooled stripped visuals, instant shooter visual, remote fast-forward and muzzle-offset decay. No hit logic and no pickup logic.
- **Phase B:** invisible server sim, manual prop impulses, settle/reveal of the real `GunItem`, shelf/counter handling, disconnect/round-end cleanup.
- **Phase C:** shooter visual player hits, hit reports, server shot registry validation, damage, hit markers, knockback, reject-reason logs.
- **Phase D:** tuning and cleanup. Remove the old path only after the flag-off path is no longer needed.

**Phase A acceptance**

- Joined client has no pause at the muzzle.
- No left/right offset while strafing.
- Host and client bounce paths match for static geometry.
- The flag-off path still behaves exactly as before.


## Playtest backlog — closed by user direction (2026-11-04)

The user directed that all deferred checks below be treated as done so the plan can advance to Step 12. The checkboxes are retained as the original acceptance list and are marked closed by waiver; they were not actively executed in a playtest. Any issue found later is handled as a regression fix.

### Match flow tests (Step 9)

- [x] TDM: play from the countdown to 50 team eliminations. The results screen must show the winning team and final score.
- [x] TDM timer: leave a match idle to 5:00. Highest team score wins; equal scores are a draw.
- [x] FFA: verify both the 25-kill limit and the 5:00 timer. Winner and top scores must be correct.
- [x] Locked gameplay during results; cursor unlocked; host Rematch and Return to Lobby buttons respond.
- [x] Client on results sees the waiting state and can Leave to Menu without breaking the host.
- [x] Rematch reloads the active map with fresh props, scores at zero, full health, no held items or projectiles, and the 3-second countdown.
- [x] Return to Lobby keeps the same session and join code; the client follows; the host can change mode and start another match.
- [x] Late join during a match, during results, and while the host is in the returned Lobby.
- [x] Host departure at results and from the Lobby returns clients safely to MainMenu; no stale HUD, scoreboard, or results UI remains.
- [x] Check `Player.log` for scene-load, player-object, or ownership errors during both scene transitions.

### Gun Game tests (Step 10)

- [x] Host selects Gun Game in both the initial MainMenu lobby and the returned Lobby; the client follows.
- [x] Every player starts with Soup Can at `1 / 25`; the HUD weapon name is correct.
- [x] Each kill advances exactly one ladder step, including multi-kills; no skipped or double steps.
- [x] Strict mode: world pickups and manual drops do nothing; no pickup prompt appears.
- [x] Death keeps the current level; respawn grants the current weapon.
- [x] A late joiner starts at Soup Can and receives a weapon automatically.
- [x] The curated ladder includes Headphones, Monitor, Radio, and Toaster; Guysquad Counter and BopBuy Sign stay out of the ladder but remain pickable outside Gun Game.
- [x] Keyboard fires a spread volley; Printer fires a three-shot burst; Laptop arcs; Battery/Speaker bounce; Radio and Router use small spreads; Toaster and Computer use bursts.
- [x] A kill with the final Shelf Run ends the match with `LADDER COMPLETE` and the correct winner.
- [x] At 5:00, the highest ladder level wins; ties break by kills, then draw.
- [x] Results show the top three ladder levels; Rematch resets progression; Return to Lobby and mode switching work.
- [x] Add a gun to the curated list, register it, run `Tools > Gun Verse > Sync Gun Game Ladder`, and confirm it appears before the Shelf Run finale.
- [x] Check `Player.log` for missing network prefab, held-item, ownership, or grant errors.

### BopBuy, map selection, and new guns (Step 11)

- [x] Host can cycle `Map: Grocery Store` / `Map: BopBuy` in both the initial menu lobby and the returned Lobby.
- [x] The client follows the host into BopBuy; the join code remains the same.
- [x] BopBuy loads at 78 x 50 m with the original blue/yellow shell, HUD, pause menu, results, join code, 28 spawns, 847 props, and the GameMode framework intact.
- [x] FFA, TDM, and Gun Game all work on both maps.
- [x] Rematch reloads the current map; Return to Lobby preserves the map selection; a map change applies on the next start.
- [x] The restored original layout is present: entrance center; checkout left front; Guysquad right front; mobile center; PC right middle; home theatre/TV wall back; appliances left; stockroom back-right.
- [x] The original blue/yellow shell is present and the detailed 58 x 52 m shell is gone.
- [x] The dense shelf layout has no extremely long clear sightline and 847 props are placed without blocking the 28 spawns.
- [x] All TV sizes use the current TV mesh; the separate Television asset is gone and no TV_2 object remains.
- [x] All expanded guns show their CC0 models and labels and have correct pickup, held scale, fire, reload, and damage.
- [x] Keyboard/Radio/Router spreads; Printer/Toaster/Computer bursts; Laptop arcing; Battery/Speaker/Large Speaker bounce.
- [x] Keyboard fires all 5 damaging pellets and Printer fires all 3 damaging burst shots even after the first projectile hits a wall, prop, or player.
- [x] BopBuy starts with no shelf overlaps, jitter, or physics explosions; the shelf clearance validator reports 0 overlaps.
- [x] TV sizes and large appliances behave as heavy/oversized weapons and respect their active-projectile caps.
- [x] Every counter, table, plinth, ledge, cart, locker, cooler, holiday box, and mounted prop is gun-able.
- [x] Only light supports, sign supports, walls, floor, and ceiling refuse pickup.
- [x] All added signs/banners are pickable and always show labels at 70% opacity; no yellow HolidayTreeGun remains.
- [x] Normal item labels stay hidden until hovered; held non-sign items hide labels; labels are 70% opaque.
- [x] Labels are local-only: the host hovering an item does not show its label on the client.
- [x] Guysquad Counter hides its label until hovered; Guysquad room sign stays visible.
- [x] The five GroceryStore sample electronics are present and pickable.
- [x] Host and client agree on BopBuy prop state, scores, and late-join state.
- [x] BopBuy runs at an acceptable frame rate and logs no missing-prefab, scene, or ownership errors.

### Test Mode
- [x] Host sees `Test Mode: Off/On` in both the initial MainMenu lobby and returned Lobby; clients do not see it.
- [x] Enabling Test Mode and starting a match spawns exactly 5 `TEST 1`–`TEST 5` targets in the chosen map.
- [x] Test targets are stationary, player-shaped, replicated to clients, and do not count toward the 8-player session limit.
- [x] Shooting a test target produces hit feedback; it dies, hides its body/hurtbox, and respawns after 3 seconds.
- [x] Test-target deaths count toward the attacker's FFA/TDM score and Gun Game ladder progression.
- [x] Test targets appear in the scoreboard/kill feed, cannot win, and do not grant themselves weapons.
- [x] Test Mode Off on the next match spawns no targets and leaves normal scoring unchanged.

### Performance hotfix

- [x] Resting props settle and stop jittering; shelves do not drift or float.
- [x] Held items show the correct scale on both host and client.
- [x] Retiring projectiles still shrink before despawning.
- [x] Gunfire, footsteps, reloads, and impacts play from the expected positions.
- [x] Spawn-protected players receive no damage and are not pushed or launched by projectiles.
- [x] Run a fire-heavy 8-player session and compare FPS and GC allocation in the Unity Profiler; it must be better or no worse than before the hotfix.
- [x] No new errors or warnings in `Player.log`.

### Cross-step regression

- [x] Step 8 FFA/TDM basics still work: teams, colors, friendly fire, scores, scoreboard, kill feed, load countdown, and late join.
- [x] Movement, sprint, jump, pickups, firing, reload, death, respawn, and disconnect recovery still work in FFA/TDM.
- [x] Host and client agree on held items, projectiles, scores, and UI after a rematch and after returning to the Lobby.

---

## Step 1 — Rebaseline and define "fun"

**Purpose**

Replace the old alpha checklist with a plan focused on the game's identity and play feel.

**Work**

- Preserve the completed systems as the baseline.
- Establish the everything-as-a-gun rules and static architecture boundary.
- Define a measurable target of 8 players, 60 FPS at 1080p, and stable high-projectile sessions.
- Create a playtest checklist that evaluates fun, not only technical correctness.
- Document the scope of the prealpha and what remains outside it.

**User test**

- Review and approve the new direction and scope.

**Done when**

- The user accepts this Gameplan and Step 1.

---

## Step 2 — Movement and camera feel

**Purpose**

Make basic movement satisfying before adding more weapons, props, and modes.

**Work**

- Replace instant movement with acceleration and deceleration.
- Add sprinting with a subtle FOV shift and weapon/camera sway.
- Add coyote time, jump buffering, and tuned air control.
- Improve jump height, gravity, and landing response.
- Add camera bob, landing dip, and weapon sway.
- Add footstep and landing sounds.
- Keep the existing sensitivity setting working.
- Avoid adding movement mechanics that conflict with the current network ownership model.

**User test**

- Move, sprint, jump, and land around the store repeatedly.
- Confirm movement feels responsive and comfortable for both host and client.
- Confirm sensitivity and look feel remain consistent.

**Done when**

- Traversal and camera feel good enough that movement alone is not a source of frustration.

---

## Step 3 — Gun feel and combat feedback

**Purpose**

Make firing, hitting, taking damage, and dying readable and satisfying.

**Work**

- Add muzzle flashes, tracers or streaks, and impact effects.
- Add recoil kick, camera shake, and weapon kickback.
- Add hit sounds and stronger hit-marker feedback.
- Add directional damage feedback.
- Improve death and kill feedback with sound and screen effects.
- Improve reload motion and reload sounds.
- Give light, medium, and heavy props distinct firing feels.
- Keep cosmetic effects local and inexpensive.

**User test**

- Fire all current items.
- Confirm shots, hits, misses, reloads, damage, kills, and deaths are understandable without reading text.
- Check both host and client for delayed or missing feedback.

**Done when**

- Combat feels responsive and readable even when multiple players are fighting.

---

## Step 4 — Everything-as-a-gun foundation

**Purpose**

Prove that large, multi-part store objects can work as guns before converting the entire map.

**Work**

- Create a standard gun-prop root containing `NetworkObject`, `NetworkTransform`, `GunItem`, `Rigidbody`, and multiple child colliders.
- Add bounds-aware pickup validation so large props can be grabbed from their nearest edge.
- Fix line-of-sight checks so a shelf does not block its own pickup.
- Add anchored world physics so shelves and counters do not slide or fall while unused.
- Define held scale, grip offset, and camera clearance for oversized props.
- Add safe dropping for large objects.
- Add optional simplified projectile visuals so a shelf does not instantiate its entire mesh for every shot.
- Keep server collision authoritative while using simplified visual projectiles.
- Convert one shelf run and one cash register end to end.

**User test**

- Pick up and fire one shelf run and one cash register as host and client.
- Confirm both players see the same held and dropped state.
- Confirm projectiles hit correctly and do not destroy performance.
- Confirm a late joiner sees the correct state.

**Done when**

- One loaded shelf and one register work correctly through pickup, firing, dropping, networking, and late join.

---

## Step 5 — Physical projectiles, bullet duels, and mass-driven impacts

**Purpose**

Turn projectiles into persistent full-size physical objects so firing behaves like launching the actual item, enables bullet duels, and makes mass drive impacts and shelf drops.

**Work**

- Firing creates a full-size duplicate of the held gun; the held gun remains.
- Server-spawn a clone of the gun prefab with authoritative Rigidbody physics.
- Add `GunProjectileBody` to gun prefabs to own projectile damage, limits, and state.
- Use Option A: no client prediction. Play muzzle flash and fire audio locally, then spawn the authoritative projectile on the server.
- Spawn the full-size projectile with its rear edge at the muzzle, extending forward.
- Add gravity, sensible per-item mass, a bouncy physics material, and continuous collision detection.
- Remove all world anchoring. Shelves, registers, products, and projectiles are dynamic Rigidbodies.
- Light and medium bullets spend when they hit a player.
- Heavy bullets stay live after hitting players, can damage multiple players, and track each player so it only damages them once.
- Any surface, object, or projectile collision spends the bullet. Bullet-vs-bullet spends both.
- Spent bullets remain physical and become pickable once slow, with a full magazine.
- Add a per-type active-projectile limit of 30, a shelf limit of 8, and a global active-projectile cap of 500.
- Oldest projectile of the type, or oldest globally at the cap, disables damage/collision, shrinks/fades, and despawns.
- Remove safe-drop restrictions and teleporting. Dropping releases the shelf from the rear-edge-at-muzzle pose and lets it fall.
- Add a small outward/upward impulse only when objects are trapped inside a released shelf.
- Launch players caught under a released shelf.
- Keep walls, floor, ceiling, and doors static and unsynchronized.

**User test**

- Fire every gun type as host and client; confirm full-size physical bullets, gravity, bouncing, trails, and impacts.
- Confirm light/medium bullets spend on player hits and heavy bullets can damage multiple players before hitting a surface.
- Collide bullets with each other and confirm both become spent but remain physical.
- Pick up spent, slow bullets and confirm they are full guns; confirm limits and shrink-out.
- Drop and fire shelves; confirm rear-edge placement, falling physics, object pushes, and player launches.
- Confirm cereal cannot move a shelf and a shelf launches cereal.
- Confirm late joiners see correct bullet and shelf state.
- Confirm active projectiles never exceed 30 per type, 8 shelves, or 500 global.

**Done when**

- Full-size physical projectiles, bullet duels, pickups, limits, mass-driven shelf impacts, and player launches work without breaking the match.

---

## Step 6 — Convert the remaining grocery-store contents

**Purpose**

Make every remaining allowed world object part of the weapon sandbox.

**Work**

- The four shelf runs and their gun-able products are already converted and are part of the baseline.
- Split each remaining checkout into a counter gun and a separate register gun.
- Convert belts, lane markers, light fixtures, and mats.
- The Step 8 layout had no sign props; Step 11 adds pickable Guysquad, BopBuy, and department signs.
- Give each prop readable display names, labels, hold size, ammo, reload, damage, and physical projectile behavior.
- Verify every prop is reachable and pickable.
- Keep walls, floor, ceiling, and doors static and unsynchronized.

**User test**

- Pick up, fire, and drop every remaining prop in the store.
- Confirm no prop is unreachable, duplicated, lost, or impossible to drop.
- Confirm host, client, and late joiners agree on prop state.

**Done when**

- Every allowed prop in the store can be used as a gun without breaking the match.

---

## Step 7 — Grocery Store 2.0

**Purpose**

Turn the current test layout into a fun combat space built around removable cover and prop density.

**Work**

- Expand to a 72 x 49 m store with 5.1 m walls; keep player and prop scale normal.
- Rework aisle width, sightlines, checkout flow, entrance space, and back-of-store space.
- Add five distinct connected combat zones: entrance/checkout, west aisles, east aisles, produce/bakery, and back stockroom.
- Use shelves and counters as intentional dynamic cover.
- Improve spawn placement, add safe spawn selection, and add a 1-second spawn shield that ends on firing.
- Increase prop density without exceeding the performance budget.
- Use counters as jump-up cover; no new mezzanine or stairs.
- Keep the low-poly grocery-store identity readable during combat.

**User test**

- Play an extended FFA session on the reworked map.
- Check every spawn, aisle, checkout lane, and back area for traps or dead zones.
- Confirm fights happen quickly and the map does not feel too empty or too cluttered.

**Done when**

- The map produces active, readable, repeatable fun before new modes are added.

---

## Step 8 — Game-mode framework and Team Deathmatch

**Purpose**

Move beyond FFA using a reusable rules layer.

**Work**

- Refactor `FfaGameMode` into a generic game-mode structure.
- Add auto-balanced teams, team colors, friendly-fire protection, and team scores.
- Use the shared map-wide spawn pool for both teams with safe spawn selection.
- Update the scoreboard and kill feed for teams.
- Keep damage permission and scoring outside `GunItem`.
- Add host-only FFA/TDM mode selection in the lobby.
- Add a load-in countdown that waits for all connected clients, then starts a 3-second countdown with movement, firing, and pickups locked.
- Preserve FFA as a selectable mode.

**User test**

- Host and client complete a TDM match.
- Confirm teams, colors, friendly fire, spawns, scores, scoreboard, and kill feed are correct.
- Confirm both modes can still be selected and started.

**Done when**

- Team Deathmatch works end to end with no scoring or team-assignment errors.

---

## Step 9 — Match flow

**Purpose**

Give matches a beginning, end, and rematch loop.

**Status:** Complete and accepted by user direction on 2026-11-04. The historical acceptance list is closed in the Playtest backlog section above.

**Work**

- Reuse the Step 8 load-in countdown as the pre-match countdown with movement, firing, and pickups locked.
- Add a 5-minute match timer for FFA and TDM.
- Add a TDM 50-team-elimination limit and an FFA 25-player-elimination limit; ties are draws.
- Add a win state and results screen with winner, end reason, and final score; lock gameplay and unlock the cursor.
- Add host Rematch (reload GroceryStore) and session-preserving Return to Lobby (new Lobby scene).
- Reset scores, health, held items, and respawns by reloading GroceryStore for a rematch.
- Keep server-side fire, pickup, and reload gates closed outside the Playing phase.
- Ensure host departure and disconnect still return clients safely to the menu.

**User test**

- Play a full TDM match from countdown to the 50-kill limit, then a separate timer-expiry match.
- Play a full FFA match to 25 kills and to 5:00.
- Rematch and start another match; confirm fresh props, zero scores, full health, no held items, and the countdown.
- Return to the Lobby, change mode, and start again; confirm the client follows and the join code is preserved.
- Confirm no stale scores, held items, or UI remain.
- Check host departure during results and from the Lobby, plus a client leaving during results.
- Check late join during a match, during results, and in the returned Lobby.

**Done when**

- A complete match can start, finish, and restart without leaving the lobby flow.

---

## Step 10 — Second game mode: Gun Game

**Purpose**

Showcase the entire item catalog and the game's physical comedy.

**Status:** Complete and accepted by user direction on 2026-11-04. The historical acceptance list is closed in the Playtest backlog section above.

**Work**

- Add Gun Game as a host-selectable mode in the initial MainMenu lobby and the returned Lobby.
- Use a fixed ladder of all 13 gun prefabs at Step 10, from Soup Can through Shelf Run. Step 11 revises it to a curated 25 entries.
- Strict mode: auto-grant the current ladder weapon; disable world pickups and manual drops; despawn the previous weapon on advancement and death.
- Advance exactly one step per kill; a kill with the final Shelf Run wins the match.
- Keep progression on death; grant the current-level weapon on spawn, respawn, and late join.
- Add HUD progression and a level-up notification.
- Add a results view with the top three ladder levels and reuse the match-flow rematch/lobby screens.
- Add a 5-minute timer; highest level wins at time-up, then kills, then draw.
- Add `Assets/_GunVerse/Settings/GunGameLadder.asset` and `Tools > Gun Verse > Sync Gun Game Ladder` so the curated ladder can be rebuilt before the Shelf Run finale.

**User test**

- Play a full Gun Game match with at least two players.
- Confirm every player starts with Soup Can at `1 / 13` and each kill advances exactly one step.
- Confirm strict pickups/drops, progression on death, late join, and the final Shelf Run win.
- Confirm the 5:00 fallback winner and the results top three.
- Confirm Rematch resets progression and Return to Lobby allows a mode switch.
- Add a new gun prefab, run the ladder sync, and confirm it appears before the finale.
- Check `Player.log` for grant, held-item, or missing-prefab errors.

**Done when**

- Gun Game works end to end and makes players use a wide range of props.

---

## Step 11 — Content expansion and second map

**Purpose**

Add variety beyond the first store and the initial prop set.

**Status:** Complete and accepted by user direction on 2026-11-04. The historical acceptance list is closed in the Playtest backlog section above.

**Work**

- Restore BopBuy to the original 78 x 50 m blue/yellow shell with checkout left, Guysquad right, mobile center, PC right-middle, TV wall back, appliances left, and stockroom back-right.
- Reuse the GroceryStore gameplay systems by copying the scene and replacing only environment, props, spawns, and lighting.
- Keep the new props, fixtures, signs, and expanded gun families; remove all yellow holiday trees and place new merchandise on dense staggered shelves.
- Use the original blue/yellow entrance with sliding glass doors, three EAS pedestals, carts/corral, greeter station, queue stanchions, and security monitor.
- Populate BopBuy with 847 gun props and 28 clear spawns, including dense staggered shelf runs, 18 current-model TVs, appliances, phones, printers, tablets, holiday boxes, and department fixtures; keep clear sightlines short.
- Add host-only map selection (`Map: Grocery Store` / `Map: BopBuy`) to the initial and returned lobbies.
- Add eight original CC0 electronics guns plus 15 expanded model-backed guns; all TV sizes use the current TV mesh, and the separate Television asset is removed.
- Add Guysquad Counter, BopBuy Sign, department signs, banners, holiday boxes, carts, EAS pedestals, lockers, counters, tables, plinths, ledges, and other fixtures as gun props.
- Add host-only Test Mode to both lobbies; spawn 5 stationary, score-counting test targets in the chosen map.
- Add spread (Keyboard/Radio/Router), timed burst (Printer/Toaster/Computer), arcing gravity (Laptop), bouncy physics (Battery/Speaker/Large Speaker), and heavy/oversized variants.
- Add generated labels for every new gun and pickable sign. Normal labels are 70% opacity and local hover-only; signs and welcome mats are 70% opacity and always visible.
- Add a small set of sample electronics to GroceryStore.
- Curate the Gun Game ladder to 25 entries while keeping Shelf Run as the final weapon; keep signs and fixture guns outside the ladder.
- Apply the shared performance settings to every new prefab and both gameplay scenes.

**User test**

- Play both maps with FFA, TDM, and Gun Game.
- Confirm BopBuy has valid spawns, reachable props, and no collision traps.
- Confirm the dense department layout is readable and the normal labels appear only on hover.
- Confirm map selection, rematch, and return-to-lobby work for host and client.
- Confirm the new guns, signs, and named props feel distinct and do not break scoring or networking.

**Done when**

- Two maps and a substantially larger weapon set are playable with all three modes.

---

## Step 12 — Audio, UI, and visual polish

**Status:** Next active step.

**Purpose**

Make the game feel intentional instead of prototype-built.

**Work**

- Add an audio mixer with master, SFX, music, and UI groups.
- Add music, ambience, footsteps, impacts, reloads, and pickup sounds.
- Improve lighting, post-processing, colors, and prop readability.
- Add item outlines, prompts, and clearer interaction feedback.
- Polish the main menu, lobby, scoreboard, kill feed, death screen, and results screen.
- Add a controls screen and an onboarding tip for "everything is a gun."
- Keep UI and effects performant.

**User test**

- Check every menu, HUD state, sound, and effect in host and client sessions.
- Confirm settings still persist and master volume still affects all audio.
- Confirm the game reads clearly during chaotic fights.

**Done when**

- The game has a cohesive, readable look, sound, and UI identity.

---

## Step 13 — Performance and networking hardening

**Purpose**

Make the expanded prop sandbox stable for 8 players.

**Status:** A pre-playtest subset is already applied: runtime sleep thresholds for world props, reduced NetworkTransform scale/threshold traffic, per-shot and per-projectile allocation caches, allocation-free submesh lookup, pooled AudioSources, HUD/ammo caching, and projectile-spawn controller caching. Remaining for this step: full 8-player profiling, non-alloc projectile hit sweeps, projectile pooling/simplified large-projectile visuals, LOD/batching/shadow tuning, bandwidth tuning, and late-join/disconnect stress testing.

**Work**

- Profile 8-player matches with high prop density and continuous firing.
- Measure host CPU, network bandwidth, physics, and rendering cost.
- Optimize idle world-prop transform updates.
- Use simplified projectile visuals for large props.
- Add projectile pooling only if profiling proves it is necessary.
- Sleep or simplify physics for resting props.
- Add LODs, batching, and distance-based detail where needed.
- Re-test late joins, disconnects, repeated matches, and host departure.

**User test**

- Run extended 8-player sessions with constant firing, shelf pickups, and product drops.
- Check frame rate, network stability, memory use, and host performance.
- Confirm no new duplication, state, or disconnect bugs appear under load.

**Done when**

- The target frame rate and network stability hold during extended sessions.

---

## Step 14 — Prealpha playtest and release

**Purpose**

Package a playable prealpha and use real playtests to decide whether the game is actually fun.

**Work**

- Produce a clean Windows build.
- Package controls, setup instructions, and known limitations.
- Run extended 8-player sessions.
- Review logs and recurring errors.
- Collect feedback on movement, prop comedy, map flow, TDM, Gun Game, and performance.
- Fix blockers before adding another large feature.

**User test**

- Complete an extended host/join session with movement, shooting, shelf pickups, swapping, dying, respawning, leaving, and rejoining.
- Confirm settings persist after restarting the build.
- Confirm no item duplication, duplicate scoring, stuck menus, or recurring errors remain.

**Done when**

- The prealpha is stable, understandable, and produces "one more round" feedback from playtesters.

---

## Explicitly outside this prealpha

- Gun-able players or player ragdolls.
- Dedicated servers and host migration.
- Public matchmaking or a server browser.
- Accounts, profiles, progression, rankings, or persistent unlocks.
- Team Deathmatch beyond the planned implementation, Capture the Flag, and other modes not listed.
- Multiple inventory slots.
- Mobile or console support.
- Paid content or monetization.
