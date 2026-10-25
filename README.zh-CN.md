# 更好的吃喝数值可视化 · Better Hydration & Energy HUD

[English](README.md) | [简体中文](README.zh-CN.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

《逃离鸭科夫》/ Escape from Duckov 的 HUD 增强 mod：在水/饱腹圆环旁显示**差值**与**当前 / 上限**。

> **v2.1.0** — 单个 DLL，**10 种游戏语言**，**F10 快捷键可改**，**零前置**。

## 重点

- **快捷键可改**：默认 F10 呼出面板；如果和其他 mod 冲突，到「设置 → 吃喝数值」里换键即可。
- **10 种游戏语言**：跟随游戏语言自动切换——简中、繁中、English、日本語、한국어、Français、Deutsch、Español、Português、Русский。
- **零前置**：不需要 ModConfig / ModSetting / HarmonyLib，订阅就能用。
- **游戏内 F10 面板**：打开时游戏暂停、鼠标可用，可实时调位置 / 字号 / 格式，改完自动保存。
- 每个数值独立「重置」、低值变色、百分比 / 小数位、官方设置页原生标签页。

## 安装

1. 在 [Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771) 订阅。
2. 在游戏 **Mods（模组）** 列表里启用。
3. 进入存档，按 `F10` 打开游戏内面板。

## 构建

```powershell
dotnet build src/BetterHydrationEnergyHUD/BetterHydrationEnergyHUD.csproj -c Release
powershell -ExecutionPolicy Bypass -File verify.ps1 -Dll src/BetterHydrationEnergyHUD/bin/Release/BetterHydrationEnergyHUD.dll
```

项目引用游戏本体的托管 DLL，但不打包它们。API、测试与发布记录见 `docs/`。

## 链接

- Steam 创意工坊：<https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771>
- 发布说明：[docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md)
- 测试报告：[docs/TEST_REPORT.md](docs/TEST_REPORT.md)

## 作者

—— Dorei Hime ♥
