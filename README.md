# Better Hydration & Energy HUD

[English](README.md) | [简体中文](README.zh-CN.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

HUD mod for **Escape from Duckov**: shows how much is missing and the **current / max** value next to the hydration and hunger rings.

> **v2.1.0** — one DLL, **10 game languages**, **rebindable F10 hotkey**, **zero prerequisites**.

## Highlights

- **Rebindable hotkey** — the panel opens with F10 by default. If it conflicts with another mod, change it in **Settings → HUD Values**.
- **10 game languages** — follows the game language automatically: English, 简体中文, 繁體中文, 日本語, 한국어, Français, Deutsch, Español, Português, Русский.
- **Zero prerequisites** — no ModConfig / ModSetting / HarmonyLib. Subscribe and play.
- **In-game F10 panel** — pauses the game while open, mouse stays usable, live tuning of position / font size / format; saves automatically.
- Per-value reset buttons, low-value warning, percentage / decimals, native options tab.

## Install

1. Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771).
2. Enable the mod in the game's **Mods** list.
3. Load a save. Press `F10` for the in-game panel.

## Build

```powershell
dotnet build src/BetterHydrationEnergyHUD/BetterHydrationEnergyHUD.csproj -c Release
powershell -ExecutionPolicy Bypass -File verify.ps1 -Dll src/BetterHydrationEnergyHUD/bin/Release/BetterHydrationEnergyHUD.dll
```

The project references the game's managed DLLs and does not bundle them. See `docs/` for the API notes, test report and release notes.

## Links

- Steam Workshop: <https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771>
- Release notes: [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md)
- Test report: [docs/TEST_REPORT.md](docs/TEST_REPORT.md)

## Author

— Dorei Hime ♥
