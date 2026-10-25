# 《逃离鸭科夫》Mod 前置框架生态调研与选型建议

> 调研时间：**2026-10-26**（数据均为当日抓取；工坊订阅数/评论会持续变化）
> 目标 mod：**BetterHydrationEnergyHUD**（工坊 `3591875771`，815,042 订阅，2025-10-25 后未更新）
> 本机环境：游戏 `2.3.30`（`Info.ini: version = 2.3.30`，`C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov`），Unity 2022.3.62f2 Mono，已订阅 53 个工坊 mod
> 证据等级：**A**=本机源码/文件实证　**B**=官方公告/API　**C**=工坊页面/评论文本　**D**=未能核实（标注「待验证」）

---

## 0. 结论（先给答案）

1. **主 mod 本体：保持"零前置、自包含"是最优解**（也是当前设计），重写时只依赖游戏官方加载器与 `TeamSoda.Duckov.Core`。**不需要** ModConfig / ModSetting / Duckov-Feather / BepInEx 中的任何一个来"修 HUD / 提性能"——这几个框架都不解决 HUD 数值读取与性能问题。
2. **设置界面：首选游戏官方 `CustomOptionsPanel` + 官方 `OptionsManager` 持久化**（1.3.10 官方补丁加入，`OnPanelEnabled` 钩子已在本机 2.3.30 反编译源码中确认存在），零第三方前置、原生外观。**不要**把设置界面硬绑到 ModConfig（已停更、仓库已归档、作者自己警告"未来游戏更新可能失效"）。
3. 如果重写后确认**必须用 Harmony 补丁**（例如游戏没有暴露数值变化事件、只能 patch HUD 更新函数），则唯一前置选 **HarmonyLoadMod（3589088839，149 万订阅、315 个 mod 依赖、本机已装）**，并把 `0Harmony` 视为编译期依赖、由该前置在运行时提供；不要自己往 mod 里塞 `0Harmony.dll`（多个 mod 各自内嵌会造成重复加载/版本不一致，HarmonyLoadMod 存在的初衷就是统一引用依据，见 §2.2 社区解释）。
4. **ModSetting（3595729494，46.8 万订阅）是"设置界面"层面比 ModConfig 明显更好的框架**（支持按键绑定/滑块/下拉/输入/按钮/分组、主菜单可用、非强依赖 API），但：**11 个月未更新**、**本机未安装**、集成有"加载顺序 + 重启才生效"的社区坑。建议只做**软依赖（检测到才把同样的设置注册过去）**，不设为必需物品，更不作为主 mod 的硬前置。
5. **Duckov-Feather / 鸭羽（3759478942，1.09 万订阅）适合"内容型 mod"（物品/任务/天赋/敌人），不适合 HUD mod**：它为 29 个模块、22 个 Harmony 补丁，还强制依赖 HarmonyLoadMod，且社区有"与 Harmony 冲突导致 mod 列表空白/卡加载""建筑消失"等反馈（作者已修复部分），属于过度依赖。除非未来要把本 mod 扩展成大型内容 mod，否则不引入。
6. **BepInEx 不适用**：它走 Nexus 手动安装、独立插件加载体系，与官方 `Duckov_Data/Mods` + 创意工坊分发是两套世界；把它作为前置会让 81.5 万订阅玩家中的绝大多数装不上。仅可作本地调试备选。
7. **Ducky.Sdk 不是运行时前置**：它是构建期 SDK（源码分发 + ILRepack 单 DLL + 自动部署 + 本地化源生成），**不是**玩家需要订阅的框架，且 2026-01-06 后无更新。可作为工程脚手架参考，不进入选型对比的"前置"行列。
8. 官方 mod API 自 **2026-02（2.0.26）** 之后**没有再出现 mod 相关改动**；**2.3.30（2026-07-02）补丁说明里完全没有 MOD 条目**。这既说明 2.3.x 对 mod 相对友好（API 稳定），也说明"适配最新版本"主要靠自己按 2.3.30 反编译源码重写，而不是等框架更新。

**最终推荐组合（详见 §6）：**
| 位置 | 推荐 | 备选 |
|---|---|---|
| 主 mod 本体 | **A. 零前置自包含**（官方 `ModBehaviour` + 自绘 overlay） | B. `HarmonyLoadMod` 最小前置（仅在必须 patch 时） |
| 设置界面 | **官方 `CustomOptionsPanel` + `OptionsManager`**（零前置） | ModSetting **软依赖**（检测到才注册，不设必需物品）；自建 OptionsPanel tab 兜底 |
| 明确淘汰 | ModConfig（设置 UI）、BepInEx（加载器）、Duckov-Feather（主 mod 依赖） | — |

---

## 1. 目标 mod 现状与社区问题（为什么要改）

### 1.1 客观数据（2026-10-26 抓取）

| 项目 | 数值 | 来源 |
|---|---|---|
| 工坊 ID | 3591875771 | [工坊页](https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771) |
| 当前订阅 | **815,042** | 同上 |
| 收藏 / 评分 | 12,131 / 3,896（5 星） | 同上 |
| 发布 / 最后更新 | 2025-10-22 18:26 / **2025-10-25 06:09** | 同上 |
| 改动说明 | 6 条（全部在 2025-10-22~25） | [changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3591875771) |
| 必需物品（前置） | **无** | 工坊页无"必需物品"区块 |
| 体积 | 427.957 KB（含 `language/zh`、`language/en` 两套 DLL） | 工坊页 + 本机 `workshop/content/3167020/3591875771` |
| GitHub | [DoreiHime/Better-Hydration-Energy-HUD](https://github.com/DoreiHime/Better-Hydration-Energy-HUD)：MIT，1 star，创建 2025-10-26、最后 push 2025-10-26（英文版/E 平台分发用） | GitHub API |
| 本地存档配置 | mod 根目录 `settings.ini`（`ShowWaterTop`/偏移/`HudFontSize` 等 13 项） | 本机文件实测 |

**结论：该 mod 已约 12 个月未更新，但官方加载器和 API 在 2.3.x 保持稳定（§3），所以"还能用"和"该重写优化"并不矛盾。**

### 1.2 社区吐槽与需求（原文 + 链接）

| 类别 | 原文摘录 | 出处 |
|---|---|---|
| **F10 硬编码、冲突、关不上** | "人才啊一张图做了四五个任务 **F10 就是关不上，正好遮挡拾取页面，什么也捡不起来**，切输入法，切屏都没用"（白观，2026-06-14） | [留言页](https://steamcommunity.com/sharedfiles/filedetails/comments/3591875771) |
| | "overlaps with the cross hair mod... both have F10, though it looks like for both it is **hardcoded** into the MOD...either need configurable or maybe F9"（HercMighty，2025-12-25） | 同上 |
| | "overlaps with other hotkey. **Can I change the hotkey?**"（흥덕아재，2025-12-23） | 同上 |
| | "优点：简洁、直观、清晰…**可改进的点：快捷键固定 F10，无法调整**，容易和部分 MOD 的快捷键冲突"（多维爆炸，2025-12-15） | 同上 |
| **黑屏时显示 `0 100/100`** | 作者自述："**进入/退出场景时黑屏上会有 0 100/100 的数字，这个我想办法解决。。**" | 工坊简介 |
| `--/--` 占位符（已修） | 作者："--/-- 的问题…这个占位符问题是**游戏对 mod 开关不绝对**造成的"、"已修复" | [BUG 讨论帖](https://steamcommunity.com/workshop/filedetails/discussion/3591875771/596289460456774458/) |
| **性能疑问** | "占用 fps 率有点高 正常吗"（XXX_NPAI2100，2025-11-13） | [留言页](https://steamcommunity.com/sharedfiles/filedetails/comments/3591875771) |
| **分辨率对位** | 1080P 下"数值往右偏 1 点…默认坐标往左偏一丢丢"（终战之翼，2025-10-29），作者承认 1K 偏左只能手动调 | [BUG 讨论帖 #5/#6](https://steamcommunity.com/workshop/filedetails/discussion/3591875771/596289460456774458/) |
| **语言** | "Could the language setting be **configurable in-game with the mod config mod**, or **base it off the game's set language**?"（Reaper，2025-11-08） | [建议帖](https://steamcommunity.com/workshop/filedetails/discussion/3591875771/596289845780810425/) |
| **功能需求** | "Can you add an option for the top number to **count down from 100 > 0**?"（shimosabe，2026-08-08） | 同上 |

### 1.3 从本机反编译源码看到的实现（根因分析，证据 A）

源码：`work/decompiled/mod/BetterHydrationEnergyHUD/`（下称 BHE）。

- **HUD 定位**：`Boot()` 协程每 0.5s 轮询 `CharacterMainControl.Main` 和 `FindHudCanvas()`，最多 12 次（[ModBehaviour.cs#L124-L164](../work/decompiled/mod/BetterHydrationEnergyHUD/ModBehaviour.cs#L124)）；`FindHudCanvas()` 先走 `LevelManager.Instance.../HUDCanvas`，失败后 `Resources.FindObjectsOfTypeAll<Canvas>()` 全量搜索兜底（同文件 L280-L300）。图标路径硬编码 `Water/Background/Icon`、`Energy/Background/Icon`，再加 sprite 名兜底。
- **刷新循环**：`Loop()` 每 0.05s 醒一次（unscaled），数值 4Hz（变化后 1.5s 内 20Hz 突发），位置 10Hz；只在数值变化时 `SetTextIfDiff` 并限制每 0.2s 才标记 Layout 重建（[ModBehaviour.cs#L166-L269](../work/decompiled/mod/BetterHydrationEnergyHUD/ModBehaviour.cs#L166)）。**不是纯每帧 SetText，但空转轮询 + HUD 丢失时的重度查找是主要性能/稳定性隐患。**
- **黑屏 `0 100/100` 的直接嫌疑**：`UpdateValues()` 在 `CharacterMainControl.Main == null || main.Health.IsDead` 时**直接 return，既不隐藏也不清空文本**（[ModBehaviour.cs#L225-L231](../work/decompiled/mod/BetterHydrationEnergyHUD/ModBehaviour.cs#L225)）；场景切换/加载界面期间 overlay 未随 HUD 隐藏 → 上一帧的 "0 / 100" 留在黑屏上。
- **F10 硬编码**：设置面板在 `Update()` 里 `Input.GetKeyDown((KeyCode)291)`（291 = Unity `KeyCode.F10`），无任何配置项（[BHE_SettingsUI.cs#L94](../work/decompiled/mod/BetterHydrationEnergyHUD/BHE_SettingsUI.cs#L94)）。
- **设置面板是自绘全屏 Canvas**：`BHE_Settings_Canvas` + 自建 uGUI 控件（[BHE_SettingsUI.cs#L159-L172](../work/decompiled/mod/BetterHydrationEnergyHUD/BHE_SettingsUI.cs#L159)），不进游戏设置菜单，这也是"每人都自己画设置面板"生态乱象的一部分。
- **兼容性风险点**：若别的 mod 完全替换 `HUDCanvas` 或隐藏圆环图标，文本锚定会失败（工坊简介亦承认）——外部 mod 冲突不可控。

---

## 2. 候选框架逐一核实

> 说明：工坊页面的"必需物品"即 Steam 强制前置；"依赖此项目的所有 N 个项目"是工坊反查的随附数据。

### 2.1 总览表（2026-10-26 抓取）

| 候选 | 工坊 ID | 订阅 | 收藏 | 评分 | 发布 | 最后更新 | 强制前置 | 依赖它的 mod | 本机已装 | 维护活性 |
|---|---|---|---|---|---|---|---|---|---|---|
| **HarmonyLoadMod**（显示名 HarmonyLib） | [3589088839](https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839) | **1,493,805** | 18,004 | 3,852 | 2025-10-18 | **2025-11-03** | 无 | **315** | ✅ | 代码只有 42 行、纯加载器，**版本无关** |
| **ModConfig** | [3590674339](https://steamcommunity.com/sharedfiles/filedetails/?id=3590674339) | 1,175,664 | 14,941 | 3,274 | 2025-10-20 | **2025-11-11** | 无 | 103 | ✅ | **GitHub 已 archived**（2026-05-31 元数据更新，最后代码 2025-11-12） |
| **ModSetting** | [3595729494](https://steamcommunity.com/sharedfiles/filedetails/?id=3595729494) | 467,990 | 6,010 | 1,335 | 2025-10-29 | **2025-11-30** | **无** | 70 | ❌ | GitHub 最后 push 2025-12-01（6 star/3 fork），无归档 |
| **Duckov-Feather 鸭羽** | [3759478942](https://steamcommunity.com/sharedfiles/filedetails/?id=3759478942) | 10,873 | 143 | 34 | 2026-07-06 | **2026-09-23** | **HarmonyLib（3589088839）** | — | ❌ | **活跃**：GitHub 最后 push 2026-09-20，1 个 open issue |
| **Ducky.Sdk** | NuGet（非工坊） | 13.3K 下载（总计） | — | — | — | **2026-01-06** | 编译期引用，非玩家前置 | — | — | 2026-01-06 后无更新；0 open issue |
| **BepInEx** | Nexus `escapefromduckov/mods/3`（非工坊） | — | — | — | —（Nexus 手动分发） | 未知（页面 403） | 无（自带加载器） | — | ❌ | 与官方 mod 体系并行（待验证） |
| 目标 mod 本体 | 3591875771 | 815,042 | 12,131 | 3,896 | 2025-10-22 | 2025-10-25 | 无 | — | ✅ | 约 12 个月未更新 |

### 2.2 HarmonyLoadMod（唯一可考虑的"加载器级"前置）

- **事实**：工坊显示名 **HarmonyLib**，149.38 万订阅、**315 个 mod 依赖**、2.652 MB；描述"独立版 Harmony 已同步更新到 **2.4.1.0**，现已支持 Mac"（[工坊页](https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839)）。
- **本机实证（A）**：`workshop/content/3167020/3589088839` 仅含 `0Harmony.dll`（**FileVersion 2.4.1.0**）、`HarmonyLoadMod.dll`（5,120 B）、`info.ini`、预览图；`Duckov_Data\Managed` 里**没有** `0Harmony.dll` → 游戏本体不内置 Harmony，Harmony 必须由前置 mod 提供。
- **实现极简（A）**：反编译后 `OnEnable()` → `Assembly.LoadFrom(<自身目录>/0Harmony.dll)`，共 42 行（[HarmonyLoadMod/ModBehaviour.cs#L29-L41](../work/decompiled/HarmonyLoadMod/HarmonyLoadMod/ModBehaviour.cs#L29)）。它**不依赖任何游戏 API**，因此**不会随游戏版本失效**；"2025-11-03 之后没更新"对该前置不是实质性风险。
- **社区注意点（C）**：评论区有玩家解释其作用是"统一引用依据，避免每个 mod 各自内嵌 Harmony 带来的性能/冲突问题"，并强调**必须排在依赖它的 mod 上面**，否则 Steam 上依赖它的 mod 会勾选不上；另有 2025-11-11 两条"用了进不去游戏""每两秒卡顿"的反馈（可能与别的 mod 混合有关，[留言页](https://steamcommunity.com/sharedfiles/filedetails/comments/3589088839)）。
- **评估**：**若必须 patch，它是最稳的前置**（149 万订阅的事实标准；纯 DLL 加载、无版本耦合）。**若不需要 patch，则完全不必引入。**

### 2.3 ModConfig（设置 UI 框架之一，已停止维护）

- **工坊**：117.57 万订阅、3,274 评分、103 个 mod 依赖；最后更新 **2025-11-11**（[工坊页](https://steamcommunity.com/sharedfiles/filedetails/?id=3590674339)）。
- **GitHub 已归档（A）**：[FrozenFish259/duckov_mod_config](https://github.com/FrozenFish259/duckov_mod_config) API 返回 `"archived": true`，最后代码 push **2025-11-12**，5 个 open issue；README 与工坊简介均明确警告：
  > "`ModConfig` 实现逻辑较复杂，**不排除未来游戏某次更新后会导致其失效并需要重新维护的可能性**"、
  > "**禁止**将你的 mod 配置读写逻辑与 ModConfig 建立强依赖关系，配置读写即使没有 ModConfig 也必须满足正常功能"（[README](https://github.com/FrozenFish259/duckov_mod_config)）
- **能力有限**：只支持 `bool/int/float/string` 四种类型；**不支持按键绑定**；设置菜单**只进游戏后才能打开**（主菜单看不到）；首次启用/开关后可能需要重启（工坊简介）。仅 KillFeed 一个 mod 明确接入（[KillFeed 工坊页](https://steamcommunity.com/sharedfiles/filedetails/?id=3588412062)）。
- **脆弱点（A）**：反编译 `ModConfig.dll` 可见它靠**反射**拿官方 `OptionsPanel` 的私有字段 `tabButtons`，克隆一个 tab、替换私有 `tab` 字段再反射调用 `Setup()`；模板下拉框/滑条也是从游戏现有控件实例克隆（[ModConfig/ModBehaviour.cs#L270-L352](../work/decompiled/ModConfig/ModConfig/ModBehaviour.cs#L270)）。**游戏 Options UI 一改结构就崩**，这与作者警告一致。
- **社区反馈（C）**：2026-01-22"非 ModConfig 设置会被覆盖为默认设置，就算正在游戏中也会被覆盖"；2025-11-16"更新后每次都会恢复成默认设置"；2026-10-24"最近几天就卡在摄像头哪里了点一下然后直接卡住"（**疑似当前版本兼容问题，待验证**）。均有 [留言页](https://steamcommunity.com/sharedfiles/filedetails/comments/3590674339) 原文。
- **结论**：**不再作为新接入目标**，更不应硬依赖。

### 2.4 ModSetting（设置 UI 框架，明显比 ModConfig 完整，但同样停更）

- **工坊**：46.80 万订阅、1,335 评分、70 个 mod 依赖、15 条改动说明；发布 2025-10-29，最后更新 **2025-11-30**；**无"必需物品"（不需要任何前置）**（[工坊页](https://steamcommunity.com/sharedfiles/filedetails/?id=3595729494)）。
- **作者定位（C）**：因为"**ModConfig 没有按键绑定和主菜单设置**，不能支持字体缩放的设置，所以做了一个**增强版**的 Mod 设置"——即 **ModSetting 是 ModConfig 的替代/增强，不是它的附属**；两者可并存但各占一个设置 tab（工坊简介）。
- **能力（A+C）**：支持下拉/滑块（int、float）/开关/输入框/**按键绑定**/按钮/分组；有**主菜单设置**与设置保存系统；两种接入方式：
  1. **复制 `ModSettingAPI.cs`（反射调用）**：mod 与 ModSetting 互不强制、不看启用顺序、可独立运行，代价是内存占用略高 → **推荐给"希望独立运行"的 mod**；
  2. **引用 `ModSetting.dll`（`SettingsBuilder`）**：性能更好，但**强依赖、mod 必须排在 ModSetting 下面**。
  源码：[xisTC/ModSetting](https://github.com/xisTC/ModSetting)（MIT，6 star/3 fork，最后 push 2025-12-01；[readme](https://github.com/xisTC/ModSetting/blob/master/readme.md) 列出 0.5.0/0.4.0 更新）。`ModSetting.csproj` 显示 .NET Framework 4.8、直接引用全套游戏 DLL，**未引用 0Harmony** → 不需要 HarmonyLoadMod。
- **本机实证（A）**：本机已装的 **ValuePerKg** 反编译源码里就带一份 `ModSettingAPI.cs`：
  - `private static readonly Version VERSION = new Version(0, 4, 0);`、`MOD_NAME = "ModSetting"`、`TYPE_NAME = "ModSetting.ModBehaviour"`（[ModSettingAPI.cs#L38-L56](../work/decompiled/ValuePerKg/ModSettingAPI.cs#L38)）；
  - API：`AddDropDownList / AddSlider(float/int) / AddToggle / AddKeybinding(WithDefault) / AddInput / AddButton / AddGroup / GetValue / SetValue / GetSavedValue / HasConfig / RemoveUI / RemoveMod`（同文件 L97-L306）；
  - 接入时机：`ModManager.OnModActivated` 事件里判断 `modInfo.name == "ModSetting"` 后初始化（[ValuePerKg/ModBehaviour.cs#L37-L96](../work/decompiled/ValuePerKg/ValuePerKg/ModBehaviour.cs#L37)），并在 `OnDisable()` 里 `RemoveMod()` 清理；语言切换时 `RemoveMod + Init + Setup` 重建。
  - 本机游戏 2.3.30 反编译确认 `ModManager.OnModActivated` 是官方静态事件（`Action<ModInfo, ModBehaviour>`，[ModManager.cs#L69](../work/decompiled/TeamSoda.Duckov.Core/Duckov.Modding/ModManager.cs#L69)）→ 这套"软依赖"模式**有官方事件支撑，是可靠的**。
- **坑（C）**：
  - ValuePerKg 作者/玩家在评论区反复踩"**加载顺序**"：mod 必须排在 ModSetting 上面（或下）才显示配置项、排序后**还要重启游戏**；作者结论："我得等游戏通知我的模组加载完了才能初始化 ModSetting 的通信"（[ValuePerKg 留言页](https://steamcommunity.com/sharedfiles/filedetails/comments/3590728993)）。
  - ModSetting 自己也有"设置被重置/最新版打不开游戏"等历史反馈（2026-02-10、2026-10-03 等，[留言页](https://steamcommunity.com/sharedfiles/filedetails/comments/3595729494)）；长 mod 名会撑破设置项布局。
  - 作者 readme 还提示：引用 dll 方式的前置未启用时，会导致"所有 mod 勾选不上"的假象（readme "已知的 bug" 一节）。
- **结论**：**能力上最适合做"统一设置入口"，但生态健康度不足以做主依赖**；建议"软依赖 + 可选集成"，不作为必需物品。

### 2.5 Duckov-Feather / 鸭羽（内容型 mod 框架）

- **工坊**：10,873 订阅、143 收藏、34 评分；发布 2026-07-06，最后更新 **2026-09-23**；**必需物品：HarmonyLib（3589088839）**；9 条改动说明（[工坊页](https://steamcommunity.com/sharedfiles/filedetails/?id=3759478942)）。
- **GitHub（A/B）**：[0999312/Duckov-Feather](https://github.com/0999312/Duckov-Feather)，MIT，7 star/2 fork，创建 2025-11-22，最后 push **2026-09-20**，1 个 open issue；另有模板仓库 [duckov_modding_template](https://github.com/0999312/duckov_modding_template)（GitHub template，2025-12-13 更新）。
- **能力（B）**：README 宣称 29 个模块、22 个 Harmony 补丁点、15 个原生事件桥接；.NET Standard 2.1；Harmony 2.4.1.0（运行时由 HarmonyLoadMod 提供，仓库内 vendored 仅供编译）；`fml.json` 声明依赖并做**拓扑排序**，解决加载顺序问题；有 `ModOptionsRegistry.RegisterPanel(modId, displayName, builder)`，参数支持 `AddToggle/AddSlider/AddDropdown/AddButton`，**面板出现在"游戏设置 → Custom Options 标签页"，值经 `OptionsManager` 自动持久化**（[API_INTERACTION_UI.md](https://github.com/0999312/Duckov-Feather/blob/main/Docs/API/API_INTERACTION_UI.md)）。其 `GameUIUtils` 可克隆原生 Button/ScrollRect/ItemDisplay、取游戏字体与配色。
- **问题/风险（C/B）**：
  - 工坊 5 条留言里两条是加载冲突："又不行了啊大佬，打了这个 mod 进游戏 **mod 列表直接空白**，只能全部取消重新订阅一遍才行…**跟 Harmony 冲突**，两个单独订阅就没事儿，一块订阅就卡加载"（BB Aviator，2026-08-14）；"你这个前置一打**好多东西直接消失了**"（valar morghulis，2026-07-08，作者 7-7 已回复修复，玩家 7-8 确认恢复）。
  - GitHub 最新合并的 PR #8 是 "Fix camera overlay issue by **introducing URP pipeline modification**"（2026-09-20）→ 框架会改渲染管线，和视觉类 mod 的潜在冲突面较大。
  - open issue [#9](https://github.com/0999312/Duckov-Feather/issues/9)（2026-09-28）指出其 `Identifier → 数字 ID 自动偏移` 机制**对存档有风险**（物品 ID 随 mod 顺序漂移）——本 HUD mod 不涉及自定义物品，**该风险与本次需求无关**，但说明框架复杂度。
- **结论**：**为 HUD 引入它属于"为了一个设置页而背上一个内容框架"**。除非未来要把它扩展成大型内容 mod，否则不选。

### 2.6 Ducky.Sdk（构建期 SDK，不是运行时前置）

- **NuGet**：[Ducky.Sdk 0.1.8](https://www.nuget.org/packages/Ducky.Sdk)，最后发布 **2026-01-06**，总下载 13.3K，MIT；要求 **.NET SDK 9.0** 构建，目标 `netstandard2.1`；源码分发、ILRepack 合成单 DLL、自动部署到游戏目录、源生成器本地化。
- **GitHub**：[ducky7go/Ducky.Sdk](https://github.com/ducky7go/Ducky.Sdk)，5 star/3 fork，最后 push **2026-01-06**，0 open issue。
- **性质**：玩家**不需要**安装它；用它构建的 mod 是"自包含单 DLL"。它提供 `ModBehaviourBase` 与 `ModOptions.ForThis.Set/Get`（**只做配置存储，README 未提供游戏内设置面板/UI 注册 API**）、MessageHub 跨 mod 通信、可选 Harmony。
- **结论**：可作为工程脚手架（自动部署、info.ini 生成、本地化 CSV 源生成）参考，但**不是"前置框架"选项**；2026-01 后停更，且当前项目已有可用构建流程，**不建议切换**。

### 2.7 BepInEx（不适用于工坊 mod）

- **存在性**：Nexus 有 [BepInEx 5 with Configuration Manager](https://www.nexusmods.com/escapefromduckov/mods/3)（`escapefromduckov/mods/3`）；第三方镜像 [vgtimes 条目](https://vgtimes.com/games/escape-from-duckov/files/88446-bepinex-5.html)（2025-10-28 上传、1,205 下载）称"BepInEx 5 是运行本游戏所有 mod 的前置"——**该说法与官方工坊 mod 体系不符，属第三方表述**。
- **无法核实**：Nexus 页面被 Cloudflare 403 拦截，**最后更新时间/是否适配 2.3.30 待验证**。
- **结论**：BepInEx 是独立插件加载器（改 `winhttp.dll`/`BepInEx/plugins`），与官方 `Duckov_Data/Mods` + `ModBehaviour` + 创意工坊**不互通**；把它作为前置意味着玩家要退出 Steam 创意工坊安装流程手动装文件，**订阅转化率会断崖式下跌**。只建议作为本地调试/热重载工具，不建议发布依赖。

### 2.8 其它社区资源（开发参考，非前置）

| 资源 | 说明 | 状态 |
|---|---|---|
| [xiaomao-miao/duckovAPI](https://github.com/xiaomao-miao/duckovAPI) | 社区维护的游戏 API/反编译参考（220 star、23 fork、4 open issue） | 最后代码 push **2025-10-25**，**已落后于 2.3.30**；只适合入门检索 |
| [obscurefreeman/duckovsrc](https://github.com/obscurefreeman/duckovsrc) | "鸭科夫部分解包，供开发者使用" | 参考 |
| [0999312/duckov_modding_template](https://github.com/0999312/duckov_modding_template) | 社区 mod 工程模板 | GitHub template，2025-12-13 更新 |
| [官方 MOD Guide](https://store.steampowered.com/news/app/3167020/view/617681009992796272) | 官方 mod 指南（由官方道歉公告引用） | 正文抓取未成功，**待验证**；[官方道歉公告](https://steamcommunity.com/games/3167020/announcements/detail/1818752592131064) 引用其为准 |
| 官方渠道 | [Discord](https://discord.gg/2aNHAV4rXN)、[Reddit r/EscapeFromDuckov](https://www.reddit.com/r/EscapeFromDuckov) | 官方社区 wrap 中公布（[来源](https://steamcommunity.com/games/3167020/announcements/detail/676261330313809982)） |

---

## 3. 官方 modding API 变化与"官方推荐做法"

### 3.1 官方公告里与 mod 相关的全部改动（近 50 条公告扫描，证据 B）

数据源：[Steam News API](https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=3167020&count=50&format=json) + [官方 RSS](https://steamcommunity.com/games/3167020/rss/)。

| 版本 | 日期 | 与 mod 相关的内容 | 链接 |
|---|---|---|---|
| **1.3.10** | 2025-12-18 | **MOD Related Updates**：① 新增 `GameplayDataSettings.UIPrefabs.Button` / `ScrollRect` prefab 引用；② 新增 **`CustomOptionsPanel.OnPanelEnabled` 事件**（用于在设置界面管理自定义 UI）；③ 可用 `DConsole.RegisterCommand()` 加游戏内命令；④ 自定义合成/分解 API（`RegisterDynamic`） | [公告](https://steamcommunity.com/games/3167020/announcements/detail/1819386365088887) |
| **2.0.26** | 2026-02-10 | **MOD Related Updates**：① 新增静态事件 `Task.OnTaskStatusChanged`；② **移除主菜单里多余的 `ModManagerUI` 实例** | [公告](https://steamcommunity.com/games/3167020/announcements/detail/1823825466507474) |
| **2.3.19 / 2.3.20 / 2.3.30** | 2026-06-25 / 06-26 / **07-02** | **均无 MOD 相关条目**；2.3.30 只有任务/武器/平衡/碰撞修复 | [2.3.30](https://steamcommunity.com/games/3167020/announcements/detail/683005949807430766)、[2.3.20](https://steamcommunity.com/games/3167020/announcements/detail/683005949807428326)、[2.3.19](https://steamcommunity.com/games/3167020/announcements/detail/683005949807428071) |

> 结论：**2.0.26（2026-02）之后半年多没有官方 mod API 变更；2.3.30 本身不含 mod 改动。** 对 mod 开发者而言，2.3.x 是"API 稳定期"；但也没有新的官方设施可等，重写要依赖 2.3.30 反编译源码（见 `docs/game-api.md`）。

### 3.2 官方设置 UI 钩子（关键发现，证据 A+B）

1.3.10 公告里的 `CustomOptionsPanel` 在本机 2.3.30 反编译源码中确认存在：

```csharp
namespace Duckov.CustomOptions;
public class CustomOptionsPanel : MonoBehaviour {
    public static Action<RectTransform> OnPanelEnabled;   // L10
    private void OnEnable() { ... OnPanelEnabled?.Invoke(rectTransform); } // L12-24
}
```
（[CustomOptionsPanel.cs](../work/decompiled/TeamSoda.Duckov.Core/Duckov.CustomOptions/CustomOptionsPanel.cs#L10)）

含义：**官方提供了一个"设置面板出现时把 RectTransform 交给 mod"的静态事件**，mod 可据此把自己的控件挂进游戏原生设置界面——这正是 **零前置设置界面** 的官方入口。Duckov-Feather 的 `ModOptionsRegistry` 文档也明确写"面板出现在 游戏设置 → **Custom Options** 标签页"（[API 文档](https://github.com/0999312/Duckov-Feather/blob/main/Docs/API/API_INTERACTION_UI.md)），侧证该 tab 存在且被框架使用。
> ⚠️ **待验证**：`CustomOptionsPanel` 是"全体 mod 共享一个 Custom Options tab"还是"每个 mod 一页"；以及如何获得/复用该面板实例。需按 `docs/game-api.md` 在游戏内实测（游戏代码中除该文件外无其他引用，面板实例可能来自 prefab 资源）。

### 3.3 官方设置持久化 API（证据 A）

`Duckov.Options.OptionsManager` 是公开静态类，读写 `Options.ES3`，带 `OnOptionsChanged` 事件与备份恢复：
- `public static event Action<string> OnOptionsChanged;`（[OptionsManager.cs#L44](../work/decompiled/TeamSoda.Duckov.Core/Duckov.Options/OptionsManager.cs#L44)）
- `public static void Save<T>(string key, T obj)` / `public static T Load<T>(string key, T defaultValue)`（同文件 L46-L101）
> 用它做 mod 设置持久化可以**完全不需要第三方配置框架**；注意 key 加 mod 前缀防冲突（Feather 内部也是走它，见其 API 文档"值经 OptionsManager 自动持久化"）。

### 3.4 官方原生控件资源（证据 B）

1.3.10 新增 `GameplayDataSettings.UIPrefabs.Button` / `ScrollRect` 引用；写设置页/按钮时优先克隆官方 prefab，外观、精灵、字体、Shader 自动继承——这就是同类 mod（Duckov-Feather 的 `GameUIUtils.CloneButton/CloneScrollRect`、ModConfig 克隆 `OptionsUIEntry_*`）"原生观感"的来源。

### 3.5 是否有"官方推荐的前置框架"

- **没有找到官方指定的前置框架**。官方 mod 指南（Steam News `617681009992796272`，由官方道歉公告引用）正文未能抓取，**待验证**；官方公告从未点名 ModConfig/ModSetting/Feather/HarmonyLoadMod。
- 官方对 mod 的治理以"安全/合规"为主：恶意 mod 公告、零容忍、3 个工作日内处理（[恶意 mod 公告](https://steamcommunity.com/games/3167020/announcements/detail/693145389614759948)、[道歉公告](https://steamcommunity.com/games/3167020/announcements/detail/1818752592131064)）。**没有任何"官方推荐使用某社区框架"的表述**——所以"官方推荐做法"实际就是：`Duckov_Data/Mods/<name>/info.ini` + `ModBehaviour` 子类 + 官方 API。

---

## 4. 同类 HUD / 信息显示 mod 的实现方式与社区评价

| mod | 订阅/评分 | 最后更新 | 前置 | 实现/设置方式（页面原文） | 社区评价要点 |
|---|---|---|---|---|---|
| **信息显示增强**（3592074969） | 31,907 / 303 | 2025-11-04 | **无**（作者称"已移除对 HarmonyLib 的依赖"；但本机 DLL 里仍打包了 `0Harmony.dll 2.3.3.0`，**待验证**） | "已在**设置面板中增加本 Mod 相关选项**"，5 种语言；显示血量数值/名字/建筑名/击杀目标；HUD 文字 TMP | 好评为主；"建筑物按 F 会卡死"、"拍照模式后自己血条消失"、"第三人称只有数字没有血条"；作者另做了 **设置面板增强**（3595113062，5,033 订阅）解决"多个 mod 各自设置 tab 挤在一起" | 
| **谁偷我帧数？**（3594997139） | 2,988 / 165 | 2025-10-29 | **HarmonyLib** | F3 开关；**Harmony patch 每个已加载 mod 做耗时统计**，右侧显示 FPS/帧时间 | "打开后加载巨慢"、"进了几次进不去游戏"、"卡顿"；作者称部分无法复现。**印证：patch 全量 mod 的 Harmony 方案代价高、易冲突** |
| **ValuePerKg**（3590728993） | 32,746 / 327 | 2025-11-23 | **ModSetting** | "不加前置 mod 也能正常使用，只是无法在菜单中修改参数"；用 `ModSettingAPI.cs` 反射接入，支持开关/字体/颜色阈值 | "必须排在 ModSetting 上面才有设置项""排序完还得重开游戏"——**ModSetting 集成有加载顺序坑的实证** |
| **KillFeed**（3588412062） | 360,566 / 1,900 | 2025-10-25 | **ModConfig** | 订阅 ModConfig 后可在"Mod 设置"调整；未检测到 ModConfig 时读 `Duckov_Data/StreamingAssets/KillFeedModConfig.txt` | 兼容/表现良好；偶有"敌人死后尸体残留可反杀"的 bug 反馈（与击杀判定有关） |
| **设置面板增强**（3595113062） | 5,033 / 59 | 2025-11-04 | 无 | 给设置面板**分页**（超过 5 个 tab 出现翻页），解决"多个 mod 自定义 tab 溢出" | 其存在本身说明：**"每个 mod 自建设置 tab"会挤爆设置界面**，是生态痛点 |

**从同类 mod 提炼的常规做法：**
1. **HUD 数值**：直接读 `CharacterMainControl.Main`（CurrentWater/MaxWater/CurrentEnergy/MaxEnergy/Health）——本 mod 已这样做；改进点是**事件驱动**（若 `docs/game-api.md` 确认无事件，再考虑 Harmony patch 或低频轮询）。
2. **HUD 定位**：锚定游戏原生 `HUDCanvas` 的圆环图标（`Water/Background/Icon`、`Energy/Background/Icon`），找不到时用 sprite 名兜底——本 mod 已做；**建议改为按组件类型/父级结构稳健查找，并在 HUD 重建（分辨率/场景变化）时重建 overlay**。
3. **UI 外观**：克隆游戏原生 prefab（`GameplayDataSettings.UIPrefabs.Button`/`ScrollRect`、`OptionsUIEntry_*`）+ 游戏 TMP 字体，而不是自绘一套。
4. **设置界面**：三种路线——① 官方 `CustomOptionsPanel`（零依赖，推荐）；② 反射克隆官方 `OptionsPanel` tab（ModConfig 路线，无第三方依赖但最脆弱）；③ 第三方框架（ModSetting/ModConfig，玩家需额外订阅）。
5. **性能**：HUD 文本只在值变化时 SetText + 限频 Layout 重建（本 mod 已做一半）；禁用/隐藏时停止协程；避免 `Resources.FindObjectsOfTypeAll` 常驻调用；避免全屏 `RaycastTarget`。

---

## 5. 选型方案对比

### 5.1 端到端方案（主 mod 本体）

| 维度 | **方案 A：零前置自包含（推荐）** | **方案 B：最小前置（HarmonyLoadMod）** | **方案 C：框架化（ModSetting 设置 + 自包含 HUD，或 Feather 全框架）** |
|---|---|---|---|
| 组成 | 官方 `ModBehaviour` + 自绘 overlay Canvas/TMP + 官方 API 读取 | A + `HarmonyLoadMod`（必需物品）用于 patch | ModSetting 硬依赖（设置）或 Duckov-Feather（全框架） |
| 玩家额外安装 | **0** | 1 个（149 万订阅，本机已装者众） | ModSetting 1 个（46.8 万订阅，**本机未装**）；Feather 2 个（Feather + HarmonyLoadMod，1.09 万订阅） |
| 2.3.x 兼容证据 | 官方 API 稳定（2.0.26 后无 mod 变更；2.3.30 无 mod 条目）；本机 2.3.30 源码可直接核对 | HarmonyLoadMod 只 `Assembly.LoadFrom` 0Harmony，版本无关；社区事实标准 | ModSetting 2025-11-30 后停更、无 2.3.x 适配证据；Feather 2026-09 仍活跃 |
| 性能收益 | 高（事件驱动/去轮询/按需刷新，全在自己控制） | 最高（可 patch 到数值更新点，零轮询） | 低（框架不解决 HUD 刷新） |
| 实现成本 | 中（要自己处理 HUD 生命周期/黑屏/热键/设置页） | 中高（补丁需处理兼容与卸载） | 中高（学框架 + 处理加载顺序） |
| 主要风险 | HUD 结构变化需自己适配；无官方设置 tab 时需自建 | 补丁冲突（参考"谁偷我帧数"）；需声明前置 | 第三方框架失效/停更/加载顺序/订阅流失 |
| 结论 | **✅ 主推** | ✅ 需要 patch 时的唯一备选 | ❌ 不推荐（除非未来扩展内容 mod） |

### 5.2 设置界面方案对比

| 维度 | **官方 CustomOptionsPanel + OptionsManager（推荐）** | ModSetting 软依赖（可选增强） | 自建 OptionsPanel tab（兜底） | ModConfig（淘汰） |
|---|---|---|---|---|
| 第三方前置 | **无** | 无（检测到才用；不设必需物品） | 无 | 需要 ModConfig |
| 外观 | 游戏原生"Custom Options" tab | 游戏原生"Mod 设置"tab | 反射克隆官方 tab，外观接近原生 | 反射克隆官方 tab |
| 支持热键重绑定 | 需自己写按键捕获控件（可控） | **内置按键绑定 API** ✅ | 需自己写 | ❌ 不支持 |
| 持久化 | 官方 `OptionsManager.Save/Load`（`Options.ES3`） | 框架自带保存系统 | 官方 OptionsManager | `ModConfig.ES3`（强耦合） |
| 稳定性 | 官方 API，1.3.10 起未变 | 11 个月未更新；加载顺序/重启坑 | **最脆弱**：反射私有字段，UI 改版即崩 | **已停更/已归档**，作者警告可能失效 |
| 玩家覆盖 | 全部玩家可用 | 仅装了 ModSetting 的玩家额外获得统一入口 | 全部玩家 | 仅装 ModConfig 的玩家 |
| 结论 | **✅ 首选，零前置** | ✅ 有余力再做（软依赖） | ⚠️ 仅当官方钩子实测不可用时兜底 | ❌ 不采用 |

### 5.3 硬依赖 vs 软依赖（针对 ModSetting / HarmonyLoadMod）

| | 硬依赖（加"必需物品"） | 软依赖（反射检测，可选集成） |
|---|---|---|
| 实现 | 工坊页把前置加入 Required Items；代码可直接引用/启动时假定存在 | 复制 `ModSettingAPI.cs` 式反射层 + `ModManager.OnModActivated` 事件；未检测到就用自己的 UI |
| 玩家成本 | **所有玩家都要多订阅 1~2 个 mod**；本机 53 个已装 mod 中**没有 ModSetting**，现有订阅者需重新订阅；还受加载顺序影响（ValuePerKg 评论区反复出现） | 玩家无需任何额外操作；已装 ModSetting 的玩家自动多一个统一入口 |
| 失败模式 | 前置失效/被下架/顺序不对 → 主 mod 直接不可用（最伤 81.5 万订阅） | 前置失效 → 自动退回自带设置页，主功能不受影响 |
| 维护成本 | 低（直接引用） | 中（ValuePerKg 的 `ModSettingAPI.cs` 约 400 行反射 + 缓存 + 卸载清理，可直接借鉴） |
| 适用 | **只有 Harmony**（编译期类型依赖，无法软依赖；且 HarmonyLoadMod 是事实标准、版本无关） | **ModSetting / 其它设置框架** |
| 建议 | HarmonyLoadMod：**若必须 patch 才硬依赖**；其它：**不硬依赖** | ModSetting：**只做软依赖**；ModConfig：不集成 |

---

## 6. 最终推荐（可直接写进开发计划）

### 6.1 主 mod 本体：**方案 A（零前置）**

- 保留"无需前置"卖点（工坊简介当前就在强调这一点，也是它 81.5 万订阅的一部分原因）。
- 重写要点（与 §1.3 的根因对应）：
  1. **数据**：优先事件驱动；无事件时把 4Hz/20Hz 轮询降为"值变化才刷新 + 1~2Hz 兜底"，并用 `docs/game-api.md` 确认 `CurrentWater/Energy` 是否有 `Action/event`。
  2. **生命周期**：场景加载/卸载、`CharacterMainControl.Main` 为 null 或 `IsDead` 时**隐藏 overlay**（而不是 return 留下旧文本）→ 修复黑屏 `0 100/100`。
  3. **HUD 定位**：HUDCanvas 重建（分辨率变化、进出场景）时重建 overlay；图标查找失败时优雅降级（不显示）而不是死循环查找。
  4. **热键**：默认 F10 保留，但**可配置**（存 `OptionsManager`）；用游戏同款输入系统（本机 BHE DLL 引用的是 `UnityEngine.InputLegacyModule`，说明当前用 legacy Input；`docs/game-api.md` 需确认游戏是否同时有 InputSystem，避免与其它 mod/输入法冲突）。
  5. **性能**：只在值变化时 `SetText` + 限频重建 Layout；设置面板打开时才创建 Canvas 控件；避免 `Resources.FindObjectsOfTypeAll` 常驻。
  6. **多语言**：改为跟随游戏语言（Reaper 的建议）+ 保留 `language/` 覆盖机制，去掉"手工替换 DLL 切语言"这种反模式。
- **仅在确认必须 Harmony patch 时**切到方案 B：工坊加 `HarmonyLib(3589088839)` 为必需物品，并提示玩家把它排在 mod 列表前面。

### 6.2 设置界面：**官方 CustomOptionsPanel + OptionsManager（零前置）**

- 在 `CustomOptionsPanel.OnPanelEnabled` 里往 `RectTransform` 挂自己的控件：显示开关 4 个数值、字体大小、X/Y 偏移、刷新频率、热键重绑定（自己写按键捕获按钮）、语言跟随/覆盖、重置默认。
- 持久化用 `OptionsManager.Save/Load`，key 统一前缀 `BHE.`（避免与游戏/其它 mod 冲突）；同时保留对旧 `settings.ini` 的**一次性迁移**，避免老用户设置丢失。
- 视觉：克隆官方 prefab（`GameplayDataSettings.UIPrefabs.Button/ScrollRect`）或使用 TMP + 游戏字体，保证原生观感。
- 兜底：若游戏内实测 `CustomOptionsPanel` 无法挂载/无 Custom Options tab，再退回"反射克隆 `OptionsPanel_TabButton` 自建 tab"（ModConfig 路线，[ModConfig/ModBehaviour.cs#L270-L352](../work/decompiled/ModConfig/ModConfig/ModBehaviour.cs#L270)），但把它列为**风险方案**。

### 6.3 可选增强（第二阶段再决定）

- **ModSetting 软依赖**：复制 `ModSettingAPI.cs`（或自己写等价反射层），在 `ModManager.OnModActivated` 检测 `ModSetting` 后注册同样的开关/滑块/**按键绑定**；未检测到就只显示官方设置页。**不把 ModSetting 加进"必需物品"。** 收益：已装 ModSetting 的玩家获得统一入口，且 keybinding 控件现成；成本：约 1~2 天 + 需要处理加载顺序/语言切换/卸载清理。
- **不推荐**：为设置界面引入 ModConfig（停更+归档）或 Duckov-Feather（内容框架，对 HUD 过度）。

### 6.4 不引入任何前置的可行性

**完全可行且是首选**：官方加载器（`Duckov_Data/Mods` + `info.ini` + `ModBehaviour`）+ 官方 API（`CharacterMainControl`、`HUDCanvas`、`CustomOptionsPanel`、`OptionsManager`、`GameplayDataSettings.UIPrefabs`）已经足够实现本 mod 的全部功能与设置界面，2.3.30 反编译源码可直接核对签名（见 `docs/game-api.md`）。**唯一可能需要前置的场景**是"必须用 Harmony 拦截数值更新"——那时只加 HarmonyLoadMod 一个，且它是版本无关的纯加载器。

---

## 7. 待验证清单（按优先级）

1. **CustomOptionsPanel 的实际挂载方式**：2.3.30 里除 `Duckov.CustomOptions.CustomOptionsPanel` 自身外，反编译代码中无其他引用，面板实例可能来自 prefab；需在游戏内实测 `OnPanelEnabled` 是否触发、RectTransform 的父级/布局，确认能否直接塞控件。
2. **`CharacterMainControl` 是否有数值变化事件**（决定"事件驱动"还是"低频轮询/Harmony"）——由 `docs/game-api.md` 核实。
3. **ModSetting 0.5.0 与 2.3.30 是否兼容**：无明确失效报告，但 11 个月未更新，需游戏内实测（尤其软依赖接入的加载顺序）。
4. **ModConfig 是否在 2.3.30 失效**：2026-10-24 有"卡在摄像头/加载"评论，未复现；若最终仍要兼容老 ModConfig 集成需实测。
5. **BepInEx on Duckov 的维护状态**：Nexus 页面 403，最后更新/兼容性待验证（不影响本选型结论）。
6. **官方 MOD Guide 正文**：Steam News 页面抓取失败，需登录/换区再取；它可能包含官方对 mod 分发的额外要求。
7. **游戏是否以 InputSystem 为主**：本机 BHE 用 legacy Input；若游戏 UI 用 InputSystem，热键监听应采用 `Keyboard.current` 以避免泄漏/冲突。
8. **信息显示增强是否使用官方 CustomOptionsPanel**：其 DLL 含 `OptionsPanel/ModSetting/HarmonyLib` 字符串但无源码佐证，仅作旁证，不作为结论。

---

## 8. 关键链接汇总

**目标 mod**
- 工坊：https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771
- 留言 / 讨论 / 改动：https://steamcommunity.com/sharedfiles/filedetails/comments/3591875771 、https://steamcommunity.com/sharedfiles/filedetails/discussions/3591875771 、https://steamcommunity.com/sharedfiles/filedetails/changelog/3591875771
- GitHub（英文版/E 平台）：https://github.com/DoreiHime/Better-Hydration-Energy-HUD

**框架**
- HarmonyLoadMod：https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839
- ModConfig：https://steamcommunity.com/sharedfiles/filedetails/?id=3590674339 ｜ https://github.com/FrozenFish259/duckov_mod_config
- ModSetting：https://steamcommunity.com/sharedfiles/filedetails/?id=3595729494 ｜ https://github.com/xisTC/ModSetting ｜ https://github.com/xisTC/ModSetting/blob/master/readme.md
- Duckov-Feather：https://steamcommunity.com/sharedfiles/filedetails/?id=3759478942 ｜ https://github.com/0999312/Duckov-Feather ｜ https://github.com/0999312/Duckov-Feather/blob/main/Docs/API/API_INTERACTION_UI.md
- 鸭羽模板：https://github.com/0999312/duckov_modding_template
- Ducky.Sdk：https://github.com/ducky7go/Ducky.Sdk ｜ https://www.nuget.org/packages/Ducky.Sdk
- BepInEx（Nexus）：https://www.nexusmods.com/escapefromduckov/mods/3 ｜ 镜像：https://vgtimes.com/games/escape-from-duckov/files/88446-bepinex-5.html

**同类 mod / 生态问题**
- 信息显示增强：https://steamcommunity.com/sharedfiles/filedetails/?id=3592074969 ｜ https://github.com/ISQIShI/InformationDisplayEnhancementQX
- 设置面板增强：https://steamcommunity.com/sharedfiles/filedetails/?id=3595113062
- 谁偷我帧数：https://steamcommunity.com/sharedfiles/filedetails/?id=3594997139
- ValuePerKg：https://steamcommunity.com/sharedfiles/filedetails/?id=3590728993 ｜ https://github.com/kalonocat/ValuePerKg
- KillFeed：https://steamcommunity.com/sharedfiles/filedetails/?id=3588412062

**官方**
- Steam News API：https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=3167020&count=50&format=json
- 官方 RSS：https://steamcommunity.com/games/3167020/rss/
- 2.3.30：https://steamcommunity.com/games/3167020/announcements/detail/683005949807430766
- 2.0.26（最后一次 MOD 相关改动）：https://steamcommunity.com/games/3167020/announcements/detail/1823825466507474
- 1.3.10（CustomOptionsPanel 等）：https://steamcommunity.com/games/3167020/announcements/detail/1819386365088887
- MOD Guide：https://store.steampowered.com/news/app/3167020/view/617681009992796272
- 恶意 mod 公告：https://steamcommunity.com/games/3167020/announcements/detail/693145389614759948
- Discord：https://discord.gg/2aNHAV4rXN ｜ Reddit：https://www.reddit.com/r/EscapeFromDuckov

**本机源码/文件（证据 A，工作区相对路径）**
- `work/decompiled/TeamSoda.Duckov.Core/Duckov.CustomOptions/CustomOptionsPanel.cs`（L10 `OnPanelEnabled`）
- `work/decompiled/TeamSoda.Duckov.Core/Duckov.Options/OptionsManager.cs`（L44 事件、L46/L65 Save/Load）
- `work/decompiled/TeamSoda.Duckov.Core/Duckov.Options.UI/OptionsPanel.cs`（L10-L11 私有 `tabButtons`）
- `work/decompiled/TeamSoda.Duckov.Core/Duckov.Modding/ModManager.cs`（L69 `OnModActivated`）
- `work/decompiled/ModConfig/ModConfig/ModBehaviour.cs`（L270-L352 反射建 tab）
- `work/decompiled/HarmonyLoadMod/HarmonyLoadMod/ModBehaviour.cs`（L29-L41 加载 0Harmony）
- `work/decompiled/ValuePerKg/ModSettingAPI.cs`（L38-L56 版本/类型；L97-L306 API）
- `work/decompiled/ValuePerKg/ValuePerKg/ModBehaviour.cs`（L37-L96 软依赖接入）
- `work/decompiled/mod/BetterHydrationEnergyHUD/ModBehaviour.cs`（L124-L269 HUD 查找与刷新）
- `work/decompiled/mod/BetterHydrationEnergyHUD/BHE_SettingsUI.cs`（L94 F10；L159-L172 自绘 Canvas）
- 游戏版本：`C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov\Info.ini`（`version = 2.3.30`）
- 本机工坊目录：`C:\Program Files (x86)\Steam\steamapps\workshop\content\3167020\`（已装 HarmonyLoadMod / ModConfig，**未装** ModSetting/Feather；`Managed` 目录无 `0Harmony.dll`）
