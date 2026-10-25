using System;
using System.Collections.Generic;
using System.Reflection;
using BetterHydrationEnergyHUD.Core;
using Duckov.Options.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BetterHydrationEnergyHUD.Integration
{
    /// <summary>
    /// Official settings integration.
    ///
    /// The game (1.3.10+) exposes an empty "CustomOptions" slot in the options screen and the
    /// <c>Duckov.CustomOptions.CustomOptionsPanel.OnPanelEnabled</c> hook. Putting all mod settings
    /// at the bottom of the Common page looked out of place, so instead we add a dedicated tab
    /// after the vanilla tabs (Common / Audio / Graphics / Key bindings).
    ///
    /// That tab only contains the two hotkey bindings. Every other option lives in the in-game
    /// F10 panel, which keeps the vanilla options screen clean.
    /// </summary>
    public static class OfficialOptionsPage
    {
        private sealed class InjectedTab
        {
            public OptionsPanel Panel;
            public GameObject Button;
            public GameObject Content;
        }

        private static readonly List<InjectedTab> Injected = new List<InjectedTab>();
        private static bool _injecting;

        /// <summary>Called by the game when its custom options slot becomes visible.</summary>
        public static void HandlePanelEnabled(RectTransform customOptionsPanel)
        {
            if (_injecting) return; // never re-enter: cloning game UI used to recurse here
            try
            {
                if (customOptionsPanel == null) return;
                OptionsPanel panel = customOptionsPanel.GetComponentInParent<OptionsPanel>();
                if (panel == null) return;

                for (int i = Injected.Count - 1; i >= 0; i--)
                {
                    if (Injected[i].Panel == null) Injected.RemoveAt(i);
                }
                for (int i = 0; i < Injected.Count; i++)
                {
                    if (Injected[i].Panel == panel) return;
                }

                _injecting = true;
                try
                {
                    InjectedTab tab = Inject(panel);
                    if (tab != null) Injected.Add(tab);
                }
                finally
                {
                    _injecting = false;
                }
            }
            catch (Exception e)
            {
                _injecting = false;
                Log.Warn("Official options tab injection failed: " + e);
            }
        }

        private static InjectedTab Inject(OptionsPanel panel)
        {
            Transform tabs = panel.transform.Find("Tabs");
            Transform scroll = panel.transform.Find("ScrollView");
            Transform content = scroll != null ? scroll.Find("Viewport/Content") : null;
            if (tabs == null || content == null)
            {
                Log.Warn("OptionsPanel layout not found (Tabs/Content missing)");
                return null;
            }

            OptionsPanel_TabButton templateButton = tabs.GetComponentInChildren<OptionsPanel_TabButton>(true);
            Transform templateContent = content.Find("Common");
            if (templateButton == null || templateContent == null)
            {
                Log.Warn("OptionsPanel tab templates not found");
                return null;
            }

            // 1) New tab content: do NOT clone the vanilla page (it owns components whose
            //    OnEnable would re-enter this hook). Create an empty page and copy the layout.
            GameObject ourContent = new GameObject("BHE_TabContent", typeof(RectTransform));
            RectTransform ourRt = (RectTransform)ourContent.transform;
            ourRt.SetParent(content, false);
            CopyLayout(templateContent as RectTransform, ourRt);
            ourContent.SetActive(false);
            OptionsTabContent behaviour = ourContent.AddComponent<OptionsTabContent>();

            // 2) New tab button after the vanilla tabs.
            GameObject buttonGo = UnityEngine.Object.Instantiate(templateButton.gameObject, tabs, false);
            buttonGo.name = "BHE_TabButton";
            buttonGo.SetActive(true);
            OptionsPanel_TabButton button = buttonGo.GetComponent<OptionsPanel_TabButton>();
            if (button == null)
            {
                UnityEngine.Object.Destroy(buttonGo);
                UnityEngine.Object.Destroy(ourContent);
                return null;
            }

            TMP_Text label = buttonGo.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                try
                {
                    SodaCraft.Localizations.TextLocalizor loc =
                        label.GetComponent<SodaCraft.Localizations.TextLocalizor>();
                    if (loc != null) UnityEngine.Object.Destroy(loc);
                }
                catch { }
                label.text = L10n.TabTitle;
            }

            // 3) Point the button at our tab content and register it in the OptionsPanel list.
            try
            {
                FieldInfo tabField = typeof(OptionsPanel_TabButton).GetField("tab",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (tabField == null)
                {
                    Log.Warn("OptionsPanel_TabButton.tab field not found");
                    UnityEngine.Object.Destroy(buttonGo);
                    UnityEngine.Object.Destroy(ourContent);
                    return null;
                }
                tabField.SetValue(button, ourContent);

                FieldInfo listField = typeof(OptionsPanel).GetField("tabButtons",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                List<OptionsPanel_TabButton> list = listField != null
                    ? listField.GetValue(panel) as List<OptionsPanel_TabButton>
                    : null;
                if (list == null)
                {
                    Log.Warn("OptionsPanel.tabButtons field not found");
                    UnityEngine.Object.Destroy(buttonGo);
                    UnityEngine.Object.Destroy(ourContent);
                    return null;
                }
                list.Add(button);
            }
            catch (Exception e)
            {
                Log.Warn("OptionsPanel reflection failed: " + e.Message);
                UnityEngine.Object.Destroy(buttonGo);
                UnityEngine.Object.Destroy(ourContent);
                return null;
            }

            // 4) Clicking selects the tab. Assigning onClicked directly avoids re-running Setup()
            //    (which would duplicate handlers on all vanilla buttons).
            button.onClicked = (b, e) =>
            {
                try
                {
                    if (e != null) e.Use();
                    panel.SetSelection(b);
                }
                catch (Exception ex)
                {
                    Log.Warn("Tab click failed: " + ex.Message);
                }
            };

            behaviour.TemplateRoot = templateContent;
            behaviour.TabLabel = label;
            behaviour.Build();
            Log.Info("Official options tab injected into " + panel.name);
            return new InjectedTab { Panel = panel, Button = buttonGo, Content = ourContent };
        }

        public static void DestroyAll()
        {
            for (int i = 0; i < Injected.Count; i++)
            {
                try { if (Injected[i].Button != null) UnityEngine.Object.Destroy(Injected[i].Button); } catch { }
                try { if (Injected[i].Content != null) UnityEngine.Object.Destroy(Injected[i].Content); } catch { }
            }
            Injected.Clear();
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null) continue;
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static void CopyLayout(RectTransform from, RectTransform to)
        {
            if (from == null || to == null) return;
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
            to.offsetMin = from.offsetMin;
            to.offsetMax = from.offsetMax;
            to.localScale = from.localScale;
            to.localRotation = from.localRotation;
        }
    }

    /// <summary>Content of the dedicated mod tab: hotkey bindings only.</summary>
    internal sealed class OptionsTabContent : MonoBehaviour
    {
        private static readonly Color TextColor = new Color(0.93f, 0.95f, 0.975f, 1f);
        private static readonly Color SubColor = new Color(0.66f, 0.73f, 0.80f, 1f);
        private static readonly Color AccentColor = new Color(0.29f, 0.76f, 0.98f, 1f);
        private static readonly Color RowColor = new Color(0.10f, 0.12f, 0.145f, 1f);
        private static readonly Color ButtonColor = new Color(0.145f, 0.175f, 0.21f, 1f);

        private TMP_FontAsset _font;
        private Sprite _white;
        private TextMeshProUGUI _noteText;
        private string _capture;
        private bool _built;
        private readonly List<KeyRow> _rows = new List<KeyRow>();

        public TMP_Text TabLabel;
        public Transform TemplateRoot;

        private sealed class KeyRow
        {
            public string ConfigId;
            public TMP_Text Label;
            public TMP_Text Value;
        }

        public void Build()
        {
            if (_built) return;
            _built = true;
            try
            {
                ResolveFont();
                _white = MakeWhiteSprite();

                RectTransform root = (RectTransform)transform;
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = new Vector2(24f, 16f);
                root.offsetMax = new Vector2(-24f, -16f);

                VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.padding = new RectOffset(2, 2, 4, 8);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.UpperLeft;
                ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                LayoutElement rootLe = gameObject.AddComponent<LayoutElement>();
                rootLe.flexibleWidth = 1f;

                _noteText = NewText(root, "Note", L10n.TabNote, 15f,
                    new Color(0.84f, 0.88f, 0.92f, 0.92f), TextAlignmentOptions.TopLeft);
                _noteText.enableWordWrapping = true;
                _noteText.overflowMode = TextOverflowModes.Overflow;
                AddHeight(_noteText.gameObject, 50f);
                StyleNoteLikeNative();

                AddKeyRow("open", L10n.KeyOpenPanel);
                AddKeyRow("toggle", L10n.KeyToggleHud);

                try { L10n.OnLanguageChanged += OnLanguageChanged; } catch { }
                Refresh();
            }
            catch (Exception e)
            {
                Log.Error("Options tab content build failed", e);
            }
        }

        /// <summary>
        /// Builds one row by cloning a vanilla options row (native background, font and layout)
        /// and turning its dropdown/slider control into a click-to-rebind key field.
        /// </summary>
        private void AddKeyRow(string configId, string label)
        {
            Transform template = FindTemplateRow();
            GameObject row;
            TMP_Text rowLabel = null;
            TMP_Text valueLabel = null;

            if (template != null)
            {
                row = Instantiate(template.gameObject, transform, false);
                row.name = "BHE_Key_" + configId;
                row.SetActive(true);

                // Strip the vanilla logic/localization so the row becomes inert.
                foreach (SodaCraft.Localizations.TextLocalizor loc in
                         row.GetComponentsInChildren<SodaCraft.Localizations.TextLocalizor>(true))
                {
                    DestroyImmediate(loc);
                }
                foreach (OptionsUIEntry_Dropdown entry in row.GetComponentsInChildren<OptionsUIEntry_Dropdown>(true)) DestroyImmediate(entry);
                foreach (OptionsUIEntry_Slider entry in row.GetComponentsInChildren<OptionsUIEntry_Slider>(true)) DestroyImmediate(entry);
                foreach (OptionsUIEntry_Toggle entry in row.GetComponentsInChildren<OptionsUIEntry_Toggle>(true)) DestroyImmediate(entry);

                GameObject controlGo = null;
                TMP_Dropdown dropdown = row.GetComponentInChildren<TMP_Dropdown>(true);
                if (dropdown != null)
                {
                    controlGo = dropdown.gameObject;
                    DestroyImmediate(dropdown);
                }
                else
                {
                    Slider slider = row.GetComponentInChildren<Slider>(true);
                    if (slider != null)
                    {
                        controlGo = slider.gameObject;
                        DestroyImmediate(slider);
                    }
                    else
                    {
                        TMP_InputField input = row.GetComponentInChildren<TMP_InputField>(true);
                        if (input != null)
                        {
                            controlGo = input.gameObject;
                            DestroyImmediate(input);
                        }
                    }
                }

                // Drop the (now unused) dropdown list template so it cannot render or eat clicks.
                if (controlGo != null)
                {
                    Transform templateDropdown = controlGo.transform.Find("Template");
                    if (templateDropdown != null) DestroyImmediate(templateDropdown.gameObject);
                }

                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text t = texts[i];
                    if (t == null) continue;
                    if (controlGo != null && t.transform.IsChildOf(controlGo.transform)) continue;
                    rowLabel = t;
                    break;
                }
                if (rowLabel != null) rowLabel.text = label;

                if (controlGo != null)
                {
                    valueLabel = controlGo.GetComponentInChildren<TMP_Text>(true);
                    if (valueLabel == null)
                    {
                        valueLabel = NewText(controlGo.transform, "Value", "", 16f, Color.white,
                            TextAlignmentOptions.Center);
                        Stretch(valueLabel.rectTransform, 8f, 8f, 2f, 2f);
                    }
                    Image controlImage = controlGo.GetComponent<Image>();
                    if (controlImage == null)
                    {
                        controlImage = controlGo.AddComponent<Image>();
                        controlImage.color = new Color(1f, 1f, 1f, 0.001f);
                    }
                    controlImage.raycastTarget = true;
                    Button button = controlGo.AddComponent<Button>();
                    button.targetGraphic = controlImage;
                    button.transition = Selectable.Transition.ColorTint;
                    ColorBlock block = new ColorBlock();
                    block.normalColor = Color.white;
                    block.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
                    block.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                    block.selectedColor = Color.white;
                    block.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                    block.colorMultiplier = 1f;
                    block.fadeDuration = 0.08f;
                    button.colors = block;
                    string cfg = configId;
                    TMP_Text value = valueLabel;
                    button.onClick.AddListener(() =>
                    {
                        _capture = cfg;
                        if (value != null) value.text = L10n.KeyPress;
                    });
                }

                // The cloned row may not carry a LayoutElement; our vertical layout needs one.
                float rowHeight = 44f;
                RectTransform rowRt = row.transform as RectTransform;
                if (rowRt != null && rowRt.rect.height > 20f) rowHeight = rowRt.rect.height;
                LayoutElement layout = row.GetComponent<LayoutElement>();
                if (layout == null) layout = row.AddComponent<LayoutElement>();
                if (layout.preferredHeight <= 0f) layout.preferredHeight = rowHeight;
                layout.minHeight = Mathf.Max(30f, rowHeight - 8f);
                layout.flexibleWidth = 1f;
            }
            else
            {
                // Fallback if a future game version has no vanilla row to clone.
                GameObject rowGo = new GameObject("BHE_Key_" + configId, typeof(RectTransform));
                row = rowGo;
                row.transform.SetParent(transform, false);
                RectTransform rowRt = (RectTransform)row.transform;
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.sizeDelta = new Vector2(0f, 52f);
                LayoutElement le = rowGo.AddComponent<LayoutElement>();
                le.preferredHeight = 52f;
                le.minHeight = 52f;
                le.flexibleWidth = 1f;

                Image bg = NewImage(row.transform, "Bg", new Color(0.10f, 0.12f, 0.145f, 0.95f));
                Stretch(bg.rectTransform);
                rowLabel = NewText(row.transform, "Label", label, 16f, TextColor, TextAlignmentOptions.Left);
                RectTransform labelRt = rowLabel.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(16f, 0f);
                labelRt.offsetMax = new Vector2(-280f, 0f);
                Button button = NewButton(row.transform, "KeyButton", 240f, 34f);
                RectTransform buttonRt = (RectTransform)button.transform;
                buttonRt.anchorMin = new Vector2(1f, 0.5f);
                buttonRt.anchorMax = new Vector2(1f, 0.5f);
                buttonRt.pivot = new Vector2(1f, 0.5f);
                buttonRt.sizeDelta = new Vector2(240f, 34f);
                buttonRt.anchoredPosition = new Vector2(-14f, 0f);
                valueLabel = NewText(buttonRt, "Value", "", 15f, AccentColor,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                Stretch(valueLabel.rectTransform, 6f, 6f, 2f, 2f);
                string cfg = configId;
                TMP_Text value = valueLabel;
                button.onClick.AddListener(() =>
                {
                    _capture = cfg;
                    if (value != null) value.text = L10n.KeyPress;
                });
            }

            if (valueLabel != null)
            {
                valueLabel.text = InputKeys.GetDisplayName(configId == "open"
                    ? ModConfig.OpenPanelKey
                    : ModConfig.ToggleHudKey);
            }
            _rows.Add(new KeyRow { ConfigId = configId, Label = rowLabel, Value = valueLabel });
        }

        private Transform FindTemplateRow()
        {
            if (TemplateRoot == null) return null;
            Transform t = TemplateRoot.Find("UI_Language");
            if (t != null) return t;
            t = TemplateRoot.Find("UI_MouseSensitivity");
            if (t != null) return t;
            return TemplateRoot.childCount > 0 ? TemplateRoot.GetChild(0) : null;
        }

        /// <summary>Uses the vanilla row label font/size for our hint line so it blends in.</summary>
        private void StyleNoteLikeNative()
        {
            try
            {
                Transform template = FindTemplateRow();
                if (template == null || _noteText == null) return;
                TMP_Text templateLabel = null;
                TMP_Text[] texts = template.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null) { templateLabel = texts[i]; break; }
                }
                if (templateLabel == null) return;
                _noteText.font = templateLabel.font;
                _noteText.fontSize = Mathf.Max(12f, templateLabel.fontSize * 0.82f);
                _noteText.color = new Color(templateLabel.color.r, templateLabel.color.g, templateLabel.color.b, 0.88f);
                _noteText.alignment = TextAlignmentOptions.TopLeft;
            }
            catch { }
        }

        private void Update()
        {
            if (_capture == null) return;
            try
            {
                if (InputKeys.IsPressedThisFrame(Key.Escape))
                {
                    _capture = null;
                    Refresh();
                    return;
                }
                Key pressed = InputKeys.CapturePressedKey();
                if (pressed == Key.None) return;
                string name = InputKeys.ToName(pressed);
                if (_capture == "open")
                {
                    ModConfig.OpenPanelKey = name;
                    if (SameKey(ModConfig.ToggleHudKey, name)) ModConfig.ToggleHudKey = "None";
                }
                else
                {
                    ModConfig.ToggleHudKey = name;
                    if (SameKey(ModConfig.OpenPanelKey, name)) ModConfig.OpenPanelKey = "None";
                }
                _capture = null;
                ModConfig.NotifyChanged();
                Refresh();
            }
            catch (Exception e)
            {
                Log.Warn("Options tab key capture failed: " + e.Message);
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            _capture = null;
        }

        private void OnDestroy()
        {
            try { L10n.OnLanguageChanged -= OnLanguageChanged; } catch { }
        }

        private void OnLanguageChanged()
        {
            try
            {
                if (TabLabel != null) TabLabel.text = L10n.TabTitle;
                if (_noteText != null) _noteText.text = L10n.TabNote;
                for (int i = 0; i < _rows.Count; i++)
                {
                    KeyRow row = _rows[i];
                    if (row == null || row.Label == null) continue;
                    row.Label.text = row.ConfigId == "open" ? L10n.KeyOpenPanel : L10n.KeyToggleHud;
                }
                Refresh();
            }
            catch { }
        }

        private void Refresh()
        {
            try
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    KeyRow row = _rows[i];
                    if (row == null || row.Value == null) continue;
                    row.Value.text = InputKeys.GetDisplayName(row.ConfigId == "open"
                        ? ModConfig.OpenPanelKey
                        : ModConfig.ToggleHudKey);
                }
            }
            catch { }
        }

        private static bool SameKey(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------- widgets
        private TextMeshProUGUI NewText(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = _font;
            tmp.text = text ?? "";
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableAutoSizing = false;
            return tmp;
        }

        private Image NewImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = _white;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Button NewButton(Transform parent, string name, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = _white;
            image.color = ButtonColor;
            image.raycastTarget = true;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock block = new ColorBlock();
            block.normalColor = Color.white;
            block.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            block.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            block.selectedColor = Color.white;
            block.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.08f;
            button.colors = block;
            return button;
        }

        private static void AddHeight(GameObject go, float height)
        {
            if (go == null) return;
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;
        }

        private static void Stretch(RectTransform rt)
        {
            Stretch(rt, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(RectTransform rt, float left, float right, float bottom, float top)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // ---------------------------------------------------------------- font
        private void ResolveFont()
        {
            _font = null;
            bool zh = L10n.Language != L10n.Lang.En;
            try
            {
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                TMP_FontAsset bestCjk = null;
                TMP_FontAsset bestAscii = null;
                for (int i = 0; i < fonts.Length; i++)
                {
                    TMP_FontAsset f = fonts[i];
                    if (f == null) continue;
                    if (HasAscii(f) && HasCjk(f))
                    {
                        if (f.name.IndexOf("ResourceHan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            f.name.IndexOf("HanRounded", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _font = f;
                            break;
                        }
                        if (bestCjk == null) bestCjk = f;
                    }
                    if (bestAscii == null && HasAscii(f)) bestAscii = f;
                }
                if (_font == null && zh) _font = bestCjk;
                if (_font == null) _font = bestAscii;
            }
            catch { }
            if (_font == null)
            {
                try { _font = TMP_Settings.defaultFontAsset; } catch { }
            }
        }

        private static bool HasAscii(TMP_FontAsset font)
        {
            try { return font.HasCharacter('A') && font.HasCharacter('0'); } catch { return false; }
        }

        private static bool HasCjk(TMP_FontAsset font)
        {
            try { return font.HasCharacter('水') && font.HasCharacter('设'); } catch { return false; }
        }

        private static Sprite MakeWhiteSprite()
        {
            try
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                tex.name = "BHE_White";
                Color32[] pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();
                return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            }
            catch
            {
                return null;
            }
        }
    }
}
