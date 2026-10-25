# より良い水分・満腹度HUD

[English](README.md) | [简体中文](README.zh-CN.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

**Escape from Duckov** 用の HUD mod：水分・満腹度リングの横に、不足量と**現在 / 最大**を表示します。

> **v2.1.1** — 1 つの DLL、**ゲームの 10 言語**、**F10 キー変更可能**、**前提 mod 不要**。

## ポイント

- **キー変更可能** — 既定では F10 でパネルを開きます。他の mod と衝突する場合は「設定 → HUD 数値」で変更できます。
- **ゲームの 10 言語** — ゲームの言語に自動追従：英語、简体中文、繁體中文、日本語、한국어、フランス語、ドイツ語、スペイン語、ポルトガル語、ロシア語。
- **前提 mod 不要** — ModConfig / ModSetting / HarmonyLib は不要です。
- **ゲーム内 F10 パネル** — ゲームを一時停止し、マウス使用可。位置 / 文字サイズ / 形式をリアルタイム調整、自動保存。
- 数値ごとのリセット、低値警告、パーセント / 小数、設定画面のネイティブタブ。

## インストール

1. [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771) で購読。
2. ゲームの **Mods** 一覧で有効化。
3. セーブをロードし、`F10` でパネルを開きます。

## ビルド

```powershell
dotnet build src/BetterHydrationEnergyHUD/BetterHydrationEnergyHUD.csproj -c Release
powershell -ExecutionPolicy Bypass -File verify.ps1 -Dll src/BetterHydrationEnergyHUD/bin/Release/BetterHydrationEnergyHUD.dll
```

プロジェクトはゲームのマネージド DLL を参照しますが、同梱はしません。API・テスト・リリースノートは `docs/` を参照。

## リンク

- Steam Workshop: <https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771>
- リリースノート: [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md)
- テストレポート: [docs/TEST_REPORT.md](docs/TEST_REPORT.md)

## 作者

— Dorei Hime ♥
