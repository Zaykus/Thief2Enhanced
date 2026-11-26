# Thief2Enhanced — Feature Report

**Version 1.0.0** · GUID `dev.thief2enhanced.mod` · Windows x64 · Thief Simulator 2 (Unity 2020.3, Mono)

> **Evidence legend:** **S** = source-verified (read directly from the files audited below). **R** = runtime-verified (observed in `logs/mod.log`, `logs/native-loader.log`, or the in-game visual pass). Defaults and ranges are taken from the code, not inferred from a particular save game.

---

## 1. Summary and how it loads

Thief2Enhanced is a **single-player, external mod menu** for Thief Simulator 2. It has **no BepInEx, no doorstop, no injector and no external loader**. The installer backs up the real engine DLL as `UnityPlayer_Orig.dll` and puts a small native proxy at the game root as `UnityPlayer.dll`.

Load chain (S; confirmed by R logs):

1. `UnityPlayer.dll` exports only `UnityMain` and forwards it to `UnityPlayer_Orig.dll`.
2. On the first `UnityMain` call, one worker thread attaches to `mono-2.0-bdwgc.dll`, sets `THIEF_ENHANCED_ROOT`, preloads the game's engine assemblies (deliberately **not** a second `Assembly-CSharp`), opens `Thief Simulator 2_Data\Managed\Thief2Enhanced.dll`, and invokes `ThiefSimulator2Mod.NativeEntry.Initialize()`.
3. `NativeEntry` installs only three bootstrap Harmony Update hooks. All other gameplay patches are installed later **on the Unity main thread** by `ModRuntime.Tick()`.
4. `ModRuntime` creates the `Thief2Enhanced_MenuHost` MonoBehaviour; `OnGUI()` draws an **IMGUI** menu (`GUI.Window` + `GUILayout`) with embedded **Chakra Petch** and **IBM Plex Sans** fonts. **F1** opens/closes it (rebindable in `config/mod.cfg`); **Escape** also closes it.

The native worker thread never calls Unity APIs. All Unity calls, feature application and GUI drawing happen on the main thread via the Update hook bridge.

---

## 2. Feature matrix

Persistence values: **Runtime** = plain static fields, lost on game restart; **PlayerPrefs** = Unity PlayerPrefs; **Game save** = written through the game's own `SaveMoneyAmount()`, `Save_XP_And_Level()` or `SPrefs.SetInt()`; **config file** = `Mod\config\mod.cfg`.

### Global menu behavior

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Menu key | `F1` toggles the window; Escape closes it when open and the configured key is not Escape | `F1` (configurable to any `UnityEngine.KeyCode`) | config file | Cursor is captured on open and restored on close; re-applied after game Update. |
| Hint chip | Small `"Thief2Enhanced | F1 = menu"` chip while closed | On (`showhint=true`) | config file | Not drawn while the menu is open. |
| Tabs | Eight tabs: Player, Money, Skills, Security, World, Items, Vehicles, Misc; switching resets scroll to top | Player | Runtime | Two-column card grid, one shared scroll view. |
| Window | Resizable by bottom-right grip; clamped to 820×540 min and `Screen-40` max, centered on first open | 900×700 initial width from config | PlayerPrefs (`Thief2Enhanced.MenuWidth` / `.MenuHeight`) | Last resized size overrides `windowwidth` on later runs. |
| Header `X` / drag | `X` closes; top 64 px is the drag area | — | — | Status messages show for 3.5 s; otherwise footer shows `NO CHANGES ACTIVE` / `N CHANGE(S) ACTIVE` from `ModState.ChangedCount()`. |
| Per-card `RESET` | Resets only that card's remembered mod state | — | Runtime | Reset buttons generally **do not** undo already-written game-save values or one-shot actions (noted per tab below). |

### Player

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| God mode (no damage) | Skips `PlayerScript.GotShot`; each frame sets blood fill to 1, clears bleeding, keeps health ≥ 100 | Off | Runtime | Also suppresses fall damage via the same patch condition. Not a blanket invulnerability hook. |
| No fall damage | Prefix-skips `HeadTilt.FeelDamage` | Off | Runtime | Also active implicitly when God mode is on. |
| Infinite stamina | `neverExhaust=true`, stamina set to max, `isExhausted=false`, `canRun=true` each frame | Off | Runtime | Original stamina/canRun restored when disabled. |
| Never tired | Same stamina path as Infinite stamina | Off | Runtime | Functionally overlaps Infinite stamina. |
| Defense `RESET` | Clears the four toggles above | — | Runtime | — |
| Teleport slots 1–8 | `SAVE` stores current `transform.position`; `LOAD` assigns the stored xyz | 8 empty slots | `settings.cfg` | Saved as player progress ~1.5 s after a change and restored on launch. Raw position assignment — no collision/safety check. Controls disabled until a player exists. |
| Teleport `RESET` | Marks all eight slots empty | — | `settings.cfg` | The cleared slots are persisted too. |
| Walk / Run / Crouch speed | Multiplies the captured original motor speed by the slider value | 1.0×, range 0.1–5.0 | `settings.cfg` | Saved ~1.5 s after a change; original values captured once on first frame. |
| Jump height | Scales both `jumpForce` and `originalJumpForce` (which `CalculateMovement()` rebuilds from) | 1.0×, range 0.1–4.0 | `settings.cfg` | Saved ~1.5 s after a change. |
| Gravity | Sets `motor.gravityScale` | 1.0×, range 0–2 | `settings.cfg` | Forced to 0 while No-clip is on; code clamps to 0–2. |
| Ghost speed | Walk/run/crouch speeds are all replaced with this value while No-clip is on | 12 m/s, range 1–30 | `settings.cfg` | Uses `max(0.1, value)`. |
| Unlimited jumps | `canJump=true`, `jumpsInRow=999` each frame | Off | Runtime | — |
| No-clip / ghost | Sets `motor.ghostMode`, zeroes gravity and applies ghost speed | Off | Runtime | — |
| Movement `RESET` | Restores all six sliders and both toggles | — | Runtime | — |
| Live status card | Read-only health, stamina, money/debt, level/skill points, carried weight, item count, sleep gas | Updates each frame | Runtime | Shows `--` before the player spawns; not a cheat. |

### Money

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Infinite money | Each frame raises `moneyAm` to at least 100,000,000; writes `SPrefs["Money"]` once on first enable | Off | Runtime toggle + one Game save write | Raises to a floor, does not lock to an exact amount. Save write failures are swallowed. |
| Money field + `APPLY MONEY` | `moneyAm = value`, then `SaveMoneyAmount()` and `SPrefs.SetInt("Money", …)` | 1,000,000; 0–`int.MaxValue` | Game save | Typing alone changes nothing until `APPLY MONEY`. |
| Debt field + `APPLY DEBT` | `debtAm = value`, then `SaveMoneyAmount()` and `SPrefs.SetInt("Money", …)` | 0; 0–`int.MaxValue` | Game save | Source re-writes the **Money** key here, not a debt key. |
| `CLEAR DEBT` | Sets the field to 0 and runs `APPLY DEBT` | — | Game save | — |
| Cash `RESET` | Restores Infinite money off, Money 1,000,000, Debt 0 in mod state | — | Runtime | Does not rewrite the save; use `APPLY MONEY` / `APPLY DEBT` for that. |
| No item weight | Forces `currentWeight = 0` each frame | Off | Runtime | — |
| Unlimited item uses | Every carried stack with `max_amount > 0` is set to `max_amount` each frame | Off | Runtime | Does not spawn new items. |
| Inventory `RESET` | Clears the two toggles | — | Runtime | — |
| Skill points / Special points / Level / Current XP / XP to next level | Editable fields; `APPLY PROGRESSION` writes all five and calls `Save_XP_And_Level()` | 99 / 99 / 1 / 0 / 0; ranges 0–9999, 0–9999, 1–100, 0–max, 0–max | Game save | `XP to next level` applied as `max(1, value)`. `RefreshXP()` is deliberately not called to avoid unintended level-ups. |
| Progression `RESET` | Restores the five fields in mod state | — | Runtime | Does not rewrite the save. |

### Skills

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Auto lockpick | Prefix on `Lockpick.StartLockpicking`: if learned lockpicking level ≥ difficulty + 2, calls `DoorUnlock()` + `Resett()` and skips the minigame | Off | Runtime | Requires the matching skill level; otherwise the original minigame runs. Failures are logged and fall through. |
| Auto pickpocket | Postfix on `Pickpocket_Manager.Begin_Pickpocket` calls `Success()` | Off | Runtime | Triggers when the minigame starts. |
| Auto safe cracking | Postfix on `SafeMaster.StartCracking` calls `Success()` | Off | Runtime | Triggers when the minigame starts. |
| Auto hacking (panel / ATM) | Prefix on `PlayerScript.HackingPuzzleEnable` calls `Hacking_Complete()`, clears `electronicHack`, re-enables movement/camera controls, disables blur/brightness and skips the panel | Off | Runtime | Failures are logged and fall through to the original puzzle. |
| Auto complete `RESET` | Clears the four toggles | — | Runtime | — |
| `+10 SKILL POINTS` | Adds 10 and immediately applies progression | +10 | Game save | Also writes special points, level and XP currently held in the menu fields. |
| `+10 SPECIAL POINTS` | Adds 10 and immediately applies progression | +10 | Game save | Same caveat. |
| `MAX ALL POINTS` | Sets both point fields to 99 and applies progression | 99 / 99 | Game save | Same caveat. |
| Skill points `RESET` | Restores both fields to 99 in mod state | — | Runtime | Does not apply to the save. |

### Security

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Invisible to NPCs | Each frame sets `isInvisible`, clears `isHitByLight` / `isSeenByNPC`, zeroes `detectDistance` and `addConcernNPC`; originals restored on disable | Off | Runtime | Deliberately does **not** use the global cloak flag for cameras (AI/police also read it). |
| Invisible to cameras | Snapshots and sets `SecurityCamera.isDisabled` and `CameraTrigger.isOn=false`; re-sweeps every 2 s; restores on disable | Off | Runtime | Does not call `DisablePermamently()` (which sets a 999 s game-side disable). New cameras are caught by the sweep. |
| No police chase | While `isPoliceChase`, calls `End_Chase()` then clears `isPoliceChase`, `illegal` and `wasSeenIllegal` | Off | Runtime | — |
| `END CURRENT CHASE` | One-shot: `End_Chase()` + clear the three flags, only if a chase is active | — | Runtime | Does nothing outside an active chase. |
| Detection `RESET` | Clears the three toggles | — | Runtime | Does not re-enable cameras already disabled by the one-shot button. |
| `DISABLE ALL CAMERAS` | Calls `DisablePermamently()` on every `SecurityCamera` found | — | Runtime (game-side permanent flag) | **One-shot and not restored** by the mod or by toggling camera invisibility. |
| `DISABLE ALL ALARMS` | Deactivates every `Alarm` GameObject | — | Runtime | **One-shot and not restored.** |
| Freeze guards | Postfix on `GuardAI.Update` uses reflection to set the guard agent's `speed` to 0 each frame | Off | Runtime | Reflection-based; failures are logged to `mod.log`. |
| Security devices `RESET` | Clears only Freeze guards | — | Runtime | The two one-shot device actions are untouched. |

### World

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Stop time | Saves `EnviroTime.TimeProgressMode` and sets `ProgressTime=None`; restores the saved mode on disable | Off | Runtime | Only applied when the Enviro/GameManager references exist. |
| Game speed | Sets `Time.timeScale` when a change is pending | 1.0×, range 0.1–5.0 | Runtime | Never resumes time while `timeScale <= 0`, so game pause/phone/UI screens are respected. |
| Hour | Feeds `SetInternalTimeOfDay(hour + minute/60)`, wrapped to 24 h | 12, range 0–23 | Runtime | First frame after init does **not** overwrite the time loaded from the save. |
| Minute | Same as Hour | 0, range 0–59 | Runtime | Changes apply on the next frame. |
| `NIGHT 22:00` / `DAY 12:00` | Preset buttons that set Hour/Minute | — | Runtime | The World card `RESET` restores all time controls. |
| `CLEAR WEATHER` / `RAIN` / `FOG` | Verifies the preset exists in `EnviroSkyMgr`'s current list, then `ChangeWeather(name)` | — | Runtime | Shows a status if the weather system or preset is unavailable; exceptions are logged. |

### Items

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Item name / ID + Amount + `GIVE ITEM` | Case-insensitive substring match on carried `item_name`, or exact numeric ID string; adds Amount to each matching stack | `"Lockpick"`, amount 1 (1–999) | Runtime | Adds to **existing carried stacks only** — it does not spawn new objects. Reports the number of stacks updated. |
| Item spawner `RESET` | Restores `"Lockpick"` and amount 1 | — | Runtime | — |
| Item stats card | Read-only item count, weight, sleep gas | — | Runtime | Shows a hint before the player spawns. |
| `ALL TOOLS` | Sets every carried tool stack with `max_amount > 0` to its max | — | Runtime | Does not affect non-tool items. |
| `LOCKPICKS x10` | Adds 10 to the stack with `item_ID == 1` | Hardcoded ID 1 / 10 | Runtime | Assumes ID 1 is lockpicks; no match is a no-op. |
| `FULL HEALTH` | Calls `ps.Heal()`, then sets `bltHealth = 100` | — | Runtime | Menu hint also claims sleep-gas resistance is restored; `Heal()` internals were not inspected. |

### Vehicles

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Skip car collisions | Prefix-skips `CarController.OnCollisionEnter` | Off | Runtime | Hint states it only affects the car being driven. |
| Infinite sleep gas | Each frame sets `ps.gasLeft = 100` | Off | Runtime | This is the player's sleep-gas tank, **not car fuel**. |
| `REFILL SLEEP GAS` | One-shot `gasLeft = 100` | — | Runtime | — |
| `CLEAR DAMAGE BILL` | Zeroes `carDamagesValue`, hides the damage object/text and calls `DisableAlarm()` on every `CarController` | — | Runtime | No save write in source code. |
| Vehicle `RESET` | Clears the two toggles | — | Runtime | Does not re-enable alarms or restore the damage bill. |
| Notes card | Read-only hints about sleep gas, in-game car menu and collision scope | — | — | — |

### Misc

| Control | Exact behavior | Default / range | Persistence | Caveats |
|---|---|---|---|---|
| Menu key hint | Displays the configured key and points to `Mod\config\mod.cfg` | `F1` | config file | Changing the key requires editing the file / restarting. |
| `RESET WINDOW SIZE` | Sets 900×700, recenters, saves the size | 900×700 | PlayerPrefs | Clamped to the current screen size. |
| `CLOSE MENU` | Closes the window | — | — | Same as the header `X` / Escape. |
| `OPEN LOG FOLDER` | Opens `Mod\logs` via `Process.Start` | — | — | Shows `Could not open log folder` on failure. |
| About card | Version, "Native UnityPlayer proxy + Mono attach. No BepInEx loader.", "Single-player use only." | v1.0.0 | — | — |
| Credits card | "Mod developer: Zaykus" plus the thief-edition Clarke's Third Law joke quote | — | — | Two-line quote wrap avoids clipping. |

### Persistence quick reference

| Store | What uses it | Written when |
|---|---|---|
| **Runtime only** | Cheat toggles, money/skill/progression fields, spawner text, time/weather, inventory and vehicle actions, tab/scroll/status state | Immediately in memory; lost on game restart |
| **PlayerPrefs** | Window width/height (`Thief2Enhanced.MenuWidth`, `.MenuHeight`) | On resize mouse-up or `RESET WINDOW SIZE` |
| **Game save / SPrefs** | Infinite money (first enable), `APPLY MONEY`, `APPLY DEBT`, `CLEAR DEBT`, `APPLY PROGRESSION`, the three skill-point buttons | Only from those explicit actions |
| **`settings.cfg`** | Six movement sliders + eight teleport slots | Auto-written ~1.5 s after a change; loaded on startup |
| **config file** | `menukey`, `showhint`, `windowwidth` | Created on first run; otherwise edited by the user |

---

## 3. Architecture and safety notes

- **Main-thread bridge (S, R):** `GameManager.Update`, `PlayerScript.Update` and `MainMenu.Update` have Harmony prefixes calling `ModRuntime.Tick()`. `Tick()` deduplicates by `Time.frameCount` (all three hooks can fire in one frame), then runs features, menu-key handling and the GUI. Update **postfixes** re-apply cursor state because the game rewrites it every frame.
- **Deferred patching (S, R):** `NativeEntry.Initialize()` runs on the Mono worker thread and installs only the three bootstrap Update patches. The eight gameplay patch classes are installed from `ModRuntime` on the main thread, retried every 2 s until complete, and each class is patched at most once to avoid double postfixes.
- **Harmony patches (S, R):** 3 bootstrap methods + 8 gameplay classes — God mode (`PlayerScript.GotShot`), fall damage (`HeadTilt.FeelDamage`), auto lockpick (`Lockpick.StartLockpicking`), auto safe cracking (`SafeMaster.StartCracking`), auto pickpocket (`Pickpocket_Manager.Begin_Pickpocket`), auto hacking (`PlayerScript.HackingPuzzleEnable`), car collisions (`CarController.OnCollisionEnter`) and guard freeze (`GuardAI.Update`). The runtime log confirms `3 methods` and `8 classes`.
- **Game-context caching (S):** Player/manager references are refreshed at most once per second (`GameContext.Refresh`) because objects are recreated on level load. Feature groups registered: Player, Economy, Security, World, Items (5, confirmed in R log).
- **Embedded fonts (S, R):** Six SIL-OFL TTFs are embedded as `Thief2Enhanced.Fonts.*` resources, extracted to `%TEMP%\Thief2EnhancedFonts` and registered privately via `AddFontResourceEx(FR_PRIVATE)`; IMGUI uses `Font.CreateDynamicFontFromOSFont` with fallbacks. Runtime log: `UI fonts: Chakra Petch / IBM Plex Sans`.
- **Merged dependencies (S):** `build/merge.ps1` uses ILRepack with `/internalize /ndebug` to fold Harmony, Mono.Cecil, MonoMod.Utils and MonoMod.RuntimeDetour into the single `Thief2Enhanced.dll`. The installed mod needs only that DLL plus the native proxy. `lib/BepInEx*.dll` files are unused and git-ignored.
- **Source-only repo (S):** `.gitignore` excludes `decompiled/`, `docs/`, `tools/`, the game's own DLL/data, `logs/`, `bin/`/`obj/`, native binaries and BepInEx libs. Tracked files are source, build scripts, the config sample, fonts and their licenses, and the third-party build-time libraries. No game files or compiled mod output are tracked.

---

## 4. Configuration keys (`Mod\config\mod.cfg`)

Keys are parsed case-insensitively; unknown keys are ignored and malformed values fall back to defaults. The file is auto-created on first run.

| Key | Default | Meaning | Status |
|---|---|---|---|
| `menukey` | `F1` | Any `UnityEngine.KeyCode` name (`F1`, `Insert`, `F8`, …) | **Active** |
| `showhint` | `true` | Draw the small hint chip while the menu is closed | **Active** |
| `windowwidth` | `900` | Initial window width; last resized PlayerPrefs size wins on later runs | **Active** |
| `defaultmoney` | `1000000` | Legacy starting-money value | **Ignored** — accepted for compatibility; the menu uses its built-in 1,000,000 |
| `defaultskillpoints` | `99` | Legacy starting-skill-points value | **Ignored** — menu uses built-in 99 |
| `walk_speed`, `run_speed`, `jump_height` | `1`, `6`, `1` | Legacy speed values | **Ignored** — use the in-menu sliders (which are multipliers) |

---

## 5. Known limitations and deliberately NOT persisted

- **Single-player / offline only.** No online or anti-cheat compatibility is intended.
- **Version-coupled.** Patches and field names are tied to this game build's Unity 2020.3 Mono assemblies; a game update can break them. Compile-time game assemblies are required to rebuild (`-GameRoot`).
- **Main-menu startup.** The runtime initializes on the first in-game `GameManager.Update()` frame; features and telemetry do nothing on the title screen (the menu hook itself also covers `MainMenu.Update`).
- **Not persisted:** cheat toggles, money/debt/progression fields, spawner query/amount, time/weather presets, current tab, open/closed state and status messages. They reset every launch. Movement sliders and teleport slots *are* persisted in `settings.cfg`; window size is persisted in `PlayerPrefs`.
- **One-shot actions are not reversible by the menu:** disable-all-cameras, disable-all-alarms, clear damage bill and weather presets. Camera invisibility is the reversible alternative to `DisablePermamently()`.
- **Reset buttons are state-only:** they do not undo writes already committed to the game save or the one-shot actions above.
- **Teleport** is a raw transform assignment (no ground check or collision safety); it can leave the player inside geometry or in the void.
- **Auto lockpick** needs the learned skill level to be at least difficulty + 2; it is not an unconditional bypass. **Freeze guards** uses reflection on private fields and logs errors if the game's internals differ.
- **Item spawner** only augments carried stacks; ID `1` is hardcoded for the lockpick quick-give. **Infinite sleep gas** is the sleep-gas tank, not vehicle fuel.
- **Game speed** will not force time back on while the game's own `timeScale` is 0 (pause/phone/UI). **Stop time** restores the previous Enviro progress mode, including when toggled off deliberately.
- **Logging is best-effort:** log failures never break the game; the native loader log is append-only across runs.

---

## 6. Verification evidence

| Check | Evidence |
|---|---|
| Managed build | **S/R:** Final `build/merge.ps1` run: `dotnet build` 0 warnings / 0 errors, then ILRepack `/internalize /ndebug`. `Thief2Enhanced.dll` is 1,790,464 bytes, SHA256 `21E4673CF8D21BC9BC432ED27D8C8A62385172422ED52EB4BFB867B081B9BA9F`, built 2026-11-26 15:03. Harmony, Mono.Cecil and MonoMod are merged into it; the six OFL fonts remain embedded resources. |
| Native build | **S/R:** `native/build.ps1` rebuilt `native/UnityPlayer.dll` (15,360 bytes, SHA256 `164EADC00CE1FDB2E3B5ADA804894A40F4BD65994CF7719CBE039AAA7F150E91`) with the legacy dependency-preload list removed; runtime log now reads `Engine assemblies preloaded.` with no dependency warnings. |
| Native loader (R) | `logs/native-loader.log` shows repeated `Native UnityPlayer proxy loaded`, `Mono runtime found`, `Mono domain attached`, `Loading managed mod: …Thief2Enhanced.dll`, `Invoke returned: … exception=0000000000000000`, `SUCCESS: managed mod initialized`. Some earlier lines reference the old `ThiefSimulator2Mod.dll` name and an older preload list; those are from earlier proxy builds in the append-only log. |
| Managed runtime (R) | `logs/mod.log` (final session 2026-11-26 15:04) shows `Native loader attached to Mono runtime`, the resolved `Assembly-CSharp.dll` location, `Harmony bootstrap patches installed: 3 methods`, `Loaded saved movement preferences and teleport slots.`, `Update hook fired on main thread`, `Registered 5 feature groups`, `Harmony gameplay patches installed: 8 classes`, `Runtime initialized on main thread. Press F1 for the menu.` and `UI fonts: Chakra Petch / IBM Plex Sans`. **No ERROR lines.** The menu was visually confirmed open on the Misc tab with the Credits card. |
| Visual judge | **R: PASS** — reported by the in-game visual verification pass; screenshots are retained in `C:\Users\OREW\AppData\Local\Temp\opencode\` (e.g. `thief2_final.png`, `thief2_merged6.png`, `menu_shot4.png`). This report author did not re-open the images; treat as reported visual evidence. |
| Repository contents (S) | `git ls-files` contains only source `.cs`/`.csproj`, build scripts, `config/mod.cfg`, fonts + OFL licenses, `FEATURE_REPORT.md` and third-party build-time `lib/*.dll`. `git status --ignored` confirms game DLL/data, `decompiled/`, `docs/`, `tools/`, `logs/`, `bin/`, `obj/`, native binaries and BepInEx DLLs are ignored. Final source edits are committed and pushed to `origin/main` before release. |

---

## 7. Recent additions

- **Safe preference auto-save (`Mod\config\settings.cfg`):** six movement sliders
  (`walkspeed`, `runspeed`, `crouchspeed`, `jumpheight`, `gravity`,
  `ghostspeed`) and all eight teleport slots are written automatically about
  1.5 seconds after a change and restored on the next launch. Values are
  clamped on load (`0–5×` speeds, `0–2×` gravity, ghost speed `1–30`, coordinates
  ±100000). Cheats, money, skills, inventory, time/weather and vehicle states are
  deliberately excluded. Deleting `settings.cfg` while the game is closed
  restores defaults. Verified by a save/reload test: `walkspeed=2.5`,
  `jumpheight=1.75` and `slot1=12.5,-3.25,99.5` all round-tripped exactly.
- **Merged single-DLL build:** Harmony/MonoMod/Mono.Cecil are merged into
  `Thief2Enhanced.dll`; the native proxy no longer tries to preload them.
- **Credits card:** added to the Misc tab.

---

## 8. Credits

- Mod developer: **Zaykus**
- Menu quote (thief-edition parody of Arthur C. Clarke's Third Law):
  *"Any sufficiently advanced technology is indistinguishable from a good
  lockpick."*
- Fonts, Harmony, MonoMod and Mono.Cecil notices remain in [THIRD_PARTY.md](THIRD_PARTY.md).
