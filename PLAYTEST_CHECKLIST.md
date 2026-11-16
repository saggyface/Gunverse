# GunVerse Prealpha Playtest Checklist

This checklist exists to answer one question: **is GunVerse fun?**
Technical correctness is only the entry gate. A session that runs perfectly but
produces no laughs, no stories, and no desire for another round has failed.

Use this after any step that changes movement, props, combat, the map, or
performance.

> **Pending acceptance:** Steps 9 (match flow), Step 10 (Gun Game), Step 11
> (BopBuy/content revision), and the pre-playtest performance hotfix are
> implemented and waiting for these checks.

## Session setup

Record before starting:

- Build: Windows dev build, or Editor host + build client.
- Players: 8 for the full check, 4 minimum for a quick check.
- Resolution 1080p, VSync off for frame-rate readings.
- Host and clients on separate machines when possible.
- At least one player joins late.
- Hardware notes (CPU/GPU) for the host.

Log locations:

- Host: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Gun Verse\Player.log`
- Each client: same path on the client machine.

## 0. Baseline smoke test (5 min)

Any failure here is a blocker. Stop and report it before judging fun.

- [ ] Host creates a session and clients join with the join code.
- [ ] Pick up, fire, reload, swap, and drop all work.
- [ ] Damage, death, respawn, and scoring work.
- [ ] A late joiner sees correct player, score, and item state.
- [ ] A client disconnect does not break the match.
- [ ] Host departure returns clients cleanly to the main menu.
- [ ] No red errors or exceptions in the log.

## 1. Movement and camera (2 min per player)

Rate 1 (bad) to 5 (great):

- Responsiveness: ____
- Sprint feel: ____
- Jump and landing feel: ____
- Look/sensitivity comfort: ____

Notes:

- Did movement ever fight you or feel floaty?
- Did you ever fight the camera during a fight?
- Any motion discomfort?

## 2. Everything is a gun (10 min)

- [ ] Try to pick up walls, floor, ceiling, and entrance doors. They should
      refuse cleanly and never enter a held state.
- [ ] Pick up at least one of each: small product, medium product, shelf run,
      checkout counter, cash register, light fixture, mat, belt, lane marker,
      electronics gun, Guysquad Counter, BopBuy Sign, and a department sign.
- [ ] Normal item labels are invisible at rest and appear only for the local
      player while hovering the item.
- [ ] Picking up a normal item hides its label; dropping it leaves the label
      hidden until hovered again.
- [ ] Signs and welcome mats show labels at all times, including while held.
- [ ] A host hovering an item does not reveal that label on a client.
- [ ] A loaded shelf can be picked up and its products fall naturally.
- [ ] A shelf is one gun while each product on it stays individually usable.
- [ ] Large props can be grabbed by their nearest edge, not only their center.
- [ ] Held props sit in a readable pose, do not clip into the camera, and do
      not block aiming.
- [ ] Dropped props return to a sensible scale and position and do not fling
      the player or explode the physics scene.
- [ ] No prop is unreachable, duplicated, lost, or impossible to drop.

Notes:

- Which object was the most fun to pick up? Why?
- Which object was awkward in a bad way?
- Did you ever want to pick something up and could not tell why it failed?

## Game modes

- [ ] Host can select FFA, Team Deathmatch, or Gun Game in the lobby.
- [ ] Host can cycle the map between Grocery Store and BopBuy.
- [ ] TDM auto-balances players into Blue and Red; no team switching is required.
- [ ] Blue/Red body colors and the local team indicator are readable.
- [ ] Friendly fire is blocked in TDM.
- [ ] Team scores, scoreboard, and kill feed are correct in TDM.
- [ ] FFA still works and shows no team score/team indicator.
- [ ] The load-in gate waits for connected clients, shows a 3-second countdown, and locks movement, firing, and pickups until it ends.
- [ ] Late joiners spawn correctly during and after the countdown.

## Match flow

- [ ] The HUD timer counts down from 5:00 in both modes.
- [ ] TDM ends immediately when a team reaches 50 eliminations; the results screen names the winning team and shows the final score.
- [ ] FFA ends immediately when a player reaches 25 eliminations; the results screen names the winner.
- [ ] If the timer reaches 0:00 first, the higher score wins and ties show as a draw.
- [ ] Movement, firing, pickups, and damage are locked while the results screen is visible.
- [ ] The cursor is unlocked on the results screen and all host buttons respond to clicks.
- [ ] A client on the results screen sees "Waiting for the host..." and can leave to the menu.
- [ ] Host Rematch reloads the active map with fresh props, scores at zero, full health, no held items, and the 3-second countdown.
- [ ] Host Return to Lobby keeps the same session and join code; clients follow without rejoining.
- [ ] The host can change mode or map in the returned lobby and start another match.
- [ ] A late joiner connecting while the host is in the returned lobby reaches the lobby correctly.
- [ ] No stale scores, held items, kill feed, or results UI remain after a rematch.
- [ ] Host departure during results or from the returned lobby returns clients safely to the main menu.
- [ ] A client leaving during results does not break the host's session.

## Gun Game

- [ ] Host can select Gun Game in the lobby; clients follow the mode.
- [ ] Every player starts with the Soup Can, and the HUD shows 1 / 25 with the weapon name.
- [ ] Each kill advances exactly one ladder step, and the HUD updates to the next weapon.
- [ ] A multi-kill advances once per kill, not once per shot.
- [ ] World pickups and manual drops do nothing in Gun Game; no pickup prompts or hover labels appear.
- [ ] Dying keeps the current ladder level, and respawning grants the current weapon.
- [ ] A late joiner starts at Soup Can and is granted a weapon automatically.
- [ ] The curated ladder includes the original electronics plus Headphones, Monitor, Radio, and Toaster.
- [ ] Guysquad Counter and BopBuy Sign are not in the ladder; they remain pickable in FFA/TDM.
- [ ] Keyboard/Radio/Router spreads and Printer/Toaster/Computer bursts work.
- [ ] Laptop arcing and Battery/Speaker/Large Speaker bounce work.
- [ ] Getting a kill with the final Shelf Run ends the match with LADDER COMPLETE and the correct winner.
- [ ] At 5:00, the highest ladder level wins; ties break by kills, then draw.
- [ ] The results screen shows the top three ladder levels and no stale progression after Rematch.
- [ ] Add a gun to the curated list, register it, run Tools > Gun Verse > Sync Gun Game Ladder, and confirm it appears in the ladder asset.

## BopBuy and new guns

- [ ] Host can cycle Map: Grocery Store / Map: BopBuy in the initial and returned lobbies.
- [ ] The client follows the host into BopBuy with the same join code.
- [ ] BopBuy loads at 78 x 50 m with the original blue/yellow shell, HUD, pause menu, results, join code, 28 spawns, 572 props, and all three modes.
- [ ] Entrance center, checkout left front, Guysquad right front, mobile center, PC right middle, home-theatre/TV wall back, appliances left, and stockroom back-right are readable.
- [ ] The Guysquad room is sectioned off and has its own tables and checkouts.
- [ ] The original blue/yellow shell, entrance vestibule, EAS pedestals, carts, greeter station, and security monitor are present.
- [ ] Every counter, table, plinth, TV ledge, cart, EAS pedestal, locker, cooler, holiday box, and mounted prop can be picked up.
- [ ] Only light supports, sign supports, walls/storefront glass, floor, and ceiling refuse pickup.
- [ ] All TVs use the current TV mesh; the separate Television asset is gone and no TV_2 object remains.
- [ ] Every new electronics/appliance gun has a model, label, correct held scale, fire, reload, and damage.
- [ ] Keyboard/Radio/Router fire spreads; Printer/Toaster/Computer fire bursts.
- [ ] Keyboard all 5 pellets and Printer all 3 burst shots still damage after the first projectile hits a wall, prop, or player.
- [ ] BopBuy starts without shelf overlaps, jitter, or physics explosions; 28 spawns remain clear.
- [ ] Laptop projectiles arc; Battery, Speaker, and Large Speaker bounce noticeably.
- [ ] TV 32/55/65/75 and large appliances feel heavy and respect their projectile caps.
- [ ] Guysquad Counter and BopBuy Sign can be picked up, fired, and dropped.
- [ ] All added signs/banners are pickable and keep their 70%-opacity labels visible; no yellow holiday trees remain.
- [ ] Guysquad Counter hides its label until hovered; the Guysquad room sign stays visible.
- [ ] Normal BopBuy labels are 70% opaque, appear only for the hovering local player, and hide when held.
- [ ] The five sample electronics in GroceryStore are pickable.
- [ ] Host and client agree on BopBuy prop state and late-join state.
- [ ] No missing-prefab, scene, or ownership errors appear in the log.

## Test Mode
- [ ] Host sees `Test Mode: Off/On` in both the initial lobby and returned Lobby; clients do not.
- [ ] Enabling Test Mode and starting a match spawns `TEST 1` through `TEST 5` in the selected map.
- [ ] Targets are stationary, player-shaped, visible to host and client, and do not count toward the 8-player limit.
- [ ] Shooting a target gives hit feedback; it dies, hides, and respawns after 3 seconds.
- [ ] Attacker kills on test targets count in FFA/TDM and advance the Gun Game ladder.
- [ ] Test targets appear in the scoreboard/kill feed and cannot win.
- [ ] Test Mode Off on the next match spawns no targets and normal scoring is unchanged.

## 3. Combat feel (10 min)

- [ ] Light, medium, and heavy props feel clearly different to fire.
- [ ] Shots, hits, and misses are understandable without reading text.
- [ ] Taking damage and its direction are readable.
- [ ] Kills and deaths are clear and quick to understand.
- [ ] Reload motion and sound communicate reload state.
- [ ] Host and client see feedback at comparable times.
- [ ] No missing or delayed feedback on the client.
- [ ] Firing a ridiculous object makes you laugh or react at least once.

Notes:

- Which prop felt best to shoot? Which felt worst?
- Did any effect make the fight harder to read?
- Could you tell what killed you and from where?

## 4. Readable chaos (10 min)

Run this with 4+ players and encourage constant firing and prop throwing.

- [ ] Could you follow the fight most of the time?
- [ ] Could you track your own projectiles?
- [ ] Could you tell teammates, enemies, and scenery apart (once teams exist)?
- [ ] Did any moment feel chaotic in a good way?
- [ ] Did any moment feel noisy, muddy, or unfair?

Notes:

- Describe the best moment of the session.
- Describe the worst moment of the session.
- Did you tell another player about something that happened? What was it?

## 5. Map and flow (10 min)

- [ ] Spawns are safe and no spawn trap occurred.
- [ ] The blue spawn shield appears, protects for about one second, and disappears when firing.
- [ ] A spawn-protected player is not pushed or launched by projectiles.
- [ ] No dead zone where fights never happen.
- [ ] Fights start quickly after respawn.
- [ ] Shelves and counters work as dynamic cover.
- [ ] The five map zones stay readable during a fight.
- [ ] The 72 x 49 m GroceryStore and 78 x 50 m BopBuy do not feel empty or require long empty traversal.
- [ ] BopBuy's dense staggered shelves and 28 spawns keep fights moving; no extremely long sightline, dead department, or collision trap.

Notes:

- Best fight location:
- Worst fight location:
- Any spot you avoided on purpose?

## 6. Performance (10 min, 8 players)

Measure during constant firing, shelf pickups, and product drops.

- [ ] Average FPS at 1080p with 8 players: ____ (target 60+)
- [ ] 1% low FPS: ____ (target 50+ for prealpha)
- [ ] No sustained drop below 50 FPS for longer than 2 seconds.
- [ ] No visible physics explosion or prop jitter.
- [ ] Resting props settle and stop jittering; shelves do not drift or float.
- [ ] Held items show the correct scale on host and client.
- [ ] Retiring projectiles still shrink before despawning.
- [ ] Gunfire, footsteps, reloads, and impacts still play from the expected positions.
- [ ] Host CPU and network remain stable.
- [ ] Projectile activity does not cause visible hitches.

Record the timestamp and situation of the worst frame rate.

## 7. Fun scorecard

Rate 1 (bad) to 5 (great). This is the actual result of the playtest.

- One more round: ____
- "Everything is a gun" is obvious and inviting: ____
- Physical comedy: ____
- Combat readability: ____
- Movement comfort: ____
- Speed of return to action: ____
- Map flow: ____
- Would recommend to a friend: ____

Verbatim comments:

- What made you laugh?
- What frustrated you?
- What did you want to do that the game would not let you do?
- What would you change first?

## Pass criteria

A step passes only when all of these are true:

- Baseline smoke test has no blockers.
- The tester says they want another round without being prompted.
- No single frustration dominates the session.
- Performance target is met, or a documented exception is accepted.
- The new feature from the step is discoverable without reading instructions.

Fail the step if the feature works but the session is not fun. In that case,
fix feel before adding scope.

## Explicitly outside this prealpha

- Gun-able players and player ragdolls.
- Dedicated servers and host migration.
- Public matchmaking and server browser.
- Accounts, progression, rankings, and persistent unlocks.
- Multiple inventory slots.
- Modes other than FFA, Team Deathmatch, and Gun Game.
- Mobile and console support.
- Paid content and monetization.
