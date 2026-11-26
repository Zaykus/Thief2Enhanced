# Thief2Enhanced

An external mod menu for **Thief Simulator 2**, loaded through a small native
`UnityPlayer.dll` proxy. No BepInEx, no doorstop, no injector.

The menu is drawn with **Unity IMGUI** (`OnGUI` / `GUILayout`) from a
`MonoBehaviour` host and uses the bundled **Chakra Petch** + **IBM Plex Sans**
fonts.

## Features

| Tab | What it does |
|---|---|
| **Player** | God mode, no fall damage, infinite stamina, never tired, walk/run/crouch/jump/gravity/ghost-speed multipliers, unlimited jumps, no-clip, 8 teleport slots (save/load). |
| **Money** | Infinite money, set money/debt, skill points, special points, level and XP, apply to the save. |
| **Skills** | Auto lockpick, auto pickpocket, auto safe cracking, auto hacking, +10 / max skill points. |
| **Security** | Invisible to NPCs/cameras, no police chase, end current chase, disable all cameras/alarms, freeze guards. |
| **World** | Stop time, game speed, set hour/minute, night/day presets, clear/rain/fog weather. |
| **Items** | Spawner by item name or ID, give all tools, lockpicks x10, full health, live inventory stats. |
| **Vehicles** | Skip car collisions, infinite/refill sleep gas, clear the damage bill. |
| **Misc** | Configurable menu key, reset window size, open log folder, version info. |

The window is resizable (drag the bottom-right grip) and remembers its size.

## Requirements

- Windows 10/11 x64
- Thief Simulator 2 (Unity 2020.3, Mono x64)
- .NET SDK 8 or newer
- ILRepack global tool for merging dependencies:
  `dotnet tool install --global dotnet-ilrepack`
- Visual Studio Build Tools with the **Desktop development with C++** workload
  and a Windows 10/11 SDK (only needed to rebuild the native proxy)

## Install from the release zip (no build tools)

1. Close the game.
2. Extract the zip directly into the game folder (next to
   `Thief Simulator 2.exe`), or anywhere else and pass `-GameRoot`.
3. Run the prebuilt installer:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Thief2Enhanced-v1.0.0\build\install-release.ps1 `
       -GameRoot "C:\Program Files (x86)\Steam\steamapps\common\Thief Simulator 2"
   ```
   `-GameRoot` can be omitted when the extracted folder sits directly in the
   game folder.
4. Start the game and press **F1**. The zip contains the compiled
   `bin\Thief2Enhanced.dll` and `native\UnityPlayer.dll`; no .NET SDK or C++
   compiler is needed. `build\uninstall.ps1` works for both install methods.

## Install from source

1. Close the game.
2. Clone the repository anywhere:
   ```powershell
   git clone https://github.com/Zaykus/Thief2Enhanced.git
   ```
3. Run the installer, pointing it at the game folder:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Thief2Enhanced\build\install.ps1 `
       -GameRoot "C:\Program Files (x86)\Steam\steamapps\common\Thief Simulator 2"
   ```
   If the repository is already inside the game folder as `<game>\Mod`, the
   `-GameRoot` argument can be omitted.
4. Start the game and press **F1**.

### Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File .\Thief2Enhanced\build\uninstall.ps1 `
    -GameRoot "C:\Program Files (x86)\Steam\steamapps\common\Thief Simulator 2"
```

This restores the original engine DLL and removes the mod files.

## How it works

```
Thief Simulator 2.exe
        | imports UnityMain
        v
UnityPlayer.dll            (our native proxy)
        | forwards UnityMain
        v
UnityPlayer_Orig.dll       (the real engine)
        |
        +- worker thread attaches to Mono (mono-2.0-bdwgc.dll)
        +- waits for Assembly-CSharp / GameManager
        +- mono_assembly_open(".../Managed/Thief2Enhanced.dll")
        +- invokes ThiefSimulator2Mod.NativeEntry.Initialize()
                    |
                    +- Harmony patches GameManager.Update
                    +- every frame on the Unity main thread -> ModRuntime.Tick()
                                    |
                                    +- features
                                    +- IMGUI menu (F1)
```

All UnityEngine calls happen on the **main thread**. The native worker thread
only loads the assembly, attaches to Mono and installs patches.

## Configuration

`<game>\Mod\config\mod.cfg` is created on first run:

```ini
menukey=F1
showhint=true
windowwidth=900
defaultmoney=1000000
defaultskillpoints=99
```

| Key | Meaning |
|---|---|
| `menukey` | Any `UnityEngine.KeyCode` name, e.g. `F1`, `Insert`, `F8`. |
| `showhint` | Draw the small hint chip while the menu is closed. |
| `windowwidth` | Initial window width (the last resized size is remembered in `PlayerPrefs`). |

`defaultmoney` and `defaultskillpoints` are accepted for compatibility with
older configs; the menu starts with its built-in values. The speed keys are
likewise ignored — use the sliders instead.

### Saved settings

`<game>\Mod\config\settings.cfg` is written automatically about 1.5 seconds
after you change a safe setting. It stores only:

- movement sliders: `walkspeed`, `runspeed`, `crouchspeed`, `jumpheight`,
  `gravity`, `ghostspeed`
- teleport slots: `slot1=x,y,z` through `slot8=x,y,z` (`empty` when unset)

Window size is saved separately in `PlayerPrefs`. Cheats, money, skills,
inventory, time/weather and vehicle states are intentionally **not** saved.
Delete `settings.cfg` while the game is closed to restore movement defaults
and empty teleport slots.

## Building manually

```powershell
cd native
.\build.ps1

cd ..
.\build\merge.ps1 -GameRoot "C:\Program Files (x86)\Steam\steamapps\common\Thief Simulator 2"
```

`merge.ps1` compiles the managed project and merges Harmony, MonoMod and
Mono.Cecil into `Thief2Enhanced.dll`, so no extra runtime DLLs are shipped.
The `-GameRoot` argument is optional when the repository lives at
`<game>\Mod`; it is required for a standalone clone because the project
compiles against the game's own assemblies. `native\build.ps1` auto-detects the
newest MSVC toolset and Windows SDK.

## Repository layout

```
build/                  install / uninstall / merge scripts
config/mod.cfg          sample configuration
fonts/                  Chakra Petch + IBM Plex Sans (SIL OFL, licenses included)
lib/                    libraries merged into the DLL at build time (Harmony, MonoMod, Mono.Cecil)
native/                 C++ UnityPlayer proxy + build script
src/ThiefSimulator2Mod/
    NativeEntry.cs      called by the native loader
    ModConfig.cs        mod.cfg parser
    Runtime/            main-thread bridge and logger
    Patches/            Harmony patches
    Features/           gameplay features and shared state
    UI/                 IMGUI menu and skin
```

## Notes

- The menu is IMGUI, not uGUI. It is drawn from `ModGuiHost.OnGUI()`.
- Fonts are embedded in the managed DLL, extracted to `%TEMP%` and registered
  privately with `AddFontResourceEx(FR_PRIVATE)` at runtime.
- Harmony/MonoMod are merged into `Thief2Enhanced.dll` at build time, so the
  installed mod needs only the proxy and that one managed DLL.
- This is a single-player mod. Use it offline only.
- **No game files are included in this repository.** The `decompiled/`, `docs/`
  and `tools/` folders used during development are intentionally excluded, as
  is the game's own `Thief Simulator 2.dll`.
- Third-party notices are in [THIRD_PARTY.md](THIRD_PARTY.md).

## Credits

- Mod developer: **Zaykus**
- House quote: *"Any sufficiently advanced technology is indistinguishable from
  a good lockpick."* — Clarke's Third Law, thief edition.

## Troubleshooting

- **Game crashes at startup** - run `build\uninstall.ps1` to restore the engine.
- **Menu does not open** - check `Mod\logs\native-loader.log` and
  `Mod\logs\mod.log`.
- **No logs at all** - the native proxy was not loaded. `UnityPlayer.dll` in the
  game root must be the small proxy and `UnityPlayer_Orig.dll` must exist.
- **Features do nothing on the main menu** - the runtime initializes on the first
  in-game `GameManager.Update()` frame.
