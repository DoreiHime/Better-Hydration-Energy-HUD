# Улучшенный HUD воды и голода

[English](README.md) | [简体中文](README.zh-CN.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

HUD-мод для **Escape from Duckov**: показывает, сколько не хватает, и значение **текущее / максимум** рядом с кольцами воды и голода.

> **v2.1.2** — один DLL, **10 языков игры**, **изменяемая горячая клавиша F10**, **без зависимостей**.

## Главное

- **Клавишу можно изменить** — панель открывается на F10 по умолчанию. Если клавиша конфликтует, смените её в **Настройки → Значения HUD**.
- **10 языков игры** — автоматически следует языку игры: английский, 简体中文, 繁體中文, 日本語, 한국어, французский, немецкий, испанский, португальский, русский.
- **Без зависимостей** — ModConfig / ModSetting / HarmonyLib не нужны.
- **Панель F10 в игре** — ставит игру на паузу, мышь работает, настройка позиции / шрифта / формата в реальном времени, автосохранение.
- Сброс для каждого значения, предупреждение о низком значении, проценты / десятичные, нативная вкладка в настройках.

## Установка

1. Подпишитесь в [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771).
2. Включите мод в списке **Mods** в игре.
3. Загрузите сохранение. Нажмите `F10`, чтобы открыть панель.

## Сборка

```powershell
dotnet build src/BetterHydrationEnergyHUD/BetterHydrationEnergyHUD.csproj -c Release
powershell -ExecutionPolicy Bypass -File verify.ps1 -Dll src/BetterHydrationEnergyHUD/bin/Release/BetterHydrationEnergyHUD.dll
```

Проект ссылается на управляемые DLL игры, но не включает их. API, тесты и заметки о выпуске — в `docs/`.

## Ссылки

- Steam Workshop: <https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771>
- Заметки о выпуске: [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md)
- Отчёт о тестировании: [docs/TEST_REPORT.md](docs/TEST_REPORT.md)

## Автор

— Dorei Hime ♥
