# Release Notes

## v2.1.2 — 2026-10-27（与其他 mod 的兼容修复）

### 修复 / Fixed
- 某些旧 mod 会在 `LevelManager.OnAfterLevelInitialized` 回调中抛异常（例如「近战cd显示」的 `ActionProgressHUD.parentCanvasGroup` 字段在 2.3.x 已改名为 `SliderCanvasGroup`，触发 `MissingFieldException`）。游戏虽捕获了异常，但 `LevelManager.AfterInit` 永远不会置 true，导致本 mod 的 HUD 一直不绑定、数值不显示。
- 现在 HUD 增加兼容兜底：`LevelInited` 已为 true、不在加载场景、且主角色存在时，即使 `AfterInit` 为 false 也照常绑定；已实测两个 mod 同时启用时数值正常显示。
- 该修复不改变「近战cd显示」本身：它仍然因字段过期而无法工作，需要作者更新。

### 验证 / Verification
- 实机：同时启用 BetterHydrationEnergyHUD v2.1.2 与 近战cd显示，日志中近战 mod 抛完 `MissingFieldException` 之后，本 mod 仍输出 `[BHE] HUD bound in level`，HUD 正常显示（作者确认）。

## v2.1.1 — 2026-10-26（语言热切换修复）

### 修复 / Fixed
- 切换游戏语言后，F10 面板的**标题、版本行、底部「恢复默认」「更改会自动保存」「关闭」** 需要重启游戏才刷新；现在不重启也会即时跟随游戏语言变化。
- 工坊简介版本号同步到 v2.1.1。

### 验证 / Verification
- 实机：中文启动 → 不重启切到俄语/日语 → 按 F10，标题与底部控件立即变为对应语言（作者确认）。

## v2.1.0 — 2026-10-26（10 种游戏语言）

### 新增 / Added
- **mod UI 扩展到游戏支持的 10 种语言**：English / 简体中文 / 繁體中文 / 日本語 / 한국어 / Français / Deutsch / Español / Português / Русский（跟随游戏语言，单 DLL）。
- **F10 面板把「快捷键」移到最上方**：改键入口一眼可见；官方设置标签页保持只放快捷键。
- **工坊简介 30 语种排版重排**：列表内去掉多余空行、重点功能加粗置顶——快捷键可改 / 10 种语言 / 零前置。

### 说明 / Notes
- 游戏本体只提供 10 种语言（StreamingAssets/Localization 下 10 个 CSV），因此 mod UI 不扩展到 Steam 工坊的 30 种语言；工坊简介仍保留 30 语种。
- 版本号 2.1.0；DLL 由 89,600 bytes 增至 111,616 bytes（新增 7 套翻译表）。

## v2.0.0 — 2026-10-26（重写版 / Rewrite）

面向《逃离鸭科夫》2.3.30 的完全重写。旧版 1.x 的 settings.ini 布局配置可无缝沿用。

### 重写 / Rewrite
- **HUD 改为事件驱动**：订阅 `CustomData.OnSetData`（水分 / 饱腹）与 `Stat.OnSetDirty`（上限），
  删除旧版 0.05 s 协程轮询、10 Hz 位置跟随、0.2 s 强制 Layout 重建。
- **标签改为游戏 HUD 的子节点**：直接挂到 Water / Energy 圆环下，克隆 `HealthHUD` 的 TMP 数字
  （7segment SDF 字体），随 HUD 自动缩放、淡出、隐藏；下方数值改为三段式（当前值右对齐、
  斜杠居中、上限左对齐），不依赖字体空格宽度。
- **不再自建 Canvas（旧版 sortingOrder 9999 嵌套 Canvas 会盖住加载幕布）**：
  同时用 `LevelManager.LevelInited && AfterInit && !SceneLoader.IsSceneLoading` 门控，
  修复进出关卡时黑屏出现 `0 100/100` 的问题。
- **单 DLL 多语言**：跟随游戏语言（简中 / 繁中 / English），删除 1.x 的 `language/` 目录。
- **零前置**：不依赖 ModConfig / ModSetting / HarmonyLib（`verify.ps1` 强制校验程序集引用白名单）。

### 设置界面 / Settings
- **官方设置页新增独立标签页**：通过 `CustomOptionsPanel.OnPanelEnabled` 拿到 `OptionsPanel` 后，
  在「键位设置」后面追加一个「吃喝数值」标签页；页内 **克隆游戏原生设置行**（原生底图 / 字体 /
  布局），只把右侧下拉框改造成点击改键的快捷键设置。标签页只包含两个快捷键，其余设置都在
  游戏内 F10 面板中。
- **游戏内面板（F10）重写为稳健实现**：Unity 原生 Slider / Button、显式锚点布局、游戏原生
  TMP 字体（ResourceHanRoundedCN-Medium SDF）、滚动 + 淡入滑入动画。
- **打开面板时接管鼠标**：解锁并显示光标（每帧 LateUpdate 覆盖），冻结游戏时间，
  解决“游戏进行中鼠标被锁、无法操作面板”的问题。
- **接入游戏官方输入拦截**：`InputManager.DisableInput(panel)` / `ActiveInput(panel)`，
  面板期间的点击/按键不会被角色输入系统记录，关闭面板后不会“补开枪 / 补移动”。
- **每个数值都有独立「重置」按钮**（不再只能全部重置）；底部保留「恢复默认」。
- **快捷键可重绑**：打开面板键（默认 F10）、显示/隐藏数值键（默认未绑定），解决 F10 冲突。
- 新增：小数位数、百分比、满值隐藏差值、低值阈值变色、粗体、文字阴影。

### 兼容 / Compatibility
- 适配游戏 **2.3.30**（实测：进档 HUD、F10 面板、原生设置标签页、改键、输入拦截均正常）。
- 旧 settings.ini 的 13 个键保持兼容；新增键有安全默认值。
- 停用 mod 时清理标签、取消订阅、销毁面板与注入的标签页。

### 验证 / Verification
- 构建：`dotnet build` 0 error / 0 warning；`verify.ps1` 通过（入口类型、继承关系、程序集白名单）。
- 实机：HUD 数值/位置正常；F10 面板可鼠标操作且时停；输入不再回放；官方设置标签页不卡死、
  可改键；ModPerformanceMonitor 实测本 mod 帧耗时约 0.3%~0.5%。
- 详细记录见 [TEST_REPORT.md](TEST_REPORT.md)。
