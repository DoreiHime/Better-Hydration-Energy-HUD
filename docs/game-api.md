# 《逃离鸭科夫》2.3.30 游戏接口参考（BetterHydrationEnergyHUD 重写用）

> 环境：Escape from Duckov v2.3.30，Unity 2022.3.62f2 **Mono**；Managed DLL：`C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov\Duckov_Data\Managed`。
> **证据缩写**（行号为实测反编译源码）：
> `<CORE>`=`work/decompiled/TeamSoda.Duckov.Core`；`<ISS>`=`work/decompiled/ItemStatsSystem`；`<UTIL>`=`work/decompiled/DuckovUtilities/Duckov.Utilities`（`TeamSoda.Duckov.Utilities.dll`）；`<SODA>`=`work/decompiled/SodaLocalization/SodaCraft.Localizations`；`<OLD>`=`work/decompiled/mod/BetterHydrationEnergyHUD`（现有实现）。
> `【资源】`=`Duckov_Data/resources.assets` 经 UnityPy 1.25 只读解析出的层级/组件/序列化字段，需游戏内复核；`需游戏内验证`=仅静态无法判定。

## 1. CharacterMainControl：数值 API 与事件驱动方案

### 1.1 精确签名（全是属性；数值不在 CharacterMainControl 里）
| 成员 | 位置 | 实现 |
|---|---|---|
| `public float CurrentWater { get; set; }` | `<CORE>/CharacterMainControl.cs:867-884` | get `characterItem.Variables.GetFloat("CurrentWater".GetHashCode())`，set `Variables.SetFloat` |
| `public float MaxWater { get; }` | `:843-853` | `characterItem.GetStatValue("MaxWater".GetHashCode())`，无 setter |
| `public float CurrentEnergy { get; set; }` | `:824-841` | 同上，key=`CurrentEnergy` |
| `public float MaxEnergy { get; }` | `:800-810` | 同上，key=`MaxEnergy` |
| `public Item CharacterItem { get; }` | `:363` | `CharacterCreator.CreateCharacter` 里 `SetItem`（`<CORE>/CharacterCreator.cs:24`） |
| `public Health Health { get; }` | `:525` | — |
| `public bool IsMainCharacter` | `:543-553` | `LevelManager.Instance.MainCharacter == this` |
| `Health.IsDead` / `CurrentHealth` / `MaxHealth` | `<CORE>/Health.cs:172` / `:155-170` / `:108-121` | — |

**Water/Energy 没有专用事件**（全量检索 `event/Action`：CharacterMainControl `:1101-1125` 只有队伍/动作/射击；Health 只有 `OnHealthChange/OnMaxHealthChange/OnDeadEvent/OnHurtEvent`，`Health.cs:36-42`）。

### 1.2 真正的可订阅点（本任务最重要发现）
- `Item.Variables` 是 `CustomDataCollection`（`<ISS>/ItemStatsSystem/Item.cs:443`），元素为 `<UTIL>/CustomData.cs:9` 的 `CustomData`。
- **`CustomData` 有值变化事件**：`public event Action<CustomData> OnSetData;`（`<UTIL>/CustomData.cs:63`）；`SetFloat` 写 `Data` 时**无条件** `OnSetData?.Invoke(this)`（`CustomData.cs:29-38,96`）。`CustomDataCollection.SetFloat(int hash,float)` 转发到 entry（`<UTIL>/CustomDataCollection.cs:259-268`；按 key 版 `:64,175`）。
  → TickVariables、`AddWater/AddEnergy`（`<CORE>/CharacterMainControl.cs:2397-2425`）、读档都会触发，**不用 Harmony**。
- 上限是 **Stat**：`Item.GetStatValue`（`<ISS>/ItemStatsSystem/Item.cs:1144`），订阅 `Stat.OnSetDirty`（`<ISS>/ItemStatsSystem/Stat.cs:104`；`AddModifier/RemoveModifier/SetDirty` 触发，`:153-183`）；`Item.GetStat(string)` 见 `Item.cs:1139`。

### 1.3 更新时机
`CharacterMainControl.Update` 每帧调 `TickVariables(Time.deltaTime, 1f)`（`<CORE>/CharacterMainControl.cs:1698-1724`，`:1706`）；`TickVariables` 内 `variableTickTimer < tickTime` 直接 return（`:2120-2127`）→ **水/能量约 1 秒变一次，且每次 tick 必写一次**（≈2 事件/秒）。旧 mod 的 0.05s 轮询 + 10Hz 跟随 + 0.2s MarkDirty（`<OLD>/ModBehaviour.cs:168,213,256-263`）应全部删除，改为「事件驱动 + 整数变化才 SetText」。

### 1.4 可编译的事件订阅器
```csharp
using System;
using Duckov.Utilities;   // CustomData
using ItemStatsSystem;    // Item, Stat
using UnityEngine;

namespace BetterHydrationEnergyHUD
{
    public sealed class CharacterStatWatcher : IDisposable
    {
        CharacterMainControl main; Item item;
        CustomData water, energy; Stat maxWater, maxEnergy;
        public event Action Changed;
        public bool Bound { get; private set; }

        public bool TryBind()
        {
            Unbind();
            var lm = LevelManager.Instance; if (lm == null) return false;
            main = lm.MainCharacter;
            if (main == null || main.Health == null || main.Health.IsDead) return false;
            item = main.CharacterItem;
            if (item == null || item.Variables == null) return false;
            water = item.Variables.GetEntry("CurrentWater");
            energy = item.Variables.GetEntry("CurrentEnergy");
            maxWater = item.GetStat("MaxWater");
            maxEnergy = item.GetStat("MaxEnergy");
            if (water == null || energy == null || maxWater == null || maxEnergy == null) return false;
            water.OnSetData += OnData; energy.OnSetData += OnData;
            maxWater.OnSetDirty += OnStat; maxEnergy.OnSetDirty += OnStat;
            Bound = true; return true;
        }
        public void Unbind()
        {
            if (water != null) water.OnSetData -= OnData;
            if (energy != null) energy.OnSetData -= OnData;
            if (maxWater != null) maxWater.OnSetDirty -= OnStat;
            if (maxEnergy != null) maxEnergy.OnSetDirty -= OnStat;
            water = energy = null; maxWater = maxEnergy = null; Bound = false;
        }
        /// max<=0 / 未初始化 / 加载中 / 死亡 时返回 false（避免显示 0 100/100）
        public bool TryRead(out int cw, out int mw, out int ce, out int me)
        {
            cw = mw = ce = me = 0;
            if (!Bound || item == null || main == null || main.Health.IsDead) return false;
            if (!LevelManager.LevelInited || !LevelManager.AfterInit || SceneLoader.IsSceneLoading) return false;
            mw = Mathf.RoundToInt(Mathf.Max(0f, main.MaxWater));
            me = Mathf.RoundToInt(Mathf.Max(0f, main.MaxEnergy));
            if (mw <= 0 || me <= 0) return false;
            cw = Mathf.Clamp(Mathf.RoundToInt(main.CurrentWater), 0, mw);
            ce = Mathf.Clamp(Mathf.RoundToInt(main.CurrentEnergy), 0, me);
            return true;
        }
        void OnData(CustomData _) { Changed?.Invoke(); }
        void OnStat(Stat _) { Changed?.Invoke(); }   // 读 .Value 时才重算，这里只通知
        public void Dispose() { Unbind(); Changed = null; }
    }
}
```
**重生/切场景**：每关重新 load Item 实例（`<CORE>/LevelManager.cs:610-627`；`CreateMainCharacterAsync :540-544`），旧 `CustomData` 随旧 Item 销毁（`<ISS>/ItemStatsSystem/Item.cs:1122-1127`）→ 必须在 `OnAfterLevelInitialized` 之后重新 `TryBind()`，不能缓存引用。**死亡**：`isDead=true → OnDeadEvent → Health.OnDead` 静态事件（`Health.cs:437-451`），`LevelManager.OnMainCharacterDead`（`LevelManager.cs:648-656,276`）；随后切场景。

## 2. HUD 结构（【资源】已核对，旧路径仍有效）

```
LevelManager (root)
└─ HUDCanvas   Canvas(ScreenSpaceOverlay, sortingOrder=0, overrideSorting=false)
               CanvasScaler(ScaleWithScreenSize, referenceResolution=2560x1440, match=1[height])
               CanvasGroup(alpha=1) + HUDManager + FadeGroup + CanvasGroupFade + CanvasScalerController
   ├─ Water    RectTransform 100x100, anchor(0.5,0), pivot(0.5,0), anchoredPos(-533.6,53.6) + CanvasGroup + WaterHUD
   │  └─ Background(100x100)
   │     ├─ Background (2) sizeDelta(-42,-42)   ├─ Fill sizeDelta(-10,-10) [ProceduralImage.fillAmount]
   │     └─ Icon sizeDelta(-60,-60) → 40x40
   ├─ Energy   同上，anchoredPos(-419.0,53.6) + EnergyHUD   （旧 mod 误称 hunger/饱腹；游戏里是"能量"）
   ├─ HP       RectTransform 260x50, anchoredPos(-594.0,53.6) + CanvasGroup + HealthHUD
   │  └─ Background(stretch) → Icon(35x35) / Slider → Fill_1 / Text (TMP)  ← HealthHUD.text, 字体 "7segment SDF"
   └─ 其他：AimMarker、ItemShortCut、ViewButtons、SmoothFollowCharacterHUD/Stamina,Oxygen、BulletTypeHUD…
```
- **LevelManager 源码中没有 HUDCanvas 字段/属性**（全仓 grep 仅旧 mod 出现 `HUDCanvas`）；HUDCanvas 是 LevelManager prefab 的子物体（【资源】`LevelManager/HUDCanvas`），只能通过组件查找或 `Transform.Find` 获取。HUD 可见性：`HUDManager.ShouldDisplay` 是私有的（`<CORE>/HUDManager.cs:19-34`，不可直接调）；实际显隐落在 HUDCanvas 的 `CanvasGroup`（public 组件）上，可直接读 `hudCanvas.GetComponent<CanvasGroup>().alpha`，或 `HUDManager.RegisterHideToken/UnregisterHideToken`（`HUDManager.cs:101-111`）。
- **`Water/Background/Icon`、`Energy/Background/Icon` 在 2.3.30 仍成立**（`<OLD>/ModBehaviour.cs:139,142`；资源层级一致）。但别再 `GameObject.Find("LevelManager")`（`<OLD>:280-300`），直接用 `WaterHUD/EnergyHUD` 组件。
- `WaterHUD`：公开 `ProceduralImage fillImage/backgroundImage`、`Color backgroundColor/emptyBackgroundColor`（`<CORE>/WaterHUD.cs:5-45`）；`EnergyHUD` 同构；`HealthHUD` 额外公开 `public TextMeshProUGUI text;`（`<CORE>/HealthHUD.cs:24`，Update 写 `"cur / max"` `:54-59`）。
- HUDCanvas 的 CanvasGroup 由 HUDManager/FadeGroup 控制显隐（`<CORE>/HUDManager.cs:19-34,81-99`）。**文字挂到 Water/Energy 下就自动继承淡入淡出/隐藏，不需要自己判断黑屏。**
- 字体：HUD 数字字体为 TMP_FontAsset `7segment SDF`（HP 已用它显示 `"100 / 100"`）；最稳是**克隆 `HealthHUD.text`**（含 font/material/outline/字号），兜底 `GameplayDataSettings.UIStyle.TemplateTextUGUI`（`<CORE>/Duckov.Utilities/GameplayDataSettings.cs:630,650,854`）。**禁止** legacy `Text`+`Arial.ttf`（`<OLD>/ModBehaviour.cs:481`、`<OLD>/BHE_SettingsUI.cs:339`）：中文缺字且丑。中文设置标签用原生 Options 标签克隆（游戏自带 fallback 链；`globalgamemanagers.assets` 内含 `LiberationSans SDF - Fallback`、`UnifontExMono SDF` 等）。

### 2.1 挂载与定位（零 Update、零跟随）
```csharp
using Duckov.Utilities;          // GameplayDataSettings
using SodaCraft.Localizations;   // TextLocalizor
using TMPro;
using UnityEngine;

namespace BetterHydrationEnergyHUD
{
    public sealed class HudLabels : MonoBehaviour
    {
        WaterHUD water; EnergyHUD energy;
        TextMeshProUGUI wt, wb, et, eb;

        public bool TryAttach()
        {
            var ws = Object.FindObjectsOfType<WaterHUD>(true);
            var es = Object.FindObjectsOfType<EnergyHUD>(true);
            var hh = Object.FindObjectOfType<HealthHUD>(true);
            if (ws.Length == 0 || es.Length == 0) return false;
            water = ws[0]; energy = es[0];
            TextMeshProUGUI tpl = hh != null ? hh.text : null;
            if (tpl == null && GameplayDataSettings.UIStyle != null)
                tpl = GameplayDataSettings.UIStyle.TemplateTextUGUI;
            if (tpl == null) return false;
            wt = Make(tpl, water.transform,  "BHE_WaterTop",    true);
            wb = Make(tpl, water.transform,  "BHE_WaterBottom", false);
            et = Make(tpl, energy.transform, "BHE_EnergyTop",   true);
            eb = Make(tpl, energy.transform, "BHE_EnergyBottom",false);
            return true;
        }

        static TextMeshProUGUI Make(TextMeshProUGUI tpl, Transform parent, string name, bool above)
        {
            var label = Object.Instantiate(tpl, parent, false);
            label.name = name;
            label.raycastTarget = false; label.maskable = false;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center; label.text = "";
            var loc = label.GetComponent<TextLocalizor>();
            if (loc != null) Destroy(loc);                       // 防止模板本地化覆盖文本
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // 锚在圆环矩形中心
            rt.sizeDelta = new Vector2(180f, 40f);
            rt.pivot = new Vector2(0.5f, above ? 0f : 1f);        // 上：向上长；下：向下长
            float half = parent is RectTransform prt ? prt.rect.height * 0.5f : 50f;
            rt.anchoredPosition = new Vector2(0f, above ? half + 8f : -half - 8f);
            label.fontSize = 22f;
            label.color = new Color32(96, 202, 249, 255);
            return label;
        }

        public void Refresh(bool ready, int cw, int mw, int ce, int me, bool top, bool bottom)
        {
            Show(wt, ready && top, (mw - cw).ToString());
            Show(wb, ready && bottom, cw + " / " + mw);
            Show(et, ready && top, (me - ce).ToString());
            Show(eb, ready && bottom, ce + " / " + me);
        }
        static void Show(TextMeshProUGUI t, bool on, string s)
        {
            if (t == null) return;
            t.gameObject.SetActive(on);
            if (on && t.text != s) t.text = s;                   // 只有整数变化才 SetText
        }
        public void DestroyAll()
        {
            if (wt) Destroy(wt.gameObject); if (wb) Destroy(wb.gameObject);
            if (et) Destroy(et.gameObject); if (eb) Destroy(eb.gameObject);
            water = null; energy = null;
        }
    }
}
```
> 差值放圆环上方、`当前 / 上限` 放下方，与旧 mod 语义一致；差值建议用 `max-cur`，可在设置里切换。**不需要** `RectTransformUtility.WorldToScreenPoint`/10Hz 跟随（旧实现 `<OLD>:272-278,401-427`）：标签是圆环子节点，自动随缩放/移动/淡出。

### 2.2 避免加载黑屏出现 "0 100/100"（旧 bug 根因）
旧实现：① 自建嵌套 Canvas 且 `sortingOrder=9999 + overrideSorting=true`（`<OLD>/ModBehaviour.cs:338-359`）把文字画到加载幕布之上；② 只等 `CharacterMainControl.Main != null` 就显示（`<OLD>:124-164,225-270`），当时 `AfterInit=false`、值还是 0。
正确门控（全部满足才显示）：
```csharp
bool ready = LevelManager.LevelInited         // <CORE>/LevelManager.cs:250-260
          && LevelManager.AfterInit           // :211-221（InitLevel 最末尾才 true，:451-455）
          && !SceneLoader.IsSceneLoading      // <CORE>/SceneLoader.cs:58,86-94
          && watcher.Bound && watcher.TryRead(out _, out _, out _, out _);
```
并且**不要自建 Canvas**：文字挂在 HUDCanvas→Water/Energy 下，HUDManager 的 CanvasGroup 会跟着加载幕布/UI 一起淡出。

## 3. 生命周期

### 3.1 Mod 组件
`ModManager.ActivateMod`：`Assembly.LoadFrom(<mod>.dll)` → 找 `<info.ini name>.ModBehaviour`（必须继承 `Duckov.Modding.ModBehaviour`）→ `new GameObject` + `AddComponent` → `SetParent(ModManager.transform)` → `Setup(master, info)` → `OnAfterSetup()`（`<CORE>/Duckov.Modding/ModManager.cs:427-501`，类型名拼接 `:445`；`ModBehaviour.cs:11-29`）。ModManager 在 GameManager 下，GameManager 是 `DontDestroyOnLoad`（`<CORE>/GameManager.cs:14,72,91,142-147`）→ **mod 组件跨场景常驻**。停用：`NotifyBeforeDeactivate()` → `OnBeforeDeactivate()` → `Destroy(mod 的 GameObject)`（`ModManager.cs:399-425`）。

### 3.2 关卡事件顺序（源码实测）
`LevelManager.Start → StartInit → InitLevel`（`<CORE>/LevelManager.cs:293-338,340-460`）：
`OnLevelBeginInitializing`(:351) → 创建主角色（`:362/382/391`，其中 `CharacterCreator.cs:24` 赋 CharacterItem）→ 挂死亡监听(:400) → 等其它初始化(:417) → **`levelInited=true`(:427)** → **`OnLevelInitialized`(:432)** → 回血/出口/设位置 → **`OnAfterLevelInitialized`(:453)** → `afterInit=true`(:454)。
- **重建点用 `LevelManager.OnAfterLevelInitialized`**（`:270`）；回调里等一帧（`yield return null`）再 `TryBind()`，因为 `AfterInit` 紧接着才变 true。
- 场景开始加载：`SceneLoader.onStartedLoadingScene`（`SceneLoader.cs:86`；`IsSceneLoading=true` `:157`）→ `DestroyAll()`+`Unbind()`；完成：`onFinishedLoadingScene`（`:88`，`:241` 复位）。
- 主菜单无 LevelManager：初始化设置页用 `MainMenu.OnMainMenuAwake`（`<CORE>/MainMenu.cs:6-12`）。
- `LevelManager.Instance` 场景销毁后是 Unity fake-null，访问时 `FindFirstObjectByType` 重建（`:132-142,601-608`）；所有缓存引用一律用 `== null` 判活。
- 重建策略：`OnAfterLevelInitialized` → 1 帧后 → 重新 `FindObjectsOfType<WaterHUD/EnergyHUD>(true)` → `TryAttach()` → `TryBind()` → 主动 `Refresh`。旧 HUD/Item 已随场景销毁，无需手工 Destroy。

## 4. 设置界面（原生外观）

### 4.1 官方扩展点 `CustomOptionsPanel`（推荐，零反射）
`<CORE>/Duckov.CustomOptions/CustomOptionsPanel.cs:6-25`：`public static Action<RectTransform> OnPanelEnabled;`，`OnEnable` 时把自身 RectTransform 传出来。
【资源】原生面板中确有容器：`GameManager/PauseMenu/Menu/OptionsPanel/ScrollView/Viewport/Content/Common/CustomOptions`（组件 `RectTransform`+`CustomOptionsPanel`，默认 active，宽 1416 高 0，父级 Common 是 `VerticalLayoutGroup`）——这就是官方的“第三方设置区”。
做法：订阅 `OnPanelEnabled` → 往该 RectTransform 塞原生控件；页签切换会 `SetActive(false/true)` 内容根（`OptionsPanel_TabButton.NotifySelectionChanged`，`OptionsPanel_TabButton.cs:21-26`），所以事件会多次触发，**必须幂等**（`root.childCount > 0` 直接 return）。注意容器默认 active，`OnEnable` 可能早于 mod 加载（错过事件）→ 初始化时还要主动 `Object.FindObjectsOfType<CustomOptionsPanel>(true)` 并对已有实例构建一次。布局：给 root 加 `VerticalLayoutGroup` + `ContentSizeFitter(verticalFit=PreferredSize)`，每行 `LayoutElement(preferredHeight≈50)`，最后 `LayoutRebuilder.ForceRebuildLayoutImmediate(root)` 一次。**容器高 0，不处理布局会看不见——需游戏内验证。**

### 4.2 原生控件模板（【资源】）
| 用途 | 路径 | 组件 |
|---|---|---|
| 滑条 | `.../OptionsPanel/ScrollView/Viewport/Content/Common/UI_MouseSensitivity` | `OptionsUIEntry_Slider`（`<CORE>/Duckov.Options.UI/OptionsUIEntry_Slider.cs`） |
| 下拉 | `.../Content/Common/UI_Language`、`UI_Resolution`、`.../Graphics/UI_vSync` 等 | `OptionsUIEntry_Dropdown` |
| 按钮 | `GameplayDataSettings.UIPrefabs.Button`（`GameplayDataSettings.cs:834`；`Duckov.UI/UIPrefabsReference.cs:9-37`） | Button |
页签：按钮 `OptionsPanel/Tabs/{Common,Audio,Graphics,KeyBinding}`，内容根 `OptionsPanel/ScrollView/Viewport/Content/{Common,Audio,Graphics,UI_InputBinding,Rules}`。

### 4.3 克隆条目（不依赖 ModConfig 的关键细节）
设置持久化直接用游戏原生 `OptionsManager`（`<CORE>/Duckov.Options/OptionsManager.cs`）：`Save<T>(key,value)`(:46)、`Load<T>(key,default)`(:65)、事件 `OnOptionsChanged`(:44)，落到 `Saves/Options.ES3`，与原生设置同生命周期。mod 键统一加 `BHE_` 前缀即可，无需自建配置文件。
私有字段（反射名即源码名）：`OptionsUIEntry_Slider`：`key`(:11) `defaultValue`(:15) `label`(:18) `slider`(:21) `valueField`(:24) `valueFormat`(:27)；`OptionsUIEntry_Dropdown`：`label`(:13) `provider`(:16) `dropdown`(:19)，`LabelKey="Options_"+provider.Key`(:33-43)；`OptionsPanel`：`tabButtons`(`OptionsPanel.cs:11`)、`selection`(:13)、`Setup()`(:20)；`OptionsPanel_TabButton`：`tab`(`OptionsPanel_TabButton.cs:12`)、`selectedIndicator`(:9)。
**坑 1**：模板是 active 的，`Instantiate` 立即跑 `Awake`（读模板自己的 key/游戏值）。必须「挂到 inactive 临时父物体下克隆 → 反射改字段 → 移入 active content → SetActive(true)」。
**坑 2**：克隆下拉会连自带 `OptionsProviderBase`（如 `vSyncSetting`）一起克隆，激活会改画质/读存档。**激活前 `DestroyImmediate(旧 provider)`** 再挂自己的。
**坑 3**：label 文本来自 `"Options_"+key` 本地化，`Awake` 时读；用 `LocalizationManager.SetOverrideText("Options_BHE_*", 文案)` 注册覆盖（`<SODA>/LocalizationManager.cs:40-48,89-99`，覆盖优先于 CSV），不要只 SetText。原生 label 上**没有** TextLocalizor，无需删除。

```csharp
using System.Reflection;
using Duckov.Options;
using Duckov.Options.UI;
using UnityEngine;

namespace BetterHydrationEnergyHUD
{
    /// 原生下拉外观 + 游戏原 Options.ES3 持久化；bool/枚举/热键都能复用
    public sealed class BheChoiceProvider : OptionsProviderBase
    {
        string key = "BHE_Option"; string[] labels = { "开", "关" }; string[] values = { "1", "0" };
        public override string Key => key;
        public void Configure(string k, string[] l, string[] v) { key = k; labels = l; values = v; }
        public override string[] GetOptions() { return labels; }
        public override string GetCurrentOption()
        {
            string saved = OptionsManager.Load(key, values[0]);          // <CORE>/Duckov.Options/OptionsManager.cs:65
            for (int i = 0; i < values.Length; i++) if (values[i] == saved) return labels[i];
            return labels[0];
        }
        public override void Set(int index)
        {
            if (index < 0 || index >= values.Length) return;
            OptionsManager.Save(key, values[index]);                     // :46，写入 Options.ES3
            BheSettings.Apply(key, values[index]);                       // 立即生效
        }
    }

    internal static class NativeOptionCloner
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Set(object o, string field, object v)
        { var f = o.GetType().GetField(field, F); if (f == null) { Debug.LogWarning("[BHE] no field " + field); return; } f.SetValue(o, v); }

        /// 在 inactive 临时父物体下克隆，避免 Awake 抢先使用模板配置
        public static GameObject CloneInactive(GameObject template, out GameObject holder)
        {
            holder = new GameObject("BHE_TmpHolder");
            holder.SetActive(false);
            return Object.Instantiate(template, holder.transform, false);
        }

        public static void AddChoice(Transform content, OptionsUIEntry_Dropdown template,
                                     string key, string[] labels, string[] values)
        {
            GameObject holder;
            GameObject go = CloneInactive(template.gameObject, out holder);
            var entry = go.GetComponent<OptionsUIEntry_Dropdown>();
            var old = (OptionsProviderBase)typeof(OptionsUIEntry_Dropdown).GetField("provider", F).GetValue(entry);
            if (old != null) Object.DestroyImmediate(old);               // 坑 2
            var mine = go.AddComponent<BheChoiceProvider>();
            mine.Configure(key, labels, values);
            Set(entry, "provider", mine);
            go.transform.SetParent(content, false);
            go.name = "BHE_" + key;
            go.SetActive(true);                                          // 此时才 Awake
            Object.Destroy(holder);
        }

        /// 滑条：克隆后改 key/defaultValue/slider 范围；原生 onChange 已会写 OptionsManager
        public static void AddSlider(Transform content, OptionsUIEntry_Slider template, string key,
                                     float min, float max, float def, bool wholeNumbers)
        {
            GameObject holder;
            GameObject go = CloneInactive(template.gameObject, out holder);
            var entry = go.GetComponent<OptionsUIEntry_Slider>();
            Set(entry, "key", key); Set(entry, "defaultValue", def);
            Set(entry, "valueFormat", wholeNumbers ? "0" : "0.0");
            var slider = (UnityEngine.UI.Slider)typeof(OptionsUIEntry_Slider).GetField("slider", F).GetValue(entry);
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = wholeNumbers;
            go.transform.SetParent(content, false);
            go.name = "BHE_" + key;
            go.SetActive(true);
            Object.Destroy(holder);
        }
    }
}
```

### 4.4 不依赖 ModConfig 单独开“Mod Settings”标签页（备选，反射）
ModConfig 的做法（`work/decompiled/ModConfig/ModConfig/ModBehaviour.cs`）：`MainMenu.OnMainMenuAwake += ...` 并立即补一次（:422-427）→ `FindObjectsOfType<OptionsPanel>(true)` 取 `DontDestroyOnLoad` 那个（:273-279）→ 反射 `tabButtons`（:286）→ 克隆 `tabButtons[0].gameObject` 与其私有 `tab` 内容根（:293-320）→ 改 `tab` 指向克隆内容根、加入列表、反射调用私有 `Setup()`（:320-321）→ 标签先 `Destroy` 其 `TextLocalizor` 再 `SetText`（:322-331）→ 克隆 `UI_Resolution`/`UI_MouseSensitivity` 作模板（:335-393）。
风险：`tabButtons/tab/Setup` 私有成员改名即崩、`OptionsPanel` 多实例、`tabButtons` 可能为空（`OptionsPanel.cs:28` 越界 NRE）、模板名硬编码。**建议**：官方 `CustomOptionsPanel` 优先；ModConfig 只作为可选软依赖（反射找 `ModConfig.ModBehaviour`，用其 `AddBoolDropdownList/AddInputWithSlider`，`:73-218`）。

### 4.5 热键设置行
- 简单可靠：复用下拉（`BheChoiceProvider`）给安全键列表（F1–F12/Home/End…），值=InputSystem 绑定路径（`<Keyboard>/f10`），存 `OptionsManager` 键 `BHE_ToggleHUDKey`，变化时 `BheInput.Apply`。
- 进阶“按任意键”：原生 Button 克隆 + 原生 TMP 标签，点击后 `action.Disable(); action.PerformInteractiveRebinding().WithControlsExcluding("<Mouse>/leftButton").OnComplete(op => 保存 op.action.bindings[0].overridePath ?? effectivePath; op.action.Enable()).Start();`。
- **不要**直接克隆原生 KeyBinding 行指向自己的 action：`UIKeybindingEntry.Setup → InputIndicator.Setup/Refresh` 会用 `GameManager.MainPlayerInput.actions[action.name]`（`<CORE>/UIKeybindingEntry.cs:86-90`、`work/decompiled/Plugins/InputIndicator.cs:94-129`），找不到会 NRE（§6）。

## 5. 本地化

### 5.1 游戏本体
`LocalizationManager.CurrentLanguage`（`<SODA>/LocalizationManager.cs:15`）、`OnSetLanguage`（:38）、`GetPlainText(key)`/`key.ToPlainText()`（:89-119，扩展 :116）、`SetOverrideText`（:40-53）。数据源 `StreamingAssets/Localization/<SystemLanguage>.csv`（`<SODA>/LanguageSettings.cs:16`、`LocalizationDatabase.cs:31-53`）；本机有 ChineseSimplified/ChineseTraditional/English/French/German/Japanese/Korean/Portuguese/Russian/Spanish。`TextLocalizor` 会在 OnEnable/切语言时刷新（`TextLocalizor.cs:32-95`），自控文本前先删它。

### 5.2 Mod 自己的 `Localization/*.json`（ValuePerKg 范式）
- 路径：**DLL 所在目录**下 `Localization/<SystemLanguage>.json`（`work/decompiled/ValuePerKg/ValuePerKg.Localizations/Localization.cs:39-48`）。
- 结构（自定键，任意层级）：
```json
{ "Settings": { "ShowWaterTop": "显示水分差值", "ShowWaterBottom": "显示水分 当前/上限",
                "FontSize": "HUD 字号", "ToggleKey": "开关热键" },
  "Hud": { "Water": "水分", "Energy": "能量" } }
```
- 加载：`JObject.Parse(File.ReadAllText(path))`（Newtonsoft.Json 游戏自带；`Localization.cs:49-60`），按语言缓存 `Dictionary<SystemLanguage,JObject>`，失败回退 `SystemLanguage.English`（`:22-37`，英文枚举=10）。用法：`GetWithFallback(LocalizationManager.CurrentLanguage)["Settings"]["FontSize"]`（`work/decompiled/ValuePerKg/ValuePerKg/ModBehaviour.cs:176-177,304`）。
- **不会自动加载**：`LocalizationDatabase` 只扫 `StreamingAssets/Localization/*.csv`（`LocalizationDatabase.cs:31-53`），mod json 必须自己读。

## 6. 输入（游戏主用 InputSystem；旧 Input 也存在，但新代码别用）
- 新输入系统：`CharacterInputControl` 用 `UnityEngine.InputSystem`+`PlayerInput`（`<CORE>/CharacterInputControl.cs:11,83-97,148`）；`GameManager.MainPlayerInput`（`<CORE>/GameManager.cs:40,132`）；`InputRebinder`（`<CORE>/InputRebinder.cs:131-147`）；动作资产 `GameplayDataSettings.InputActions`（`GameplayDataSettings.cs:826`）。
- 旧输入也存在（Player Settings 应为 Both）：游戏自己脚本仍用 `Input.GetKeyDown`（`<CORE>/DamageToSelf.cs:13,18`、`Duckov.Consoles/DConsole.cs:56,67`、`Duckov.MiniGames.Debugging/HideAndLockCursor.cs:21`）。旧 mod 的 F10 即 `Input.GetKeyDown((KeyCode)291)`（`<OLD>/BHE_SettingsUI.cs:94`）。
- 推荐：自建 `InputAction`（事件驱动、零轮询）+ `OptionsManager` 存绑定路径：
```csharp
using Duckov.Options;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BetterHydrationEnergyHUD
{
    public sealed class BheInput
    {
        public const string KeyPathPref = "BHE_ToggleHUDKey";
        InputAction toggle;

        public void Init()
        {
            toggle = new InputAction("BHE_ToggleHUD", InputActionType.Button);
            toggle.AddBinding("<Keyboard>/f10");
            toggle.performed += OnToggle;
            toggle.Enable();
            string saved = OptionsManager.Load(KeyPathPref, "");
            if (!string.IsNullOrEmpty(saved)) toggle.ApplyBindingOverride(0, saved);
        }
        public void Apply(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath)) return;
            toggle.ApplyBindingOverride(0, bindingPath);
            OptionsManager.Save(KeyPathPref, bindingPath);
        }
        public string Display() { return toggle.GetBindingDisplayString(); }

        void OnToggle(InputAction.CallbackContext _)
        {
            if (SceneLoader.IsSceneLoading || GameManager.Paused || IsTyping()) return;
            BheHudController.ToggleVisible();
        }
        static bool IsTyping()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;
            var go = es.currentSelectedGameObject;
            return go.GetComponent<TMP_InputField>() != null || go.GetComponent<UnityEngine.UI.InputField>() != null;
        }
        public void Dispose()
        {
            if (toggle == null) return;
            toggle.performed -= OnToggle; toggle.Disable(); toggle.Dispose(); toggle = null;
        }
    }
}
```
- 冲突：`Keyboard.current == null` 时不注册键盘绑定（纯手柄）；`GameManager.Paused`（`<CORE>/GameManager.cs:98`）与 `SceneLoader.IsSceneLoading` 时忽略；输入框聚焦判断见上。F10 在 2.3.30 反编译与 `resources.assets` 字符串中均未检出绑定（**需游戏内验证**无冲突）。
- 游戏自带按键设置页：`OptionsPanel/.../Content/UI_InputBinding`，条目 `UIKeybindingEntry`（`<CORE>/UIKeybindingEntry.cs`），底层 `InputRebinder.RebindAsync(name,index,excludes,save)`（`:131-147`，存档键 `InputBinding` `:27,71-91`）。**不要蹭游戏 action 名**（存档/加载顺序风险）。

## 7. Mod 生命周期与清理 / Harmony
- 顺序：`Setup()` → `OnAfterSetup()`（`ModBehaviour.cs:11-16`）→ Start/OnEnable；停用：`NotifyBeforeDeactivate()` → `OnBeforeDeactivate()` → `Destroy(gameObject)`（`ModManager.cs:399-425`）。
- `OnBeforeDeactivate` 必做：① 退订全部静态事件（`SceneLoader.onStartedLoadingScene/onFinishedLoadingScene`、`LevelManager.OnAfterLevelInitialized/OnInitialized/OnMainCharacterDead`、`LocalizationManager.OnSetLanguage`、`OptionsManager.OnOptionsChanged`、`CustomOptionsPanel.OnPanelEnabled`、`ModManager.OnModActivated` 等）；② `watcher.Dispose()`；③ `labels.DestroyAll()` + 设置页自建子物体/inactive holder；④ `RemoveOverrideText("Options_BHE_*")`；⑤ `BheInput.Dispose()`；若用过 `HUDManager.RegisterHideToken`（`<CORE>/HUDManager.cs:101-111`）要 `UnregisterHideToken`；⑥ 停 Coroutine、清静态缓存。
- 只 Destroy 自己 `new/Instantiate` 的对象，绝不 Destroy 游戏对象（HUD、OptionsPanel、GameManager 子物体）。
- Harmony：`HarmonyLoadMod` 只做 `Assembly.LoadFrom(<dir>/0Harmony.dll)`（`work/decompiled/HarmonyLoadMod/HarmonyLoadMod/ModBehaviour.cs`）。本方案（事件+原生 UI 克隆）**完全不需要 Harmony**；若确需 patch，用反射探测已加载的 `HarmonyLib.Harmony`（`AppDomain.CurrentDomain.GetAssemblies()`），避免编译期硬依赖。
- 软依赖探测（ModConfig/Duckov-Feather/其它 API mod；具体类型名以对方 DLL 为准，需安装后反编译确认）：
```csharp
static System.Type FindType(string fullName)
{
    foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
    { var t = a.GetType(fullName, false); if (t != null) return t; }
    return null;
}
// var t = FindType("ModConfig.ModBehaviour"); if (t != null) { /* 反射调用其静态 API */ }
```

## 8. 风险与坑（按优先级）
1. **API 稳定性**：本文成员均 2.3.30 实测；小版本升级后重点复查 `WaterHUD/EnergyHUD/HealthHUD` 字段、`CustomData.OnSetData`、`Stat.OnSetDirty`、`CustomOptionsPanel.OnPanelEnabled`、`OptionsUIEntry_*` 私有字段、`LevelManager` 事件、ModManager 加载规则。反射全部 try/catch，字段缺失时降级自建面板。
2. **Unity fake-null**：缓存的 `LevelManager/MainCharacter/Item/WaterHUD` 切场景后 `== null` 为 true 但引用非空；一律 `== null` 判断，禁 `?.` 调用；事件回调第一行判活。
3. **事件生命周期**：Item/CustomData 每关重建；忘重绑=数值不更新，忘退订=持有已销毁对象。切场景、死亡、停用三处都要解绑。
4. **只读不写**：`CustomData.SetFloat` 类型不匹配会静默不写（`<UTIL>/CustomData.cs:96-104` 只 LogWarning），不要写游戏变量。
5. **性能**：`FindObjectsOfType/FindObjectsOfTypeAll` 只在绑定期调用（每关≤20 次）；Update 零工作；TMP `SetText` 仅整数变化（≈1Hz）；不调用 `Canvas.ForceUpdateCanvases/LayoutRebuilder`（除设置页初始化一次）；`raycastTarget=false`；不新增 Canvas/GraphicRaycaster。
6. **黑屏**：不复刻旧 mod 嵌套 Canvas+`sortingOrder=9999/overrideSorting`（`<OLD>/ModBehaviour.cs:338-359`）；门控 `AfterInit`；显隐交给 HUDManager CanvasGroup。
7. **原生设置克隆**：私有字段改名、模板 GameObject 名变化（`UI_MouseSensitivity/UI_Resolution/UI_vSync`）、模板自带 provider 副作用（务必 `DestroyImmediate`）、inactive 克隆防 Awake 抢跑、OptionsPanel 多实例/空列表、`CustomOptions` 容器高 0 需自建布局——全部「需游戏内验证」。
8. **本地化**：`SetOverrideText` 是全局字典，用 `BHE_` 前缀并在停用时清理；切换语言时已实例化的原生条目不会自动刷新，需监听 `OnSetLanguage` 重建/重设文本。
9. **字体**：数字用克隆的 7segment TMP（HP 同款）；中文标签用原生 Options 标签字体；禁用 Arial/legacy Text；模板为 null 时降级隐藏而非崩溃。
10. **热键**：InputAction 在 `Dispose` 里 Disable+Dispose；无键盘设备时跳过注册；输入框聚焦/暂停/加载时忽略；不要把自定义 action 塞进游戏 `PlayerInput.actions` 再依赖 `InputRebinder.Save`。
11. **数值边界**：`MaxWater/MaxEnergy` 可能为 0（创建早期/读档异常）→ 不显示；`Current` 用 `Mathf.Clamp`；死亡/撤离/过场隐藏。

### 附：编译引用（netstandard2.1，`DuckovManaged` = 上述 Managed 目录）
```xml
<Reference Include="TeamSoda.Duckov.Core"><HintPath>$(DuckovManaged)\TeamSoda.Duckov.Core.dll</HintPath><Private>false</Private></Reference>
<Reference Include="ItemStatsSystem"><HintPath>$(DuckovManaged)\ItemStatsSystem.dll</HintPath><Private>false</Private></Reference>
<Reference Include="TeamSoda.Duckov.Utilities"><HintPath>$(DuckovManaged)\TeamSoda.Duckov.Utilities.dll</HintPath><Private>false</Private></Reference>
<Reference Include="SodaLocalization"><HintPath>$(DuckovManaged)\SodaLocalization.dll</HintPath><Private>false</Private></Reference>
<Reference Include="Unity.TextMeshPro"><HintPath>$(DuckovManaged)\Unity.TextMeshPro.dll</HintPath><Private>false</Private></Reference>
<Reference Include="Unity.InputSystem"><HintPath>$(DuckovManaged)\Unity.InputSystem.dll</HintPath><Private>false</Private></Reference>
<Reference Include="UnityEngine.UI"><HintPath>$(DuckovManaged)\UnityEngine.UI.dll</HintPath><Private>false</Private></Reference>
<Reference Include="Newtonsoft.Json"><HintPath>$(DuckovManaged)\Newtonsoft.Json.dll</HintPath><Private>false</Private></Reference>
```
入口类型名必须为 `<info.ini 里 name 的值>.ModBehaviour` 且继承 `Duckov.Modding.ModBehaviour`（`ModManager.cs:445-450`）；DLL 名 = `<name>.dll`（`ModInfo.cs:30`），放 `Duckov_Data/Mods/<name>/`。

> 验证：本文 4 个完整 C# 类（watcher / HudLabels / provider+cloner / BheInput）已用 `dotnet build`（netstandard2.1，引用本机 2.3.30 `Managed` 全部 DLL）实编译通过：**0 error / 0 warning**（`§2.2` 的条件片段与 `§7` 的探测方法除外，后者单独包类验证亦通过）。
