# Gun Verse Alpha Limitations

## Session model

- The host is also the server. There is no dedicated server or host migration.
- Host departure ends the match. Clients return to the main menu and must rejoin
  with a new code if the host starts another match.
- After a match, the host can return to the session lobby without ending the
  session. The same join code and player list are preserved, and the host can
  change mode or map and start another match.
- The host selects the map in the lobby with a host-only cycle button. Clients
  follow the selected map when the match starts.
- There is no reconnect directly into an in-progress match.
- Sessions are private and limited to 8 players. There is no public matchmaking
  or server browser.

## Networking

- Projectiles are full-size server-spawned network objects with authoritative
  Rigidbody physics. They are not pooled; eight-player high-load sessions should
  be profiled before adding pooling.
- There is no client projectile prediction. The shooter sees muzzle flash, fire
  audio, and recoil immediately, then the authoritative projectile appears when
  the server spawns it.
- Projectile colliders are active on the server and disabled on non-server peers
  while in flight. Clients follow the server-synced trajectory; damage and
  impacts remain server-authoritative.
- Late joiners receive in-flight projectile objects at their current positions.
  Trail history for projectiles already in flight is not replayed.
- Muzzle flashes, projectile trails, and impact effects are local cosmetic
  objects, not replicated network objects.
- Active projectiles are capped at 30 per item type, 8 shelves, and 500
  globally. The oldest eligible projectile shrinks, fades, and despawns when a
  cap is exceeded. Spent but still pickable duplicates keep counting toward the
  caps until they are picked up or retired.
- World gun props apply a runtime sleep threshold on the server so resting
  physics can sleep. Gun NetworkTransforms do not synchronize scale and use
  coarser position/rotation thresholds; the player keeps precise movement
  thresholds.
- Static scenery is not networked.

## Gameplay scope

- Free-for-all, Team Deathmatch, and Gun Game are implemented. The host selects
  the mode in the lobby; clients see the active mode in gameplay.
- TDM auto-balances players across Blue and Red, disables friendly fire, and
  tracks team scores. Team colors, scoreboard, and kill feed are implemented.
- Gun Game is a strict free-for-all ladder: each player is automatically granted
  the next grocery gun on spawn and after every kill. World pickups and manual
  drops are disabled in this mode. The previous ladder weapon is despawned when
  the player advances.
- Gun Game keeps progression on death. The player respawns with their current
  ladder weapon. A kill with the final weapon (Shelf Run) wins the match.
- The Gun Game ladder is a curated 25 entries. It keeps the original grocery
  line, the original eight electronics, and adds Headphones, Monitor, Radio, and
  Toaster. Guysquad Counter, BopBuy Sign, all department signs/banners, holiday
  props, fixture guns, and the remaining expanded guns stay out of the ladder but
  remain pickable in FFA/TDM.
- The Gun Game ladder lives in `Assets/_GunVerse/Settings/GunGameLadder.asset`.
  `Tools > Gun Verse > Sync Gun Game Ladder` now rebuilds it from the curated
  list in `GunGameSetupBuilder`; it does not auto-append every gun prefab.
  Register new prefabs as network prefabs and edit the curated list explicitly.
- BopBuy is the original 78 x 50 m blue/yellow electronics store with entrance center, checkout left front, Guysquad right front, mobile center, PC right middle, home-theater/TV wall back, appliances left, and stockroom back-right. It keeps the expanded prop set inside the original shell.
- The original blue/yellow shell uses a 6.5 m store ceiling and the original
  lighting/floor treatment. The detailed black-truss, wood-aisle 58 x 52 m
  shell is removed. Dense staggered shelf runs shorten long sightlines.
- The electronics arsenal has 23 model-backed guns: the original TV, Battery,
  Speaker, Keyboard, Laptop, Phone, Printer, and Microwave plus Headphones,
  Mouse, Monitor, Computer, Radio, Toaster, Electric Oven, Washer, Air
  Conditioner, Large Speaker, Router, Flip Phone, TV 32, TV 65, and TV 75.
  All TV sizes use the current `tv.glb` mesh; the separate Television asset has
  been removed and no `TV_2` object remains.
- Additional model-backed props include Gift Box, TV Box, Ink Box, Tablet, and
  Starlink-style Satellite Dish. Generated gun prefabs cover shopping carts,
  cart corral, EAS pedestals, greeter furniture, stanchions, beverage cooler,
  lockers, workbench, high tables, stools, cameras, doorbells, desk fan,
  security monitor, phone cases, wallets, screen protectors, and display
  fixtures. All yellow holiday trees have been removed.
- Keyboard, Radio, and Router use spread volleys; Printer, Toaster, and Computer
  use timed bursts; Laptop uses stronger gravity for an arcing shot; Battery,
  Speaker, and Large Speaker use a bouncy physics material. Burst/spread
  projectiles from the same trigger group ignore collisions with each other, and
  active projectiles ignore spent projectiles. This preserves multi-pellet and
  multi-shot damage while keeping active-vs-active bullet collisions between
  different shooters.
- BopBuy shelf placements are checked by an editor clearance validator; the
  current layout reports zero overlapping shelf bounds and 28 clear spawns.
- Normal item labels are local hover-only visuals at 70% opacity. Signs and
  welcome mats keep their 70%-opacity labels visible at all times, including
  while held. Labels are never replicated between clients.
- A small set of sample electronics is placed in GroceryStore so FFA/TDM there
  can use some Step 11 content; the full set is showcased in BopBuy.
- BopBuy carries 572 gun props with 28 spawns and 12 original ceiling light
  fixtures; eight-player performance on both maps is still a Step 13 target.
  The project registers 90+ gun/player network prefabs, including the test
  target.
- Test Mode is host-only in both lobbies and applies on the next match. It
  spawns five stationary player-shaped targets. They replicate to clients, take
  damage, die, hide their body/hurtbox, and respawn after three seconds. Their
  attacker kills count toward FFA/TDM score and Gun Game progression; they
  appear in the scoreboard/kill feed, cannot win, and do not count toward the
  eight-player session limit.
- Matches use a load-in gate that waits for connected clients, then runs a
  3-second countdown with movement, firing, and pickups locked.
- Match rules are fixed for now: all modes run a 5-minute timer, TDM ends at
  50 team eliminations, and FFA ends at 25 player eliminations. Gun Game ends
  when a player finishes the ladder, or at time-up by highest ladder level,
  then kills. Ties are draws. Host-configurable rules are planned.
- A results screen shows the winner, end reason, and final score. Movement,
  firing, pickups, and damage are locked while it is visible.
- The host can rematch, which reloads the active map with fresh props, scores,
  health, held items, and projectiles, or return to the session lobby. Clients
  follow both transitions.
- Teams are re-balanced when a rematch reloads the map.
- There is no ranking, objective mode, inventory slot progression, or
  account/profile system.
- One held item per player is the intended alpha inventory model.
- Shelf runs are networked large gun props, and their products are networked,
  individually gun-able props. GroceryStore has 12 shelf runs; BopBuy has 10.
- Nearly all non-structural props are gun-able: checkout/service counters, cash
  wrap, tables, plinths, TV ledges, shelving, registers, belts, lane markers,
  light fixtures, mats, Guysquad counters, signs/banners, carts, EAS pedestals,
  coolers, lockers, workbenches, stools, stanchions, holiday boxes, and
  holiday displays. Only light supports, sign supports, walls/storefront glass,
  floor, and ceiling structure remain static.
- Ceiling light fixtures rest on thin static mounting rails. The visible fixture
  carries its own point light, so picking one up removes its ceiling light and
  firing it creates light-emitting projectile duplicates. Active light
  projectiles are capped at 6 for performance.
- Firing creates a full-size physical duplicate of the held item. The held item
  remains in hand.
- Light and medium bullets spend when they hit a player. Heavy bullets stay
  live, damage each player at most once, and push players they hit.
- Any surface, object, or projectile collision spends a bullet. Bullet-vs-bullet
  collisions spend both. Spent bullets remain physical and become pickable with
  a full magazine once they slow down.
- Large props are no longer anchored. Dropping releases an item at full scale.
  A large prop that would clip the floor or ceiling is released level in front
  of the player; smaller props use the rear-edge-at-muzzle pose. A released
  shelf pushes trapped objects and launches players caught in its volume.
- Falling below the store floor respawns the player at a spawn point without a
  death, health reset, or score change; the held item is kept.
- The Grocery Store map is 72 x 49 m with 5.1 m walls and five connected zones.
  Player, prop, and shelf scale are unchanged. It carries 281 gun items; BopBuy
  carries 572. Eight-player performance is still a Step 13 target.
- Spawn selection is server-side and avoids enemies and direct line of sight.
  Spawned players receive a 1-second blue spawn shield that ends immediately
  when they fire.
- Item models and labels are CC0 content or generated label art; no custom
  animation assets are included.

## Audio and settings

- Pickup, fire, and reload sounds use procedural clips and a reusable pooled
  set of AudioSources.
- Footstep and landing sounds are procedural and played only for the local owner.
- Master volume and sensitivity persist through PlayerPrefs.
- Usernames are limited to 12 characters.

## Verification

- There is no automated gameplay, multiplayer, or performance test suite.
- Multiplayer and load testing are performed manually with the Windows
  development build.
