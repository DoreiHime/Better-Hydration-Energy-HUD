using System;
using System.Globalization;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>
    /// The HUD overlay.
    ///
    /// Verified against the game 2.3.30 resources / assemblies:
    ///   LevelManager/HUDCanvas/{Water|Energy}/Background/Icon
    ///   Water/Energy: 100x100 rect + CanvasGroup + WaterHUD / EnergyHUD component
    ///   HealthHUD has a TextMeshProUGUI used as a native template ("7segment SDF" font)
    ///
    /// Layout: labels are children of the Water/Energy rings, so they inherit HUD scaling,
    /// fading and visibility -- no follow code. The bottom "current / max" line uses three
    /// separate TMP labels: the current value is right aligned, the slash is centered and the
    /// max value is left aligned, so the spacing is identical no matter how wide the numbers get.
    ///
    /// Values are event driven (CustomData.OnSetData + Stat.OnSetDirty) and text is only written
    /// when the displayed value changes. Display is gated on LevelInited / AfterInit / not loading,
    /// which fixes the old "0 100/100" on loading screens.
    /// </summary>
    public sealed class HudOverlay : MonoBehaviour
    {
        private static readonly Color WaterColor = new Color32(96, 202, 249, 255);
        private static readonly Color EnergyColor = new Color32(255, 158, 59, 255);
        private static readonly Color WarningColor = new Color32(255, 82, 82, 255);

        private const float IconGap = 6f;
        private const string WaterKey = "CurrentWater";
        private const string EnergyKey = "CurrentEnergy";
        private const string MaxWaterKey = "MaxWater";
        private const string MaxEnergyKey = "MaxEnergy";

        private sealed class ValueRow
        {
            public RectTransform Root;
            public TMP_Text Cur;
            public TMP_Text Sep;
            public TMP_Text Max;
            public TMP_Text Pct;
            public string CurCache = "";
            public string SepCache = "";
            public string MaxCache = "";
            public string PctCache = "";
        }

        private HudTargets _targets;
        private TMP_Text _waterTop;
        private TMP_Text _energyTop;
        private ValueRow _waterBottom;
        private ValueRow _energyBottom;

        private TMP_Text _template;
        private float _templateFontSize = 28f;
        private Material _shadowMaterial;

        private CharacterMainControl _main;
        private Item _item;
        private CustomData _waterData;
        private CustomData _energyData;
        private Stat _maxWaterStat;
        private Stat _maxEnergyStat;

        private bool _dirty = true;
        private bool _pendingRebind = true;
        private float _lastFailLog;

        private string _waterTopText = "";
        private string _energyTopText = "";

        public bool Bound
        {
            get
            {
                return _waterTop != null && _energyTop != null &&
                       _waterBottom != null && _energyBottom != null &&
                       _targets != null && _targets.IsAlive;
            }
        }

        // ------------------------------------------------------------------- API
        public void RequestRebind()
        {
            _pendingRebind = true;
            _dirty = true;
        }

        public void OnLevelBegin()
        {
            DestroyLabels();
            DetachStats();
            _targets = null;
            _pendingRebind = true;
            _dirty = true;
        }

        public void OnLevelInitialized()
        {
            _pendingRebind = true;
            _dirty = true;
        }

        public void OnPlayerDead()
        {
            HideAll();
        }

        public void ApplyConfig()
        {
            try
            {
                if (_waterTop == null) return;
                float fontSize = ComputeFontSize();
                ApplyStyle(_waterTop, fontSize);
                ApplyStyle(_energyTop, fontSize);
                ApplyRowStyle(_waterBottom, fontSize);
                ApplyRowStyle(_energyBottom, fontSize);
                LayoutAll();
                _dirty = true;
            }
            catch (Exception e)
            {
                Log.Warn("ApplyConfig failed: " + e.Message);
            }
        }

        public void Tick()
        {
            if (_pendingRebind)
            {
                _pendingRebind = false;
                if (!TryBind()) return;
            }
            if (_waterTop == null) return;
            if (_dirty)
            {
                _dirty = false;
                RefreshValues();
            }
        }

        public void Watchdog()
        {
            try
            {
                if (!Bound)
                {
                    RequestRebind();
                    return;
                }
                if (LevelManager.Instance == null)
                {
                    HideAll();
                    return;
                }
                CharacterMainControl main = CharacterMainControl.Main;
                if (main == null)
                {
                    HideAll();
                    return;
                }
                if (!ReferenceEquals(main, _main)) Subscribe(main);
                _dirty = true;
            }
            catch (Exception e)
            {
                Log.Warn("HUD watchdog: " + e.Message);
            }
        }

        // --------------------------------------------------------------- binding
        private bool TryBind()
        {
            if (!LevelReady)
            {
                HideAll();
                return false;
            }

            HudTargets found;
            if (!HudLocator.TryFind(out found))
            {
                if (Time.unscaledTime - _lastFailLog > 10f)
                {
                    _lastFailLog = Time.unscaledTime;
                    Log.Debug("HUD not found yet (will retry).");
                }
                return false;
            }

            try
            {
                DestroyLabels();
                _targets = found;
                _template = AcquireTemplate();
                if (_template != null)
                {
                    try { _templateFontSize = Mathf.Max(10f, _template.fontSize); } catch { }
                }
                CreateLabels();
                Subscribe(CharacterMainControl.Main);
                ApplyConfig();
                Log.Info("HUD bound in level (font=" + (_template != null ? _template.font.name : "fallback") +
                         ", templateSize=" + _templateFontSize + ", labelSize=" + ComputeFontSize() + ")");
                return true;
            }
            catch (Exception e)
            {
                Log.Warn("HUD bind failed: " + e.Message);
                DestroyLabels();
                _targets = null;
                return false;
            }
        }

        private static bool LevelReady
        {
            get
            {
                try
                {
                    if (LevelManager.Instance == null) return false;
                    if (!LevelManager.LevelInited) return false;
                    if (SceneLoader.IsSceneLoading) return false;
                    if (LevelManager.AfterInit) return true;

                    // Compatibility fallback:
                    // LevelManager.InitLevel invokes OnAfterLevelInitialized inside a try/catch
                    // and only sets afterInit = true *after* the event. If any other mod's
                    // subscriber throws, the exception is swallowed but afterInit stays false
                    // forever (e.g. the outdated "近战cd显示" mod throws MissingFieldException
                    // for ActionProgressHUD.parentCanvasGroup). LevelInited is already true at
                    // this point, so bind the HUD anyway once the main character exists.
                    return CharacterMainControl.Main != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static TMP_Text AcquireTemplate()
        {
            try
            {
                HealthHUD health = HudLocator.FindHealthHud();
                if (health != null && health.text != null) return health.text;
            }
            catch { }

            try
            {
                if (GameplayDataSettings.UIStyle != null && GameplayDataSettings.UIStyle.TemplateTextUGUI != null)
                    return GameplayDataSettings.UIStyle.TemplateTextUGUI;
            }
            catch { }

            return null;
        }

        private void CreateLabels()
        {
            _waterTop = CreateTopLabel(_targets.Water.transform, "BHE_WaterTop");
            _energyTop = CreateTopLabel(_targets.Energy.transform, "BHE_EnergyTop");
            _waterBottom = CreateValueRow(_targets.Water.transform, "BHE_WaterBottom");
            _energyBottom = CreateValueRow(_targets.Energy.transform, "BHE_EnergyBottom");
        }

        private TMP_Text CreateTopLabel(Transform parent, string name)
        {
            TMP_Text label = CreateTextBase(parent, name, TextAlignmentOptions.Center);
            RectTransform rt = label.rectTransform;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            label.text = " ";
            return label;
        }

        private ValueRow CreateValueRow(Transform parent, string name)
        {
            GameObject rootGo = new GameObject(name, typeof(RectTransform));
            RectTransform root = (RectTransform)rootGo.transform;
            root.SetParent(parent, false);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(260f, 40f);
            root.anchoredPosition = Vector2.zero;

            ValueRow row = new ValueRow { Root = root };
            row.Cur = CreateTextBase(root, "Cur", TextAlignmentOptions.Right);
            RectTransform curRt = row.Cur.rectTransform;
            curRt.anchorMin = new Vector2(0.5f, 0.5f);
            curRt.anchorMax = new Vector2(0.5f, 0.5f);
            curRt.pivot = new Vector2(1f, 0.5f);
            curRt.sizeDelta = new Vector2(130f, 40f);
            curRt.anchoredPosition = new Vector2(-7f, 0f);

            row.Sep = CreateTextBase(root, "Sep", TextAlignmentOptions.Center);
            RectTransform sepRt = row.Sep.rectTransform;
            sepRt.anchorMin = new Vector2(0.5f, 0.5f);
            sepRt.anchorMax = new Vector2(0.5f, 0.5f);
            sepRt.pivot = new Vector2(0.5f, 0.5f);
            sepRt.sizeDelta = new Vector2(16f, 40f);
            // The 7segment slash sits on the font baseline and looks low; lift it slightly.
            sepRt.anchoredPosition = new Vector2(0f, 4f);
            row.Sep.text = "/";

            row.Max = CreateTextBase(root, "Max", TextAlignmentOptions.Left);
            RectTransform maxRt = row.Max.rectTransform;
            maxRt.anchorMin = new Vector2(0.5f, 0.5f);
            maxRt.anchorMax = new Vector2(0.5f, 0.5f);
            maxRt.pivot = new Vector2(0f, 0.5f);
            maxRt.sizeDelta = new Vector2(130f, 40f);
            maxRt.anchoredPosition = new Vector2(1f, 0f);

            row.Pct = CreateTextBase(root, "Pct", TextAlignmentOptions.Center);
            RectTransform pctRt = row.Pct.rectTransform;
            pctRt.anchorMin = new Vector2(0.5f, 0.5f);
            pctRt.anchorMax = new Vector2(0.5f, 0.5f);
            pctRt.pivot = new Vector2(0.5f, 0.5f);
            pctRt.sizeDelta = new Vector2(200f, 40f);
            pctRt.anchoredPosition = Vector2.zero;

            rootGo.SetActive(false);
            return row;
        }

        private TMP_Text CreateTextBase(Transform parent, string name, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TMP_Text text;
            if (_template != null)
            {
                text = Instantiate(_template, go.transform, false);
                text.gameObject.name = name;
                RectTransform trt = text.rectTransform;
                trt.localScale = Vector3.one;
                trt.localRotation = Quaternion.identity;
                trt.anchorMin = new Vector2(0.5f, 0.5f);
                trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.sizeDelta = new Vector2(130f, 40f);
                trt.anchoredPosition = Vector2.zero;
                try
                {
                    TextLocalizor localizor = text.GetComponent<TextLocalizor>();
                    if (localizor != null) Destroy(localizor);
                }
                catch { }
            }
            else
            {
                text = go.AddComponent<TextMeshProUGUI>();
                text.font = FontLibrary.HudFont;
            }

            // The intermediate "go" wrapper is only used when the template is null.
            if (_template != null)
            {
                // Reparent the instantiated text from the wrapper to the wrapper's parent.
                text.transform.SetParent(parent, false);
                Destroy(go);
            }

            text.name = name;
            text.raycastTarget = false;
            text.maskable = false;
            text.enableWordWrapping = false;
            text.richText = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = alignment;
            text.margin = Vector4.zero;          // template may carry margins -> uneven gaps
            text.extraPadding = false;
            text.characterSpacing = 0f;
            text.wordSpacing = 0f;
            text.text = " ";
            text.fontSize = ComputeFontSize();
            text.fontStyle = ModConfig.FontBold ? FontStyles.Bold : FontStyles.Normal;
            ApplyMaterial(text, ModConfig.TextShadow);
            return text;
        }

        private float ComputeFontSize()
        {
            float size = ModConfig.HudFontSize * 1.35f;
            return Mathf.Clamp(size, 8f, 96f);
        }

        private void ApplyStyle(TMP_Text label, float fontSize)
        {
            if (label == null) return;
            label.fontSize = fontSize;
            label.fontStyle = ModConfig.FontBold ? FontStyles.Bold : FontStyles.Normal;
            ApplyMaterial(label, ModConfig.TextShadow);
        }

        private void ApplyRowStyle(ValueRow row, float fontSize)
        {
            if (row == null) return;
            ApplyStyle(row.Cur, fontSize);
            ApplyStyle(row.Sep, fontSize);
            ApplyStyle(row.Max, fontSize);
            ApplyStyle(row.Pct, fontSize);
        }

        private void ApplyMaterial(TMP_Text label, bool shadow)
        {
            if (label == null) return;
            try
            {
                if (!shadow)
                {
                    if (_template != null) label.fontSharedMaterial = _template.fontSharedMaterial;
                    return;
                }
                if (_shadowMaterial == null)
                {
                    TMP_FontAsset font = _template != null ? _template.font : FontLibrary.HudFont;
                    _shadowMaterial = FontLibrary.CreateTextMaterial(font, new Color(0f, 0f, 0f, 0.72f), 0.18f);
                }
                if (_shadowMaterial != null)
                {
                    label.fontSharedMaterial = _shadowMaterial;
                    label.UpdateMeshPadding();
                }
            }
            catch (Exception e)
            {
                Log.Debug("Material apply failed: " + e.Message);
            }
        }

        // ---------------------------------------------------------------- layout
        private void LayoutAll()
        {
            if (_targets == null) return;
            LayoutTop(_waterTop, _targets.WaterIcon, ModConfig.WaterTopOffsetX, ModConfig.WaterTopOffset);
            LayoutTop(_energyTop, _targets.EnergyIcon, ModConfig.HungerTopOffsetX, ModConfig.HungerTopOffset);
            LayoutRow(_waterBottom, _targets.Water.transform, _targets.WaterIcon,
                ModConfig.WaterBottomOffsetX, ModConfig.WaterBottomOffset);
            LayoutRow(_energyBottom, _targets.Energy.transform, _targets.EnergyIcon,
                ModConfig.HungerBottomOffsetX, ModConfig.HungerBottomOffset);
        }

        private static float IconHalfHeight(Transform panel, RectTransform icon)
        {
            try
            {
                RectTransform panelRt = panel as RectTransform;
                if (icon != null && panelRt != null)
                {
                    float scale = panelRt.lossyScale.y > 0.0001f ? icon.lossyScale.y / panelRt.lossyScale.y : 1f;
                    return Mathf.Abs(icon.rect.height * 0.5f * scale);
                }
            }
            catch { }
            return 20f;
        }

        private static void LayoutTop(TMP_Text label, RectTransform icon, float offsetX, float offsetY)
        {
            if (label == null) return;
            try
            {
                float y = IconHalfHeight(icon.parent, icon) + IconGap + offsetY;
                RectTransform rt = label.rectTransform;
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(offsetX, y);
            }
            catch { }
        }

        private static void LayoutRow(ValueRow row, Transform panel, RectTransform icon, float offsetX, float offsetY)
        {
            if (row == null || row.Root == null) return;
            try
            {
                float y = IconHalfHeight(panel, icon) + IconGap + offsetY;
                row.Root.pivot = new Vector2(0.5f, 1f);
                row.Root.anchoredPosition = new Vector2(offsetX, -y);
            }
            catch { }
        }

        // ---------------------------------------------------------------- values
        private void RefreshValues()
        {
            if (_waterTop == null) return;

            if (!LevelReady)
            {
                HideAll();
                return;
            }

            CharacterMainControl main = CharacterMainControl.Main;
            bool valid = main != null && main.Health != null && !main.Health.IsDead;
            float maxWater = 0f, curWater = 0f, maxEnergy = 0f, curEnergy = 0f;
            if (valid)
            {
                try
                {
                    maxWater = main.MaxWater;
                    curWater = main.CurrentWater;
                    maxEnergy = main.MaxEnergy;
                    curEnergy = main.CurrentEnergy;
                }
                catch
                {
                    valid = false;
                }
            }

            bool waterOk = valid && maxWater > 0.01f;
            bool energyOk = valid && maxEnergy > 0.01f;

            SetTop(_waterTop, ref _waterTopText, waterOk, maxWater, curWater, ModConfig.ShowWaterTop);
            SetTop(_energyTop, ref _energyTopText, energyOk, maxEnergy, curEnergy, ModConfig.ShowHungerTop);
            SetRow(_waterBottom, waterOk, maxWater, curWater, ModConfig.ShowWaterBottom, WaterColor);
            SetRow(_energyBottom, energyOk, maxEnergy, curEnergy, ModConfig.ShowHungerBottom, EnergyColor);

            if (ModRuntime.HudHiddenRuntime) HideAll();
        }

        private void SetTop(TMP_Text label, ref string cache, bool valid, float max, float current, bool show)
        {
            if (label == null) return;
            if (!valid || !show) { SetActive(label, false); return; }

            float missing = Mathf.Max(0f, max - current);
            bool full = missing < 0.05f;
            if (ModConfig.HideDiffWhenFull && full) { SetActive(label, false); return; }

            string text = FormatValue(missing);
            if (!string.Equals(cache, text, StringComparison.Ordinal))
            {
                cache = text;
                label.text = text;
            }
            SetActive(label, true);
        }

        private void SetRow(ValueRow row, bool valid, float max, float current, bool show, Color baseColor)
        {
            if (row == null || row.Root == null) return;
            if (!valid || !show)
            {
                SetActive(row.Root, false);
                return;
            }

            Color color = baseColor;
            if (ModConfig.LowValueWarning && max > 0.0001f && current / max * 100f <= ModConfig.LowValueThreshold)
            {
                color = WarningColor;
            }

            if (ModConfig.UsePercent)
            {
                SetActive(row.Cur, false);
                SetActive(row.Sep, false);
                SetActive(row.Max, false);
                SetActive(row.Pct, true);
                string pct = FormatPercent(current / max);
                if (!string.Equals(row.PctCache, pct, StringComparison.Ordinal))
                {
                    row.PctCache = pct;
                    row.Pct.text = pct;
                }
                row.Pct.color = color;
            }
            else
            {
                SetActive(row.Pct, false);
                SetActive(row.Cur, true);
                SetActive(row.Sep, true);
                SetActive(row.Max, true);

                string cur = FormatValue(current);
                string maxText = FormatValue(max);
                string sep = "/";
                if (!string.Equals(row.CurCache, cur, StringComparison.Ordinal))
                {
                    row.CurCache = cur;
                    row.Cur.text = cur;
                }
                if (!string.Equals(row.SepCache, sep, StringComparison.Ordinal))
                {
                    row.SepCache = sep;
                    row.Sep.text = sep;
                }
                if (!string.Equals(row.MaxCache, maxText, StringComparison.Ordinal))
                {
                    row.MaxCache = maxText;
                    row.Max.text = maxText;
                }
                row.Cur.color = color;
                row.Sep.color = color;
                row.Max.color = color;

                SetActive(row.Root, true);
                return;
            }

            SetActive(row.Root, true);
        }

        private string FormatValue(float value)
        {
            int decimals = Mathf.Clamp(ModConfig.Decimals, 0, 2);
            if (decimals == 0) return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        private string FormatPercent(float ratio)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static void SetActive(Component component, bool active)
        {
            if (component == null) return;
            try
            {
                if (component.gameObject.activeSelf != active) component.gameObject.SetActive(active);
            }
            catch { }
        }

        private void HideAll()
        {
            SetActive(_waterTop, false);
            SetActive(_energyTop, false);
            if (_waterBottom != null) SetActive(_waterBottom.Root, false);
            if (_energyBottom != null) SetActive(_energyBottom.Root, false);
        }

        // ---------------------------------------------------------- subscriptions
        private void Subscribe(CharacterMainControl main)
        {
            DetachStats();
            _main = main;
            if (main == null) return;
            try
            {
                _item = main.CharacterItem;
                if (_item == null) return;
                CustomDataCollection variables = _item.Variables;
                if (variables != null)
                {
                    _waterData = variables.GetEntry(WaterKey);
                    if (_waterData != null) _waterData.OnSetData += OnStatDataChanged;
                    _energyData = variables.GetEntry(EnergyKey);
                    if (_energyData != null) _energyData.OnSetData += OnStatDataChanged;
                }
                _maxWaterStat = SafeGetStat(_item, MaxWaterKey);
                if (_maxWaterStat != null) _maxWaterStat.OnSetDirty += OnStatDirty;
                _maxEnergyStat = SafeGetStat(_item, MaxEnergyKey);
                if (_maxEnergyStat != null) _maxEnergyStat.OnSetDirty += OnStatDirty;
                _dirty = true;
            }
            catch (Exception e)
            {
                Log.Debug("Subscribe failed: " + e.Message);
            }
        }

        private static Stat SafeGetStat(Item item, string key)
        {
            try { return item.GetStat(key); } catch { return null; }
        }

        private void DetachStats()
        {
            try
            {
                if (_waterData != null) _waterData.OnSetData -= OnStatDataChanged;
                if (_energyData != null) _energyData.OnSetData -= OnStatDataChanged;
                if (_maxWaterStat != null) _maxWaterStat.OnSetDirty -= OnStatDirty;
                if (_maxEnergyStat != null) _maxEnergyStat.OnSetDirty -= OnStatDirty;
            }
            catch { }
            _waterData = null;
            _energyData = null;
            _maxWaterStat = null;
            _maxEnergyStat = null;
            _item = null;
            _main = null;
        }

        private void OnStatDataChanged(CustomData data)
        {
            _dirty = true;
        }

        private void OnStatDirty(Stat stat)
        {
            _dirty = true;
        }

        // ---------------------------------------------------------------- teardown
        private void DestroyLabels()
        {
            try
            {
                if (_waterTop != null) Destroy(_waterTop.gameObject);
                if (_energyTop != null) Destroy(_energyTop.gameObject);
                if (_waterBottom != null && _waterBottom.Root != null) Destroy(_waterBottom.Root.gameObject);
                if (_energyBottom != null && _energyBottom.Root != null) Destroy(_energyBottom.Root.gameObject);
            }
            catch { }
            _waterTop = null;
            _energyTop = null;
            _waterBottom = null;
            _energyBottom = null;
            _waterTopText = "";
            _energyTopText = "";
            if (_shadowMaterial != null)
            {
                try { Destroy(_shadowMaterial); } catch { }
                _shadowMaterial = null;
            }
        }

        private void OnDestroy()
        {
            DetachStats();
            DestroyLabels();
            _targets = null;
        }
    }
}
