# Z2Randomizer Architecture

**Contents**

- [1. Frontends](#1-frontends)
- [2. High-level data flow](#2-high-level-data-flow)
- [3. Core types](#3-core-types)
- [4. The js65 assembler integration](#4-the-js65-assembler-integration)
  - [4.1 How it is wired in](#41-how-it-is-wired-in)
  - [4.2 Segments and free space](#42-segments-and-free-space)
- [5. Config, flags, presets, player options](#5-config-flags-presets-player-options)
- [6. The generation pipeline](#6-the-generation-pipeline-hyrulecs)
- [7. Overworld subsystem](#7-overworld-subsystem-randomizercoreoverworld)
- [8. Palace subsystem](#8-palace-subsystem-randomizercoresidescrollpalace)
  - [8.1 Generators](#81-generators)
- [9. Enemies](#9-enemies-randomizercoreenemy)
- [10. Items, drops, stats](#10-items-drops-stats)
  - [10.1 Town quest items and starting spell items](#101-town-quest-items-and-starting-spell-items)
- [11. Texts & hints](#11-texts--hints)
- [12. Important ASM patches](#12-important-asm-patches)
  - [12.1 MMC5 scanline IRQ scroll split](#121-mmc5-scanline-irq-scroll-split)
- [13. Custom music](#13-custom-music-randomizercoremusiccs--ftrandolib)
- [14. Custom sprites](#14-custom-sprites)
- [15. Technical notes / Gotchas](#15-technical-notes--gotchas)
- [16. Useful related projects](#16-useful-related-projects)

---

## 1. Frontends

- `CrossPlatformUI.Desktop` — Windows (`Z2Randomizer.exe`)/Linux/macOS
- `CrossPlatformUI.Browser` — WebAssembly
- `CommandLine` — `Z2R.exe`
- Various tools: `ValidateRooms`, `DumpRooms`, `Statistics`.

The UI is built using Avalonia and ReactiveUI. See `UI.md` for more information.

## 2. High-level data flow

```
vanilla .nes  ── ROM.ValidateVanillaRom ──►  ROM.rawdata (expanded in place)
                                                │  byte edits (Put/GetByte/GetShort…)
                                                ▼
                C# shuffled state (World/Location/Palace/Enemy/RandomizerProperties)
                                                │
                  Hyrule.ApplyAsmPatches ──► asm.Module().Code(...)  (embedded Asm/*.s + ROM.cs snippets)
                                                │
                          Hyrule.Randomize: ROMData.ApplyAsm(assembler)  = js65 link (AT GENERATION TIME)
                                                ▼
                                   Js65CompileResult { romdata, debugfile, messages }
                                                │  optional FtRandoLib music import (post-link)
                                                ▼
                       UpdateRom() writes location bytes back; seed hash at 0x17C2C
                                                ▼
                                     Z2_<seed>_<flags>.nes  (+ .mlb debug file)
```

ROM changes come from three places: (a) `ROM.Put*` writes to known offsets,
(b) inline 6502 modules in `ROM.cs`, and (c) whole `.s` files embedded as
resources. See sections 4 and 12 below.

## 3. Core types

- `ROM` (`RandomizerCore/ROM.cs`) — the mutable byte model.
  - `rawdata` (bytes). Vanilla in: 16-byte iNES header + `0x20000` PRG +
    `0x20000` CHR (`VanillaRomSize`). Z2R expands PRG to `0x40000`
    (`PrgRomSize`); `FreeRomBanks` = `0x10..0x1B` (extended banks not yet
    carrying data).
  - Byte access: `Put(index, data)`, `PutShort`, `GetByte(s)`, `GetShort` —
    indices are **ROM file offsets** (header included), not bank addresses.
  - Patch hosting: `ApplyAsm(Assembler)` returns `Js65CompileResult` (ROM bytes
    + optional `.mlb` debug text + messages); `RelocateData` moves a
    bank-relative table; per-feature `public void Foo(Assembler)` methods build
    `asm.Module().Code(...)` inline strings.
- `Hyrule` (`RandomizerCore/Hyrule.cs`) — the orchestrator. Holds `ROMData`,
  `r` (seeded `Random`), `props` (`RandomizerProperties`), worlds/palaces,
  `randomizedStats`, `randomizedDrops`. `Randomize(vanillaRomData, config,
  progress, ct)` is the single entry point for both the CLI and the GUI.
- `RandomizerResult(bool success, byte[]? romdata, string? debuginfo, string?
  messages)` — return contract.
- `Assembler` / `AsmModule` — **js65.interop** types (see below);
  `NewAssemblerFn` delegate lets each frontend supply its own host.

## 4. The js65 assembler integration

### 4.1 How it is wired in

- js65 is a JavaScript implementation of the ca65 assembler and linker (package
  `js65.interop`, version pinned in `Directory.Packages.props`). It does not run
  at build time: the `Assembler` is created at generation time
  (`Hyrule.CreateAssemblyEngine()`, Hyrule.cs:559), and `ROMData.ApplyAsm` links
  once all patches are queued.
- Every `Asm/*.s`, `ram.inc`, `z2r.inc`, `.chr` and `z2mario/*` file is an
  embedded resource (`RandomizerCore.csproj`), loaded with
  `Util.ReadResource("Z2Randomizer.RandomizerCore.Asm.*")`. The callback
  `AsmFileResolveTextCallback` maps `.include "x"` paths to those resources.
- Hosts: desktop uses `Desktop.Common/DesktopJsEngine.cs` (ClearScript V8); the
  browser uses `js65.browser`. Both implement the same `Apply(byte[] rom)` /
  `Js65CompileResult` interface. `Js65Options` (debug info is on in Debug
  builds) live in `Hyrule.cs`.
- Big rewrites are `.s` resources. Small patches are inline strings in `ROM.cs`.
- Each `asm.Module()` is compiled as a unit, and a symbol must be `.import`ed if
  they are to be used across modules.

### 4.2 Segments and free space

- `Init.s` is injected first as `__init.s`. It declares every segment:
  `HEADER`, PRG0–PRG7 (16 KB each, `$8000`/fixed `$C000` semantics preserved),
  extended banks PRG10–PRG1F (8 KB each; PRG1C/1D are reserved for palace
  sideview data) and `CHR`. It also declares the linker `FREE` ranges: vanilla
  padding where `.reloc` code may land.
- The declared ranges are not the whole map. Patches that shrink, replace or
  abandon vanilla code leave holes the linker also uses:
  - Orphaned routines: `FREE_UNTIL <end>` after an `.org` trampoline hands the
    dead body back to the linker.
  - Abandoned data: `FullItemShuffle.s` re-points the vanilla item/enemy tile
    tables and claims `$EE51-$EEB2` (`FREE_UNTIL $eeb2`).
- The fixed bank (PRG7) is more valuable because it's always available without
  bank switching. Try to place patches in the other banks if possible.

## 5. Config, flags, presets, player options

- `RandomizerConfiguration` (`RandomizerCore/RandomizerConfiguration.cs`) is the
  full user-facing setting surface. It round-trips to the **flag string**
  (`SerializeFlags`/`DeserializeFlags`, `Flags/FlagBuilder.cs` +
  `Flags/FlagReader.cs`). `CheckForFlagConflicts()` validates combinations.
- `RandomizerProperties` (`RandomizerProperties.cs`) is the resolved,
  RNG-committed snapshot `config.Export(r, ...)` produces; most of the pipeline
  reads it. Attribute `DifficultyOnlyAttribute` drives
  `shareSeedAcrossDifficulty` handling.
- Presets (`CrossPlatformUI/Presets/*.cs`) are curated sets of config options.
- Player options (`Desktop.Common/PlayerOptionsService`) handles player
  preference options (accessibility, quality-of-life, custom sprites and music
  options) that are not part of the flags.
- Seed chain: seed string -> `MD5` -> `SeedHash` int -> `Random(SeedHash)`;
  `GenerateSpoiler` spins the RNG 64 bytes so spoiler/non-spoiler runs differ.

## 6. The generation pipeline (Hyrule.cs)

Order matters — later stages depend on earlier ones (see gotchas). Steps:

1. **Setup**: seed/RNG, `Export` props, `CheckForFlagConflicts`, create the
   assembler, empty `ItemGet` map.
2. **Validate + copy the ROM** (`ROM.ValidateVanillaRom`); work on a fresh copy
   to avoid seed bleed.
3. **Palace generation loop** (`GENERATING_PALACES`): `Palaces.CreatePalaces`
   builds 7 palaces from a generator per palace style; randomizes palace enemies
   and small items; `FillPalaceRooms(sideviewModule)` writes the sideview layout
   into extended-bank asm; loop until a layout validates.
4. **Byte-level housekeeping**: `WriteKasutoJarAmount`, `DoHackyFixes`,
   `AdjustGpProjectileDamage`, drops (`DropRandomizer`), stat randomization
   (`StatRandomizer`), `DisableTurningPalacesToStone`, `UpdateMapPointers`.
5. **Overworld** (`ProcessOverworld`): generate West/East/DM/MI continents,
   terrain, locations; reachability loop; `RemoveUnusedConnectors`,
   `PlaceHiddenLocations`, `UpdateKasuto`.
6. **Enemies**: overworld enemy shuffle.
7. `CombineFireSpell` if linked.
8. **Text/hints**: `CustomTexts.GenerateTexts` (+ `Spoiler`),
   `randomizedStats.Randomize` (+ difficulty-only re-roll under
   `shareSeedAcrossDifficulty`).
9. **Patches**: `ApplyAsmPatches(props, assembler, ...)` — **exactly once**, per
   run — then `ROMData.ApplyAsm(assembler)`.
10. **Music** (post-linker, `randomizeMusic`): `MusicRandomizer.ImportSongs`
    packs imported/random tracks into free banks; retries on `RomFullException`.
11. **Finish**: `UpdateRom()` writes location bytes / heart-magic tile copies
    back to `ROMData`; seed hash computed and written at `0x17C2C`.

Progress is reported via `ProgressEnum`.

## 7. Overworld subsystem (`RandomizerCore/Overworld`)

- `World` (abstract; `WestHyrule`, `EastHyrule`, `DeathMountain`, `MazeIsland`)
  each load their location table from the vanilla ROM (`LoadLocations`) and
  re-generate terrain maps (for non-vanilla Biomes).
- `Location` — an overworld location that represents anywhere you can go into a
  sideview from the Overworld.
- `UpdateRom` writes each `Location`'s data bytes back.

## 8. Palace subsystem (`RandomizerCore/Sidescroll/Palace`)

- `Palace`, `Room`, `RoomGroup` and `PalaceRooms` are the data models.
- The room pool is in `PalaceRooms.json` (vanilla plus community-made rooms).
  Every frontend loads it before `Hyrule` is constructed.
- `RoomPool` handles pool querying and validation.
- Palace color palettes live in `PalaceColors.cs`.

### 8.1 Generators

All generators subclass `PalaceGenerator` (some via `CoordinatePalaceGenerator`).

Rooms are placed from the pool and wired together through sideview pointer
tables (`RomMap.SIDEVIEW_PTR_TABLE_P125 = 0x10533`, `P346 = 0x12010`,
`GP = 0x14533`). A layout must pass reachability checks (all rooms reachable,
boss and item rooms placed sensibly). `FillPalaceRooms` then turns the placement
into extended-bank sideview data via asm, and the loop in `Randomize` retries
generators until one validates.

## 9. Enemies (`RandomizerCore/Enemy`)

- `Enemies.cs` — enemy tables (HP, stats, drops) at vanilla addresses;
  `EnemiesEditable.cs` — mutate-and-write model.
- `PalaceEnemyShuffler` / `OverworldEnemyShuffler` — swap enemy IDs per slot
  class (small/large/flying/generator), with `MixLargeAndSmallEnemies` and
  `GeneratorsAlwaysMatch` options.
- Overworld shuffle relocates a PRG1 background map to make room for more enemy
  data.

## 10. Items, drops, stats

- `StatRandomizer` — Attack/Life/Magic values per level, HP changes, OHKO,
  dripper HP, `FixRebonackHorseKillBug`.
- `DropRandomizer` — small/large drop tables, P-bag XP table, boss-drop
  collectable, drop frequency.
- `Collectable` — item identity model used by shuffle logic.

### 10.1 Town quest items and starting spell items

- The five town quest collectables are fixed: Ruto → Trophy, Saria → Mirror,
  Mido → Medicine, Nabooru → Water, and Darunia → Child. `StartWithSpellItems`
  starts with all five.

## 11. Texts & hints

- `CustomTexts.cs` — full dialog rewrite: generic NPC texts, helpful hint
  generation (`GenerateHelpfulHints`). `CharMap` in `ROM.cs` maps game-text
  chars; `Text.cs` wraps encode/decode.
- `Spoiler.cs` renders a spoiler.

## 12. Important ASM patches

| File | What it does |
| --- | --- |
| `Init.s` | Segments and `FREE` ranges |
| `MMC5.s` | MMC1 to MMC5 conversion: 8 KB PRG pairs (`$5114/$5115`), CHR mode 3, SRAM bank shadows (`$6340`), scanline IRQ split, redefined `SwapPRG`/`SwapCHR` |
| `FullItemShuffle.s` | Adds the ability for items normally received from dialogs to be shuffled |
| `Flute.s` | Flute warping like in Z1 |
| `StatTracking.s` | Stat tracking in SRAM |
| `ExpandedPauseMenu.s` | Pause menu that shows two rows of items |
| `z2ft.s` | Custom music-driver wiring: hooks PRG7 `UpdateSound = $878c` and swaps to bank 6 |
| `z2mario/*` | Mario mode |

Many patches also exist as inline methods in `Hyrule.cs` and `ROM.cs`.

### 12.1 MMC5 scanline IRQ scroll split

The side-view needs two scrolls in one frame: scanlines 0-30 use the HUD/status
scroll, scanlines 31+ use the playfield scroll. Vanilla Z2 waits for sprite 0;
Z2R got rid of sprite 0, so `MMC5.s` replaces every sprite-zero wait with a
**scanline (line) IRQ** on the MMC5's `$5203` target / `$5204` enable registers.

## 13. Custom music (`RandomizerCore/Music.cs` + `FtRandoLib`)

- `z2rndft.ips` is a prebuilt IPS patch (applied with `ApplyIps`, only when music
  is on) that rewrites the compiled driver's stale `NmiBankShadow8/A` references
  `$6380/$6381` to `$6340/$6341`.
- `FtRandoLib` is a **git submodule** (`TheRealQuantam/FtRandoLib`, net8.0):
  FTM/DNM import support. `MusicRandomizer` (in `Music.cs`) collects
  `Music/*.json5` and `Music/*.yaml`, packs them into free banks
  (`MusicRandomizer.FreeBanks`), and writes the result back into `ROMData`.

## 14. Custom sprites

- `CharacterSprite` images are shipped as `Sprites/*.ips` (applied with
  `ApplyIps`).

## 15. Technical notes / Gotchas

- **ROM offsets vs bank addresses.** `ROM.Put*` indices are file offsets (16
  byte iNES header included). `.org` and `FREE` addresses in asm are bank
  addresses (`$8000+`, fixed `$C000`).
- **js65 only runs on generation.** `dotnet build` validates C# only. ASM syntax
  and linker placement are checked only when a base ROM is passed and a seed is
  generated with flags that includes an ASM patch.
- **Generation is byte-reproducible** for a fixed commit, flags, seed and
  default customization options. This can be used to A/B test new changes.
- Adding (or modifying the length of) ASM patches that use `.reloc` will likely
  re-order where other ASM patches are placed. This can make A/B testing
  difficult and save states unloadable.
- **Larger patches belong in `.s` files.** If an inline `ROM.cs` patch grows
  bigger, move it out to a `.s` file. However, be aware that **Visual Studio can
  be bad about adding updated `.s` files to builds**.
- Adding/removing anything in the seed generation that draws from the RNG will
  likely generate a completely different seed. This can make A/B testing
  difficult and save states unloadable. Saved games used before/after such a
  change may not even be beatable — starting loadout or items pickup are not
  expected to match what's in-logic.

## 16. Useful related projects

- `z2disassembly` is a label-accurate ca65/ld65 disassembly of vanilla Zelda II
  (byte-exact via `make check`). Treat it as ground truth for any vanilla 6502
  routine an assembly patch hooks. Note: our `Asm/ram.inc` is more up-to-date
  for RAM values.
- `z2edit` is an ImGui/C++ ROM editor that reads Z2 ROMs — now including palace
  sideview data in extended banks `1C`/`1D`.
