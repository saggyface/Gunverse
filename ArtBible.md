# GunVerse Art Bible

## Style statements

- **GunVerse UI** — Arcade field-console UI: near-black panels, warm cream type, acid-lime primary, cyan support, magenta danger.
- **BopBuy** — Bright big-box electronics retail: cobalt and safety yellow over crisp white and cool concrete.
- **GroceryStore** — Warm neighborhood grocery: cream walls, produce green, tomato red, bread brown, soft daylight.

UI and maps use separate palettes. UI colors never appear in map shell or fixtures, and map colors never appear in UI. Product models, imported assets, labels, and merchandise keep their native colors. Team colors stay semantic and may override the palette for readability.

## GunVerse UI palette

| Role | Hex | Use |
| --- | --- | --- |
| Ink | `#0E1319` | Canvas backdrop, scoreboard/results overlay |
| Surface | `#1B2430` | Panels and raised controls |
| Cream | `#F4F0E6` | Primary text, crosshair, prompts |
| Primary | `#C6FF3D` | Primary buttons, selected states, kill counts, held item |
| Secondary | `#32D5E6` | Secondary buttons, slider fill, hit marker |
| Danger | `#FF4F7B` | Quit/leave/confirm, health, death counts, kill marker |
| Accent | `#FFB12E` | Timers, status labels, level-up notification |

Muted text is Cream at 70% alpha. Results and scoreboard backdrops are Ink at 85–95% alpha. Team labels and team-rich text keep the functional team colors.

## BopBuy palette

| Role | Hex | Use |
| --- | --- | --- |
| Floor | `#3A4048` | Store floor and dark neutrals |
| Wall | `#F2F0EA` | Walls, trim, shelf backs |
| Ceiling | `#D9DDE2` | Ceiling and light structure |
| Blue | `#1F4FA8` | Primary fixtures, counters, sign panels |
| Yellow | `#F4C542` | Accent fixtures, trim, lane markers |
| Metal | `#5A626D` | Shelving, supports, hardware |
| Accent | `#E44D3A` | Danger/clearance accents |

Mood: bright high-key retail lighting, cool white key light, blue/yellow department identity, short readable sightlines. `StoreFogController` uses a fog color derived from this palette.

## GroceryStore palette

| Role | Hex | Use |
| --- | --- | --- |
| Floor | `#6B6258` | Floor and warm neutrals |
| Wall | `#EFE6D4` | Walls, shelf backs, trim |
| Ceiling | `#DCD5C8` | Ceiling |
| Green | `#2F6B4F` | Checkout, produce floor |
| Tomato | `#C94A3A` | Warm accent fixtures |
| Wood/Bread | `#9A7B57` | Warm fixture accents |
| Accent | `#E0A62E` | Lighting and small accents |

Mood: soft warm daylight, cream walls, green checkout/produce identity, muted metal shelving. `StoreFogController` uses a fog color derived from this palette.

## Fixtures, prefabs, and projectiles

Map-specific fixtures that would otherwise share a prefab are duplicated as map palette variants. BopBuy scene instances use `_BopBuy` fixture prefabs so the held prop and its fired projectile clone both use BopBuy colors. GroceryStore uses the shared prefabs and the GroceryStore palette. After any full BopBuy rebuild, run `Tools > Gun Verse > Art > Apply BopBuy Palette` before building.

Gun Game grants the generic curated weapon prefabs. Those generic weapons keep their shared materials even inside a map; only props placed in the map use that map's fixture variants.
