# 更好的吃喝数值可视化 · Better Hydration & Energy HUD (v2.0)

《逃离鸭科夫》/ Escape from Duckov 的 HUD 增强 mod：在水/饱腹圆环旁显示**差值**与**当前 / 上限**，支持百分比、低值变色、位置/字号微调、可改快捷键与官方设置页。

> 距离上次更新整整一年了……这次带着适配游戏 2.3.30 的 2.0 重写回来了：事件驱动、接近零性能开销，使用游戏原生 TMP 字体与官方设置界面，**零第三方前置**。
> One year since the last update... v2.0 is back as a full rewrite for game 2.3.30: event driven, near-zero overhead, native TMP typography and the official options screen, **no third-party prerequisites**.

Steam 创意工坊：<https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771>

> 作者的话：一年没更新，不知道还有多少鸭宝记得这个小东西。谢谢每一位还在用、还在留言的你——这次回来，想让你看看最初的 mod 现在变成了什么样。♥
>
> A note from the author: a year without updates — I wasn't sure how many ducklings still remember this little thing. Thank you to everyone who kept using it and left a comment. I'm back to show you what the original mod has become. ♥


---

## 功能 / Features

- 圆环上方显示**还差多少**（差值），下方显示**当前 / 上限**（当前值右对齐、斜杠居中、上限左对齐；水分、饱腹各自独立开关）
- 官方设置页：在游戏设置里新增独立「吃喝数值」标签页（键位设置之后），只放两个快捷键；页面样式与原生设置一致
- 游戏内快速面板（默认 **F10**）：完整设置（显示/位置/字号/格式/热键），打开时**冻结游戏并接管鼠标**，可一边看 HUD 一边实时调整；关闭后恢复正常操作，期间输入不会“补放”
- 每个数值旁都有独立「重置」按钮，另有底部「恢复默认」
- 快捷键可在设置中**重新绑定**（解决 F10 和其它 mod 冲突的问题）
- 显示格式：整数 / 1~2 位小数 / 百分比；满值时可隐藏差值
- 低值提醒：数值低于阈值时文字变红（阈值可调）
- 中文（简/繁）/ English 自动跟随游戏语言；单 DLL，不再需要按语言替换文件
- 创意工坊简介已按 **30 种语言**分别本地化（Steam「编辑标题与描述」的语种下拉），文件在 `docs/workshop-localizations.txt`
- 事件驱动：数值变化时才更新文本，**不再每 0.05 秒轮询、不对 UI 反复重建**
- 加载画面不再出现 `0 100/100`（旧版 bug 已修复）
- **零前置**：不需要 ModConfig / ModSetting / HarmonyLib，订阅即用

## 安装 / Install

1. 在 Steam 创意工坊订阅本 mod（ID `3591875771`）。
2. 启动游戏，在 **Mods（模组）** 列表里勾选启用 `BetterHydrationEnergyHUD`。
3. 进入存档即可看到数值；按 `F10` 打开游戏内面板，或在游戏设置界面底部调整。

手动安装：把 `BetterHydrationEnergyHUD` 文件夹放到
`...\Steam\steamapps\common\Escape from Duckov\Duckov_Data\Mods\`（文件夹内含
`BetterHydrationEnergyHUD.dll` + `info.ini`）。

## 兼容性 / Compatibility

- 游戏版本：**2.3.30**（2026-07 补丁，当前最新）实测；1.3.10 及以上应可用（官方设置页从 1.3.10 起提供）。
- 环境：Windows / Unity 2022.3 Mono。
- 不修改游戏资源、不 patch 游戏函数、不注入 Harmony。
- 与其它 HUD 类 mod 共存：只有当你手动把本项目设置改成和其它 mod 一样的位置/热键时才可能视觉重叠——位置、字号、热键都可调。

## 从 1.x 升级 / Upgrading

- 旧的 `settings.ini`（位置、字号、显示开关）**会被自动读取**，无需重新设置。
- 2.0 只发布一个 DLL；旧的 `language/` 文件夹不再需要，可以删除。
- 旧版 F10 打开的是自绘面板；2.0 仍然保留 F10（可在设置里改），同时也把完整设置接入了游戏原生设置界面。

## 设置项 / Settings

| 分组 | 内容 |
|---|---|
| 显示内容 | 水分差值 / 水分当前-上限 / 饱腹差值 / 饱腹当前-上限（4 个开关） |
| 位置微调 | 水分·上方、水分·下方、饱腹·上方、饱腹·下方各自 X / Y 偏移（-300~300 px） |
| 外观与格式 | 字号、小数位数、百分比、满值隐藏差值、低值提醒、提醒阈值、粗体、文字阴影 |
| 快捷键 | 打开设置面板（默认 F10）、显示/隐藏数值（默认未绑定） |
| 关于 | 版本、作者、设置保存位置说明 |

每个数值右侧有独立「重置」按钮；面板底部「恢复默认」会重置全部。

官方设置页（游戏设置 →「吃喝数值」标签页）只放两个快捷键，且与原生设置行同款样式；
其余选项都在游戏内 F10 面板中，打开面板时游戏暂停、鼠标可用，可实时对照 HUD 调整。

设置保存在 mod 目录的 `settings.ini`，可手动编辑；修改后重新打开面板即生效。

## 性能 / Performance

- 数值更新：订阅 `CustomData.OnSetData`（水分/饱腹）与 `Stat.OnSetDirty`（上限），
  游戏约 1 Hz 更新一次数值，因此**每秒最多 2 次事件**；只有整数显示变化时才写 TMP 文本。
- 没有每帧 Update 轮询、没有 10 Hz 的位置跟随：标签直接挂在圆环下，随 HUD 一起缩放/淡出。
- 每秒 1 次的轻量 watchdog 只做空引用/角色切换检查；面板关闭时不做任何事。
- 实机 `ModPerformanceMonitor`（谁偷我帧数？）显示本 mod 帧耗时约 **0.3%~0.5%**（以本机 2.3.30 实测为准）。

## 构建 / Build

需要已安装游戏 + .NET SDK 8/9/10：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
# 可选：直接部署到工坊目录（会先备份原文件）
powershell -ExecutionPolicy Bypass -File .\build.ps1 -DeployWorkshop
```

产物：`dist\BetterHydrationEnergyHUD\`（DLL + info.ini + preview.png），可直接用于本地安装或工坊上传。

- 游戏路径自动探测；也可 `-GamePath "D:\Steam\...\Escape from Duckov"` 或设置环境变量 `DUCKOV_GAME`。
- 构建只引用游戏自带程序集（`Duckov_Data\Managed`），不引入任何第三方 DLL。
- `verify.ps1` 会检查入口类型继承关系、程序集引用白名单（确保零前置）。

## 目录结构 / Layout

```
src/BetterHydrationEnergyHUD/   源码（C#，netstandard2.1）
  Core/                         配置、本地化、HUD 绑定与数值刷新
  UI/                           游戏内快速设置面板（TMP/圆角/动画）
  Integration/                  官方 CustomOptionsPanel 设置页接入
assets/                         info.ini 模板与工坊预览图
docs/                           调研、游戏 API 参考、发布说明
build.ps1 / verify.ps1 / deploy.ps1
```

## 已知限制 / Known limitations

- 官方设置页的布局依赖游戏 2.3.30 的 `OptionsPanel/.../Common/CustomOptions` 结构；如果未来游戏大改设置界面，mod 会自动退回游戏内 F10 面板（不会崩溃）。
- 热键使用新 Input System 的按键名；鼠标侧键等暂不支持绑定。
- 若同时安装多个会改 HUD 布局的 mod，数值位置可能需要用「位置微调」微调。

## 致谢 / Credits

- 游戏：Escape from Duckov by TeamSoda / 碳酸小队
- 1.x 作者与所有反馈问题的鸭宝
- 字体/UI 资源全部来自游戏本体，本 mod 不分发任何游戏资源

## License

MIT（见 [LICENSE](LICENSE)）。
