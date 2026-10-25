# 测试报告 / Test Report — Better Hydration & Energy HUD v2.0.0

测试环境：Windows / Steam / Escape from Duckov `2.3.30` / Unity 2022.3.62f2 (Mono)。
测试日期：2026-10-26。测试方式：Lead 静态校验 + 作者本人实机配合（多轮重启验证）。

## 1. 构建与静态验证（通过）

```
dotnet build src/BetterHydrationEnergyHUD/BetterHydrationEnergyHUD.csproj -c Release
  -> 0 warning / 0 error
verify.ps1
  -> OK: BetterHydrationEnergyHUD.ModBehaviour found
  -> OK: entry point extends Duckov.Modding.ModBehaviour
  -> OK: dependency free (only game / Unity / netstandard references)
```

- 程序集引用仅：netstandard、TeamSoda.Duckov.Core / Utilities、ItemStatsSystem、SodaLocalization、
  UnityEngine.*、Unity.TextMeshPro、Unity.InputSystem、Plugins → **零前置**确认。

## 2. 无头启动冒烟（通过）

`-batchmode -nographics` 通过 Steam 启动（30+ mods 启用）：
`Mod Loaded: BetterHydrationEnergyHUD`、`[BHE] v2.0.0 loading (game 2.3.30)` 正常，无 `[BHE]` 异常。

## 3. 实机回归（最终版，通过）

### HUD
- 进档后 HUD 绑定成功：`[BHE] HUD bound in level (font=7segment SDF, templateSize=22, labelSize=…)`
- 圆环上方显示差值、下方显示「当前 / 上限」；采用三段式 TMP（当前值右对齐、斜杠居中、上限左对齐），
  不再依赖字体空格宽度。
- 加载/切场景不再出现 `0 100/100`（LevelInited/AfterInit/IsSceneLoading 门控 + 不使用 override 嵌套 Canvas）。
- 数值由 `CustomData.OnSetData` / `Stat.OnSetDirty` 驱动，仅在整数变化时写文本。

### 游戏内 F10 面板
- 打开面板：**游戏暂停（Time.timeScale=0）+ 接管鼠标（解锁/显示光标）**，可在游戏进行中实时拖动
  滑块、点开关、点每行「重置」按钮，HUD 仍可见，可对照调整。
- 关闭面板：恢复原时间缩放与光标状态；期间的鼠标/键盘输入被 `InputManager.DisableInput(ActiveInput)`
  拦截，**不会在恢复后“补开枪 / 补移动”**。
- 面板视觉：游戏原生 TMP 字体（ResourceHanRoundedCN-Medium SDF）、原生 Slider/Button、滚动 + 动画；
  每个数值右侧有独立重置按钮，底部有恢复默认。

### 官方设置标签页
- 游戏设置中新增独立「吃喝数值」标签页（键位设置之后）；注入通过 `CustomOptionsPanel.OnPanelEnabled`
  + 反射修改 `OptionsPanel.tabButtons`，并**克隆游戏原生设置行**（原生底图/字体/布局），
  仅把右侧下拉框改造成点击改键按钮。
- 修复过程：早期版本在 `OnPanelEnabled` 内克隆原生页面导致 `CustomOptionsPanel` OnEnable 递归 →
  设置界面卡死闪退；改为不复刻组件、只复制 RectTransform 布局并加防重入保护后正常。
- 修复过程：Unity `Destroy` 延迟删除导致 `TMP_Dropdown` 与新增 `Button`（两个 Selectable）冲突；
  改为 `DestroyImmediate` 后正常。

### 性能
- `ModPerformanceMonitor`（谁偷我帧数？）实测本 mod 帧耗时约 **0.3%~0.5%**。
- 无每帧轮询（面板不可见时不做任何事；HUD 仅每秒 1 次轻量 watchdog + 数值变化事件）。

## 4. 已知小问题 / 待作者最终确认

- HUD 下方「当前 / 上限」的斜杠与两侧数字的视觉间距受七段字体字宽影响，已按实测做右侧收紧与斜杠上移；
  不同字号下可能需要微调（如需可后续增加「间距」设置项）。
- 官方设置标签页依赖游戏 2.3.30 的 `OptionsPanel` 结构（反射字段 `tabButtons` / `tab`）；若未来游戏大改，
  会自动跳过注入（F10 面板仍可用），不会崩溃。

## 5. 其他验证

- 旧 `settings.ini` 迁移：旧 13 个键读取正常（实机沿用作者旧配置启动通过）。
- 关闭 mod / 切场景：标签、事件、面板清理逻辑均有 try/catch；无残留报错。
- 全程实机日志无 `[BHE]` 异常栈（除已修复的历史版本注入异常）。

> 本报告记录本机实机结果；正式上传工坊前建议作者再自行跑一次“订阅→启用→进档→F10→设置改键→退出重启”流程。

## 6. v2.0.0 发布记录（2026-10-26）

- **Steam 创意工坊** 3591875771 已更新（同一旧条目，非新建）：
  - mod 内容：BetterHydrationEnergyHUD.dll (89,600 bytes) + info.ini + preview.png，内容大小由 427.957 KB 变为 251.013 KB；
  - 工坊更新日期已刷新，新增 1 条 Change Note：v2.0.0 完全重写说明；
  - 简介已按 **30 种语言**分别保存（语言下拉全部语种；马来语原本为空，本次补全）；文末“心里话”改为时间叠加记录：2026.10 新写段落在上，2025.10 旧版原文原样保留在下。
- 工坊 FAQ 顶部新增「默认 F10，冲突可到设置 → 标签页改键」说明（30 语种；简中/繁中使用本地化标签名，其他语种 mod 内标签为英文 HUD Values）。
- **GitHub** DoreiHime/Better-Hydration-Energy-HUD 已推送提交
  8fde89（v2.0.0 源码、文档、构建脚本、30 语种文案）；仓库 About 简介与 topics 已更新。
- 发布后检查：英语页面标题恢复为 Better Hydration & Energy HUD；中文页面标题为「更好的吃喝数值可视化」；
  工坊描述中的 `[center]` 不支持，已移除并改回纯文本署名。