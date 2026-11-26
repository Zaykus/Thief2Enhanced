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
- Visual Studio Build Tools with the **Desktop development with C++** workload
  and a Windows 10/11 SDK (only needed to rebuild the native proxy)

## Install

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
| `defaultmoney` | Starting value of the money field. |
| `defaultskillpoints` | Starting value of the skill point fields. |

## Building manually

```powershell
cd native
.\build.ps1

cd ..\src\ThiefSimulator2Mod
dotnet build -c Release
```

`native\build.ps1` auto-detects the newest MSVC toolset and Windows SDK.

## Repository layout

```
build/                  install / uninstall scripts
config/mod.cfg          sample configuration
fonts/                  Chakra Petch + IBM Plex Sans (SIL OFL, licenses included)
lib/                    runtime libraries (Harmony, MonoMod, Mono.Cecil)
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
- This is a single-player mod. Use it offline only.
- **No game files are included in this repository.** The `decompiled/`, `docs/`
  and `tools/` folders used during development are intentionally excluded, as
  is the game's own `Thief Simulator 2.dll`.
- Third-party notices are in [THIRD_PARTY.md](THIRD_PARTY.md).

## Troubleshooting

- **Game crashes at startup** - run `build\uninstall.ps1` to restore the engine.
- **Menu does not open** - check `Mod\logs\native-loader.log` and
  `Mod\logs\mod.log`.
- **No logs at all** - the native proxy was not loaded. `UnityPlayer.dll` in the
  game root must be the small proxy and `UnityPlayer_Orig.dll` must exist.
- **Features do nothing on the main menu** - the runtime initializes on the first
  in-game `GameManager.Update()` frame.
