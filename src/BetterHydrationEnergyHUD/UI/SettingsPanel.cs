using System;
using System.Collections.Generic;
using BetterHydrationEnergyHUD.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BetterHydrationEnergyHUD.UI
{
    /// <summary>
    /// In-game quick settings panel (opened with the configurable hotkey, F10 by default).
    ///
    /// Deliberately simple and robust implementation:
    ///   * plain runtime-generated white sprite tinted for every background (no fancy SDF sprites),
    ///   * Unity's own Slider / Button controls with explicit fill/handle/target graphics,
    ///   * explicit RectTransform anchors for rows instead of nested layout groups,
    ///   * the game's own TMP font resolved with an ASCII/CJK coverage check and fallbacks.
    ///
    /// The official options page (Integration/OfficialOptions) mirrors the same settings inside
    /// the vanilla options screen; this panel exists for quick in-game tweaking.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        // ------------------------------------------------------------- palette
        private static readonly Color ScrimColor = new Color(0f, 0f, 0f, 0.62f);
        private static readonly Color PanelColor = new Color(0.052f, 0.066f, 0.082f, 0.99f);
        private static readonly Color HeaderColor = new Color(0.075f, 0.095f, 0.118f, 1f);
        private static readonly Color CardColor = new Color(0.088f, 0.106f, 0.130f, 1f);
        private static readonly Color RowColor = new Color(0.105f, 0.125f, 0.152f, 1f);
        private static readonly Color TextColor = new Color(0.93f, 0.95f, 0.975f, 1f);
        private static readonly Color SubTextColor = new Color(0.66f, 0.73f, 0.80f, 1f);
        private static readonly Color MutedTextColor = new Color(0.50f, 0.565f, 0.635f, 1f);
        private static readonly Color AccentColor = new Color(0.29f, 0.76f, 0.98f, 1f);
        private static readonly Color AccentDark = new Color(0.10f, 0.28f, 0.40f, 1f);
        private static readonly Color ToggleOnColor = new Color(0.13f, 0.55f, 0.82f, 1f);
        private static readonly Color ToggleOffColor = new Color(0.18f, 0.21f, 0.25f, 1f);
        private static readonly Color TrackColor = new Color(0.175f, 0.205f, 0.24f, 1f);
        private static readonly Color ButtonColor = new Color(0.14f, 0.17f, 0.205f, 1f);

        private const float PanelWidth = 570f;
        private const float HeaderHeight = 74f;
        private const float FooterHeight = 58f;
        private const float Pad = 18f;
        private const float RowGap = 8f;
        private const float PanelMargin = 18f;

        // -------------------------------------------------------------- state
        public static SettingsPanel Instance { get; private set; }

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _panelRect;
        private RectTransform _content;
        private ScrollRect _scroll;
        private TextMeshProUGUI _statusText;
        private GameObject _toastGo;
        private TextMeshProUGUI _toastText;
        private float _toastUntil;
        private TMP_FontAsset _font;
        private Sprite _white;

        private bool _visible;
        private bool _built;
        private float _alpha;
        private float _targetAlpha;
        private bool _deactivateWhenHidden;
        private bool _savedCursorVisible;
        private CursorLockMode _savedCursorLock = CursorLockMode.None;
        private float _savedTimeScale = 1f;

        private string _capturingConfig;
        private Action<string> _captureSink;
        private int _rebuildFrame = -1;

        private readonly List<Action> _refreshers = new List<Action>();

        // --------------------------------------------------------------- API
        public static SettingsPanel Ensure()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("BHE_SettingsUI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.AddComponent<SettingsPanel>();
        }

        public bool IsVisible
        {
            get { return _visible; }
        }

        public bool IsCapturingKey
        {
            get { return _capturingConfig != null; }
        }

        public void Show()
        {
            try
            {
                if (!_built) Build();
                if (_canvas == null) return;
                if (!_visible)
                {
                    _visible = true;
                    ModRuntime.SettingsOpen = true;
                    _canvas.gameObject.SetActive(true);
                    _canvasGroup.blocksRaycasts = true;
                    _canvasGroup.interactable = true;
                    _alpha = 0f;
                    _canvasGroup.alpha = 0f;
                    _targetAlpha = 1f;
                    _deactivateWhenHidden = false;
                    TakeOverInput();
                }
                Rebuild();
            }
            catch (Exception e)
            {
                Log.Error("SettingsPanel.Show failed", e);
            }
        }

        public void Hide()
        {
            try
            {
                if (!_visible) return;
                _visible = false;
                ModRuntime.SettingsOpen = false;
                _capturingConfig = null;
                _captureSink = null;
                _targetAlpha = 0f;
                _deactivateWhenHidden = true;
                if (_canvasGroup != null)
                {
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                }
                RestoreInput();
            }
            catch (Exception e)
            {
                Log.Warn("SettingsPanel.Hide failed: " + e.Message);
            }
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }

        public void Rebuild()
        {
            if (!_built || _content == null) return;
            if (_rebuildFrame == Time.frameCount) return;
            _rebuildFrame = Time.frameCount;
            try
            {
                ClearContent();
                _refreshers.Clear();
                BuildContent();
            }
            catch (Exception e)
            {
                Log.Error("SettingsPanel.Rebuild failed", e);
            }
        }

        public void ShowSavedToast()
        {
            try
            {
                if (_toastGo == null)
                {
                    _toastGo = new GameObject("BHE_Toast", typeof(RectTransform));
                    RectTransform rt = (RectTransform)_toastGo.transform;
                    rt.SetParent(_panelRect, false);
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(220f, 30f);
                    rt.anchoredPosition = new Vector2(0f, -HeaderHeight - 6f);
                    Image bg = _toastGo.AddComponent<Image>();
                    bg.sprite = WhiteSprite;
                    bg.color = new Color(0.08f, 0.30f, 0.42f, 0.98f);
                    bg.raycastTarget = false;
                    _toastText = NewText(_toastGo.transform, "Text", L10n.Saved, 16f, TextColor,
                        TextAlignmentOptions.Center, FontStyles.Bold);
                    Stretch(_toastText.rectTransform, 6f, 6f, 2f, 2f);
                }
                _toastGo.SetActive(true);
                _toastUntil = Time.unscaledTime + 1.2f;
            }
            catch (Exception e)
            {
                Log.Warn("Toast failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------- unity
        /// <summary>
        /// The game locks/hides the OS cursor during gameplay, which made the panel impossible to
        /// click (the cursor stayed under the game's aim input). While the panel is open we take the
        /// cursor over every frame from LateUpdate (so we win against the game's own cursor update)
        /// and freeze the game clock. Both are restored when the panel closes.
        /// </summary>
        private void TakeOverInput()
        {
            try
            {
                _savedCursorVisible = Cursor.visible;
                _savedCursorLock = Cursor.lockState;
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                // Official API: registers this GameObject as a gameplay input blocker, so clicks /
                // keys used inside the panel are not queued by the character input system.
                try { InputManager.DisableInput(gameObject); } catch { }
            }
            catch { }
        }

        private void RestoreInput()
        {
            try
            {
                try { InputManager.ActiveInput(gameObject); } catch { }
                Cursor.visible = _savedCursorVisible;
                Cursor.lockState = _savedCursorLock;
                Time.timeScale = _savedTimeScale;
            }
            catch { }
        }

        private void LateUpdate()
        {
            if (!_visible) return;
            try
            {
                // Re-apply every frame: the game's character input keeps re-locking the cursor.
                if (!Cursor.visible) Cursor.visible = true;
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                if (Mathf.Approximately(Time.timeScale, 0f) == false && _targetAlpha > 0.5f) Time.timeScale = 0f;
            }
            catch { }
        }

        private void Awake()
        {
            Instance = this;
            try { Build(); }
            catch (Exception e) { Log.Error("SettingsPanel build failed", e); }
        }

        private void OnEnable()
        {
            try { L10n.OnLanguageChanged += OnLanguageChanged; } catch { }
        }

        private void OnDisable()
        {
            try { L10n.OnLanguageChanged -= OnLanguageChanged; } catch { }
        }

        private void OnDestroy()
        {
            try { L10n.OnLanguageChanged -= OnLanguageChanged; } catch { }
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            try
            {
                if (Mathf.Abs(_alpha - _targetAlpha) > 0.002f)
                {
                    _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, Time.unscaledDeltaTime * 8f);
                    if (_canvasGroup != null) _canvasGroup.alpha = _alpha;
                }
                else if (_targetAlpha <= 0.002f && _deactivateWhenHidden && _canvas != null && _canvas.gameObject.activeSelf)
                {
                    _canvas.gameObject.SetActive(false);
                }

                if (!_visible) return;

                if (_toastGo != null && _toastGo.activeSelf && Time.unscaledTime > _toastUntil)
                {
                    _toastGo.SetActive(false);
                }

                if (_capturingConfig == null && InputKeys.IsPressedThisFrame(Key.Escape))
                {
                    Hide();
                    return;
                }

                HandleCapture();
            }
            catch (Exception e)
            {
                Log.Warn("SettingsPanel.Update: " + e.Message);
            }
        }

        private void HandleCapture()
        {
            if (_capturingConfig == null) return;

            if (InputKeys.IsPressedThisFrame(Key.Escape))
            {
                _capturingConfig = null;
                _captureSink = null;
                RefreshBindings();
                return;
            }

            Key pressed = InputKeys.CapturePressedKey();
            if (pressed == Key.None) return;

            string keyName = InputKeys.ToName(pressed);
            string config = _capturingConfig;
            Action<string> sink = _captureSink;
            _capturingConfig = null;
            _captureSink = null;

            try
            {
                if (config == "open")
                {
                    ModConfig.OpenPanelKey = keyName;
                    if (SameKey(ModConfig.ToggleHudKey, keyName)) ModConfig.ToggleHudKey = "None";
                }
                else if (config == "toggle")
                {
                    ModConfig.ToggleHudKey = keyName;
                    if (SameKey(ModConfig.OpenPanelKey, keyName)) ModConfig.OpenPanelKey = "None";
                }
                ModConfig.NotifyChanged();
            }
            catch (Exception e)
            {
                Log.Warn("Key capture commit failed: " + e.Message);
            }

            if (sink != null)
            {
                try { sink(keyName); } catch { }
            }
            RefreshBindings();
            ShowSavedToast();
        }

        private static bool SameKey(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------- build
        private void Build()
        {
            if (_built) return;
            _white = MakeWhiteSprite();
            ResolveFont();

            GameObject canvasGo = new GameObject("BHE_Settings_Canvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasGo);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32760;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            _alpha = 0f;
            _canvasGroup.alpha = 0f;

            // Full screen scrim (dim + click outside to close).
            Image scrim = NewImage(canvasRect, "Scrim", ScrimColor, true, true);
            Button scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.targetGraphic = scrim;
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(Hide);

            // Thin outline behind the panel for depth.
            Image outline = NewImage(canvasRect, "PanelOutline", new Color(0.33f, 0.62f, 0.86f, 0.28f), false, false);
            ConfigurePanelRect(outline.rectTransform, 2f);

            Image panelImage = NewImage(canvasRect, "Panel", PanelColor, true, false);
            _panelRect = panelImage.rectTransform;
            ConfigurePanelRect(_panelRect, 0f);

            BuildHeader();
            BuildScrollArea();
            BuildFooter();

            canvasGo.SetActive(false);
            _built = true;
            _visible = false;
            Log.Info("Settings panel built (font=" + (_font != null ? _font.name : "<null>") + ")");
        }

        private void ConfigurePanelRect(RectTransform rt, float expand)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(PanelWidth + expand * 2f, -(PanelMargin * 2f) + expand * 2f);
            rt.anchoredPosition = new Vector2(-PanelMargin + expand, 0f);
        }

        private void BuildHeader()
        {
            Image header = NewImage(_panelRect, "Header", HeaderColor, false, false);
            RectTransform rt = header.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, HeaderHeight);
            rt.anchoredPosition = Vector2.zero;

            Image accent = NewImage(rt, "Accent", AccentColor, false, false);
            RectTransform accentRt = accent.rectTransform;
            accentRt.anchorMin = new Vector2(0f, 0.5f);
            accentRt.anchorMax = new Vector2(0f, 0.5f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.sizeDelta = new Vector2(4f, 34f);
            accentRt.anchoredPosition = new Vector2(Pad, 0f);

            TextMeshProUGUI title = NewText(rt, "Title", L10n.Title, 24f, TextColor,
                TextAlignmentOptions.Left, FontStyles.Bold);
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.sizeDelta = new Vector2(-(Pad * 2f + 96f), 30f);
            titleRt.anchoredPosition = new Vector2(Pad + 12f, 9f);

            TextMeshProUGUI version = NewText(rt, "Version", "v" + ModRuntime.ModVersion + "  ·  " + L10n.Subtitle,
                13f, SubTextColor, TextAlignmentOptions.Left);
            RectTransform versionRt = version.rectTransform;
            versionRt.anchorMin = new Vector2(0f, 0.5f);
            versionRt.anchorMax = new Vector2(1f, 0.5f);
            versionRt.pivot = new Vector2(0f, 0.5f);
            versionRt.sizeDelta = new Vector2(-(Pad * 2f + 96f), 20f);
            versionRt.anchoredPosition = new Vector2(Pad + 12f, -16f);

            Button close = NewButton(rt, "Close", "X", false, Hide);
            RectTransform closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(34f, 34f);
            closeRt.anchoredPosition = new Vector2(-Pad, 0f);

            Image divider = NewImage(_panelRect, "Divider", new Color(1f, 1f, 1f, 0.08f), false, false);
            RectTransform dividerRt = divider.rectTransform;
            dividerRt.anchorMin = new Vector2(0f, 1f);
            dividerRt.anchorMax = new Vector2(1f, 1f);
            dividerRt.pivot = new Vector2(0.5f, 1f);
            dividerRt.sizeDelta = new Vector2(-Pad * 2f, 1f);
            dividerRt.anchoredPosition = new Vector2(0f, -HeaderHeight);
        }

        private void BuildScrollArea()
        {
            GameObject scrollRoot = new GameObject("Scroll", typeof(RectTransform));
            RectTransform scrollRt = (RectTransform)scrollRoot.transform;
            scrollRt.SetParent(_panelRect, false);
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(Pad, FooterHeight + 6f);
            scrollRt.offsetMax = new Vector2(-Pad, -(HeaderHeight + 6f));
            _scroll = scrollRoot.AddComponent<ScrollRect>();

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
            RectTransform viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(scrollRt, false);
            Stretch(viewport);
            viewportGo.AddComponent<RectMask2D>();
            Image viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.sprite = WhiteSprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0.002f);
            viewportImage.raycastTarget = true;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            _content = (RectTransform)contentGo.transform;
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = Vector2.zero;
            _content.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowGap;
            layout.padding = new RectOffset(2, 2, 2, 14);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll.viewport = viewport;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 32f;
            _scroll.inertia = true;
        }

        private void BuildFooter()
        {
            Image footer = NewImage(_panelRect, "Footer", HeaderColor, false, false);
            RectTransform rt = footer.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, FooterHeight);
            rt.anchoredPosition = Vector2.zero;

            Button reset = NewButton(rt, "Reset", L10n.ButtonReset, false, () =>
            {
                try
                {
                    ModConfig.ResetToDefaults();
                    Rebuild();
                    ShowSavedToast();
                }
                catch (Exception e)
                {
                    Log.Warn("Reset failed: " + e.Message);
                }
            });
            RectTransform resetRt = (RectTransform)reset.transform;
            resetRt.anchorMin = new Vector2(0f, 0.5f);
            resetRt.anchorMax = new Vector2(0f, 0.5f);
            resetRt.pivot = new Vector2(0f, 0.5f);
            resetRt.sizeDelta = new Vector2(132f, 32f);
            resetRt.anchoredPosition = new Vector2(Pad, 0f);

            _statusText = NewText(rt, "Status", L10n.HintAutosave, 13f, MutedTextColor,
                TextAlignmentOptions.Center);
            RectTransform statusRt = _statusText.rectTransform;
            statusRt.anchorMin = new Vector2(0f, 0.5f);
            statusRt.anchorMax = new Vector2(1f, 0.5f);
            statusRt.pivot = new Vector2(0.5f, 0.5f);
            statusRt.sizeDelta = new Vector2(-(Pad * 2f + 132f + 84f), 20f);
            statusRt.anchoredPosition = Vector2.zero;

            Button done = NewButton(rt, "Done", L10n.ButtonClose, true, Hide);
            RectTransform doneRt = (RectTransform)done.transform;
            doneRt.anchorMin = new Vector2(1f, 0.5f);
            doneRt.anchorMax = new Vector2(1f, 0.5f);
            doneRt.pivot = new Vector2(1f, 0.5f);
            doneRt.sizeDelta = new Vector2(76f, 32f);
            doneRt.anchoredPosition = new Vector2(-Pad, 0f);
        }

        // ------------------------------------------------------------ content
        private void BuildContent()
        {
            if (_content == null) return;

            AddSectionTitle(L10n.SectionDisplay);
            AddToggleRow(L10n.ShowWaterTop, ModConfig.ShowWaterTop,
                v => { ModConfig.ShowWaterTop = v; ModConfig.NotifyChanged(); },
                () => ModConfig.ShowWaterTop = ModConfig.Defaults.ShowWaterTop);
            AddToggleRow(L10n.ShowWaterBottom, ModConfig.ShowWaterBottom,
                v => { ModConfig.ShowWaterBottom = v; ModConfig.NotifyChanged(); },
                () => ModConfig.ShowWaterBottom = ModConfig.Defaults.ShowWaterBottom);
            AddToggleRow(L10n.ShowEnergyTop, ModConfig.ShowHungerTop,
                v => { ModConfig.ShowHungerTop = v; ModConfig.NotifyChanged(); },
                () => ModConfig.ShowHungerTop = ModConfig.Defaults.ShowHungerTop);
            AddToggleRow(L10n.ShowEnergyBottom, ModConfig.ShowHungerBottom,
                v => { ModConfig.ShowHungerBottom = v; ModConfig.NotifyChanged(); },
                () => ModConfig.ShowHungerBottom = ModConfig.Defaults.ShowHungerBottom);

            AddSectionTitle(L10n.SectionPosition);
            AddInfo(L10n.T("options_note_position"), 13f, MutedTextColor);
            AddOffsetGroup(L10n.StatWaterName + "  ·  " + L10n.Above,
                () => Mathf.RoundToInt(ModConfig.WaterTopOffsetX), v => { ModConfig.WaterTopOffsetX = v; ModConfig.NotifyChanged(); },
                () => Mathf.RoundToInt(ModConfig.WaterTopOffset), v => { ModConfig.WaterTopOffset = v; ModConfig.NotifyChanged(); },
                () => ModConfig.WaterTopOffsetX = ModConfig.Defaults.WaterTopOffsetX,
                () => ModConfig.WaterTopOffset = ModConfig.Defaults.WaterTopOffset);
            AddOffsetGroup(L10n.StatWaterName + "  ·  " + L10n.Below,
                () => Mathf.RoundToInt(ModConfig.WaterBottomOffsetX), v => { ModConfig.WaterBottomOffsetX = v; ModConfig.NotifyChanged(); },
                () => Mathf.RoundToInt(ModConfig.WaterBottomOffset), v => { ModConfig.WaterBottomOffset = v; ModConfig.NotifyChanged(); },
                () => ModConfig.WaterBottomOffsetX = ModConfig.Defaults.WaterBottomOffsetX,
                () => ModConfig.WaterBottomOffset = ModConfig.Defaults.WaterBottomOffset);
            AddOffsetGroup(L10n.StatEnergyName + "  ·  " + L10n.Above,
                () => Mathf.RoundToInt(ModConfig.HungerTopOffsetX), v => { ModConfig.HungerTopOffsetX = v; ModConfig.NotifyChanged(); },
                () => Mathf.RoundToInt(ModConfig.HungerTopOffset), v => { ModConfig.HungerTopOffset = v; ModConfig.NotifyChanged(); },
                () => ModConfig.HungerTopOffsetX = ModConfig.Defaults.HungerTopOffsetX,
                () => ModConfig.HungerTopOffset = ModConfig.Defaults.HungerTopOffset);
            AddOffsetGroup(L10n.StatEnergyName + "  ·  " + L10n.Below,
                () => Mathf.RoundToInt(ModConfig.HungerBottomOffsetX), v => { ModConfig.HungerBottomOffsetX = v; ModConfig.NotifyChanged(); },
                () => Mathf.RoundToInt(ModConfig.HungerBottomOffset), v => { ModConfig.HungerBottomOffset = v; ModConfig.NotifyChanged(); },
                () => ModConfig.HungerBottomOffsetX = ModConfig.Defaults.HungerBottomOffsetX,
                () => ModConfig.HungerBottomOffset = ModConfig.Defaults.HungerBottomOffset);

            AddSectionTitle(L10n.SectionStyle);
            AddSliderRow(L10n.FontSize, 12, 48, () => ModConfig.HudFontSize,
                v => { ModConfig.HudFontSize = v; ModConfig.NotifyChanged(); },
                () => ModConfig.HudFontSize = ModConfig.Defaults.HudFontSize);
            AddCycleRow(L10n.Decimals, new[] { "0", "1", "2" }, () => ModConfig.Decimals,
                v => { ModConfig.Decimals = v; ModConfig.NotifyChanged(); },
                () => ModConfig.Decimals = ModConfig.Defaults.Decimals);
            AddToggleRow(L10n.UsePercent, ModConfig.UsePercent,
                v => { ModConfig.UsePercent = v; ModConfig.NotifyChanged(); },
                () => ModConfig.UsePercent = ModConfig.Defaults.UsePercent);
            AddToggleRow(L10n.HideDiffWhenFull, ModConfig.HideDiffWhenFull,
                v => { ModConfig.HideDiffWhenFull = v; ModConfig.NotifyChanged(); },
                () => ModConfig.HideDiffWhenFull = ModConfig.Defaults.HideDiffWhenFull);
            AddToggleRow(L10n.LowWarning, ModConfig.LowValueWarning,
                v => { ModConfig.LowValueWarning = v; ModConfig.NotifyChanged(); },
                () => ModConfig.LowValueWarning = ModConfig.Defaults.LowValueWarning);
            AddSliderRow(L10n.LowThreshold, 0, 90, () => ModConfig.LowValueThreshold,
                v => { ModConfig.LowValueThreshold = v; ModConfig.NotifyChanged(); },
                () => ModConfig.LowValueThreshold = ModConfig.Defaults.LowValueThreshold);
            AddToggleRow(L10n.FontBold, ModConfig.FontBold,
                v => { ModConfig.FontBold = v; ModConfig.NotifyChanged(); },
                () => ModConfig.FontBold = ModConfig.Defaults.FontBold);
            AddToggleRow(L10n.TextShadow, ModConfig.TextShadow,
                v => { ModConfig.TextShadow = v; ModConfig.NotifyChanged(); },
                () => ModConfig.TextShadow = ModConfig.Defaults.TextShadow);

            AddSectionTitle(L10n.SectionHotkeys);
            AddKeyRow(L10n.KeyOpenPanel, "open", () => ModConfig.OpenPanelKey,
                () => ModConfig.OpenPanelKey = ModConfig.Defaults.OpenPanelKey);
            AddKeyRow(L10n.KeyToggleHud, "toggle", () => ModConfig.ToggleHudKey,
                () => ModConfig.ToggleHudKey = ModConfig.Defaults.ToggleHudKey);
            AddInfo(L10n.T("options_note_f10"), 12f, MutedTextColor);

            AddSectionTitle(L10n.SectionAbout);
            AddInfo(L10n.Format("about_format", L10n.Title, ModRuntime.ModVersion, ModRuntime.Author), 14f, SubTextColor);
        }

        private void ClearContent()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Transform child = _content.GetChild(i);
                if (child == null) continue;
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        // ------------------------------------------------------------ widgets
        private RectTransform NewRow(string name, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(_content, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;
            return rt;
        }

        private void AddSectionTitle(string text)
        {
            RectTransform row = NewRow("Section_" + text, 34f);
            Image bar = NewImage(row, "Bar", AccentColor, false, false);
            RectTransform barRt = bar.rectTransform;
            barRt.anchorMin = new Vector2(0f, 0.5f);
            barRt.anchorMax = new Vector2(0f, 0.5f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.sizeDelta = new Vector2(3f, 18f);
            barRt.anchoredPosition = new Vector2(2f, 0f);

            TextMeshProUGUI label = NewText(row, "Title", text, 17f, AccentColor,
                TextAlignmentOptions.Left, FontStyles.Bold);
            RectTransform rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 0f);
            rt.offsetMax = Vector2.zero;
        }

        private void AddToggleRow(string label, bool initial, Action<bool> onChanged, Action reset = null)
        {
            RectTransform row = NewRow("Toggle_" + label, 44f);
            Image bg = NewImage(row, "Bg", RowColor, false, false);
            Stretch(bg.rectTransform);

            TextMeshProUGUI text = NewText(row, "Label", label, 16f, TextColor, TextAlignmentOptions.Left);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14f, 0f);
            textRt.offsetMax = new Vector2(-152f, 0f);

            bool value = initial;
            Button button = NewButton(row, "Toggle", value ? L10n.OnText : L10n.OffText, value, null);
            RectTransform buttonRt = (RectTransform)button.transform;
            buttonRt.anchorMin = new Vector2(1f, 0.5f);
            buttonRt.anchorMax = new Vector2(1f, 0.5f);
            buttonRt.pivot = new Vector2(1f, 0.5f);
            buttonRt.sizeDelta = new Vector2(88f, 30f);
            buttonRt.anchoredPosition = new Vector2(-52f, 0f);
            TextMeshProUGUI buttonText = FindChildText(button.transform);
            Image buttonImage = button.GetComponent<Image>();

            button.onClick.AddListener(() =>
            {
                value = !value;
                if (buttonText != null) buttonText.text = value ? L10n.OnText : L10n.OffText;
                if (buttonImage != null) buttonImage.color = value ? ToggleOnColor : ToggleOffColor;
                try { onChanged(value); } catch (Exception e) { Log.Warn("Toggle failed: " + e.Message); }
                ShowSavedToast();
            });
            _refreshers.Add(() =>
            {
                if (buttonText != null) buttonText.text = value ? L10n.OnText : L10n.OffText;
                if (buttonImage != null) buttonImage.color = value ? ToggleOnColor : ToggleOffColor;
            });
            AddResetButton(row, reset, false);
        }

        private void AddSliderRow(string label, int min, int max, Func<int> getter, Action<int> setter,
            Action reset = null)
        {
            RectTransform row = NewRow("Slider_" + label, 64f);
            Image bg = NewImage(row, "Bg", RowColor, false, false);
            Stretch(bg.rectTransform);

            TextMeshProUGUI text = NewText(row, "Label", label, 15f, TextColor, TextAlignmentOptions.Left);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0f, 1f);
            textRt.sizeDelta = new Vector2(-210f, 24f);
            textRt.anchoredPosition = new Vector2(14f, -4f);

            TextMeshProUGUI valueText = NewText(row, "Value", "", 15f, AccentColor,
                TextAlignmentOptions.Right, FontStyles.Bold);
            RectTransform valueRt = valueText.rectTransform;
            valueRt.anchorMin = new Vector2(1f, 1f);
            valueRt.anchorMax = new Vector2(1f, 1f);
            valueRt.pivot = new Vector2(1f, 1f);
            valueRt.sizeDelta = new Vector2(96f, 24f);
            valueRt.anchoredPosition = new Vector2(-64f, -4f);

            int initial;
            try { initial = Mathf.Clamp(getter(), min, max); }
            catch { initial = min; }
            valueText.text = initial.ToString();

            Slider slider = NewSlider(row, min, max, initial, v =>
            {
                valueText.text = v.ToString();
                try { setter(v); } catch (Exception e) { Log.Warn("Slider failed: " + e.Message); }
                ShowSavedToast();
            });
            RectTransform sliderRt = (RectTransform)slider.transform;
            sliderRt.anchorMin = new Vector2(0f, 0f);
            sliderRt.anchorMax = new Vector2(1f, 0f);
            sliderRt.pivot = new Vector2(0.5f, 0f);
            sliderRt.sizeDelta = new Vector2(-28f, 24f);
            sliderRt.anchoredPosition = new Vector2(0f, 8f);

            _refreshers.Add(() =>
            {
                int v = initial;
                try { v = Mathf.Clamp(getter(), min, max); } catch { }
                if (slider != null) slider.SetValueWithoutNotify(v);
                if (valueText != null) valueText.text = v.ToString();
            });
            AddResetButton(row, reset, true);
        }

        private void AddOffsetGroup(string title, Func<int> getX, Action<int> setX, Func<int> getY, Action<int> setY,
            Action resetX = null, Action resetY = null)
        {
            RectTransform card = NewRow("Offset_" + title, 152f);
            Image bg = NewImage(card, "Bg", CardColor, false, false);
            Stretch(bg.rectTransform);
            Image accent = NewImage(card, "Accent", AccentColor, false, false);
            RectTransform accentRt = accent.rectTransform;
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.sizeDelta = new Vector2(0f, 2f);
            accentRt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI label = NewText(card, "GroupTitle", title, 14f, SubTextColor,
                TextAlignmentOptions.Left, FontStyles.Bold);
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.sizeDelta = new Vector2(-28f, 20f);
            labelRt.anchoredPosition = new Vector2(14f, -6f);

            AddInnerSlider(card, L10n.OffsetX, -300, 300, getX, setX, -30f, resetX);
            AddInnerSlider(card, L10n.OffsetY, -300, 300, getY, setY, -88f, resetY);
        }

        private void AddInnerSlider(RectTransform card, string label, int min, int max,
            Func<int> getter, Action<int> setter, float topOffset, Action reset = null)
        {
            TextMeshProUGUI text = NewText(card, "Label_" + label, label, 14f, TextColor, TextAlignmentOptions.Left);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0f, 1f);
            textRt.sizeDelta = new Vector2(-160f, 20f);
            textRt.anchoredPosition = new Vector2(16f, topOffset);

            TextMeshProUGUI valueText = NewText(card, "Value_" + label, "", 14f, AccentColor,
                TextAlignmentOptions.Right, FontStyles.Bold);
            RectTransform valueRt = valueText.rectTransform;
            valueRt.anchorMin = new Vector2(1f, 1f);
            valueRt.anchorMax = new Vector2(1f, 1f);
            valueRt.pivot = new Vector2(1f, 1f);
            valueRt.sizeDelta = new Vector2(92f, 20f);
            valueRt.anchoredPosition = new Vector2(-62f, topOffset);

            int initial;
            try { initial = Mathf.Clamp(getter(), min, max); }
            catch { initial = min; }
            valueText.text = initial.ToString();

            Slider slider = NewSlider(card, min, max, initial, v =>
            {
                valueText.text = v.ToString();
                try { setter(v); } catch (Exception e) { Log.Warn("Slider failed: " + e.Message); }
            });
            RectTransform sliderRt = (RectTransform)slider.transform;
            sliderRt.anchorMin = new Vector2(0f, 1f);
            sliderRt.anchorMax = new Vector2(1f, 1f);
            sliderRt.pivot = new Vector2(0.5f, 1f);
            sliderRt.sizeDelta = new Vector2(-32f, 22f);
            sliderRt.anchoredPosition = new Vector2(0f, topOffset - 22f);

            _refreshers.Add(() =>
            {
                int v = initial;
                try { v = Mathf.Clamp(getter(), min, max); } catch { }
                if (slider != null) slider.SetValueWithoutNotify(v);
                if (valueText != null) valueText.text = v.ToString();
            });

            Button resetButton = AddResetButton(card, reset, true);
            if (resetButton != null)
            {
                RectTransform resetRt = (RectTransform)resetButton.transform;
                resetRt.anchorMin = new Vector2(1f, 1f);
                resetRt.anchorMax = new Vector2(1f, 1f);
                resetRt.pivot = new Vector2(1f, 1f);
                resetRt.sizeDelta = new Vector2(42f, 20f);
                resetRt.anchoredPosition = new Vector2(-14f, topOffset + 2f);
            }
        }

        private void AddCycleRow(string label, string[] options, Func<int> getIndex, Action<int> setIndex,
            Action reset = null)
        {
            RectTransform row = NewRow("Cycle_" + label, 44f);
            Image bg = NewImage(row, "Bg", RowColor, false, false);
            Stretch(bg.rectTransform);

            TextMeshProUGUI text = NewText(row, "Label", label, 16f, TextColor, TextAlignmentOptions.Left);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14f, 0f);
            textRt.offsetMax = new Vector2(-152f, 0f);

            Button button = NewButton(row, "Cycle", "", false, null);
            RectTransform buttonRt = (RectTransform)button.transform;
            buttonRt.anchorMin = new Vector2(1f, 0.5f);
            buttonRt.anchorMax = new Vector2(1f, 0.5f);
            buttonRt.pivot = new Vector2(1f, 0.5f);
            buttonRt.sizeDelta = new Vector2(88f, 30f);
            buttonRt.anchoredPosition = new Vector2(-52f, 0f);
            TextMeshProUGUI valueText = FindChildText(button.transform);

            int index;
            try { index = Mathf.Clamp(getIndex(), 0, options.Length - 1); }
            catch { index = 0; }
            if (valueText != null) valueText.text = options.Length > 0 ? options[index] : "";

            button.onClick.AddListener(() =>
            {
                try
                {
                    index = (index + 1) % Mathf.Max(1, options.Length);
                    setIndex(index);
                    if (valueText != null) valueText.text = options[index];
                    ShowSavedToast();
                }
                catch (Exception e)
                {
                    Log.Warn("Cycle failed: " + e.Message);
                }
            });
            _refreshers.Add(() =>
            {
                try { index = Mathf.Clamp(getIndex(), 0, options.Length - 1); } catch { }
                if (valueText != null && options.Length > 0) valueText.text = options[index];
            });
            AddResetButton(row, reset, false);
        }

        private void AddKeyRow(string label, string configId, Func<string> getKey, Action reset = null)
        {
            RectTransform row = NewRow("Key_" + label, 44f);
            Image bg = NewImage(row, "Bg", RowColor, false, false);
            Stretch(bg.rectTransform);

            TextMeshProUGUI text = NewText(row, "Label", label, 16f, TextColor, TextAlignmentOptions.Left);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14f, 0f);
            textRt.offsetMax = new Vector2(-272f, 0f);

            Button button = NewButton(row, "Key", "", false, null);
            RectTransform buttonRt = (RectTransform)button.transform;
            buttonRt.anchorMin = new Vector2(1f, 0.5f);
            buttonRt.anchorMax = new Vector2(1f, 0.5f);
            buttonRt.pivot = new Vector2(1f, 0.5f);
            buttonRt.sizeDelta = new Vector2(200f, 30f);
            buttonRt.anchoredPosition = new Vector2(-52f, 0f);

            TextMeshProUGUI valueText = FindChildText(button.transform);
            if (valueText != null) valueText.text = InputKeys.GetDisplayName(getKey());

            button.onClick.AddListener(() =>
            {
                _capturingConfig = configId;
                if (valueText != null) valueText.text = L10n.KeyPress;
            });
            _refreshers.Add(() =>
            {
                if (_capturingConfig == configId) return;
                if (valueText != null) valueText.text = InputKeys.GetDisplayName(getKey());
            });
            AddResetButton(row, reset, false);
        }

        private void AddInfo(string text, float size, Color color)
        {
            float height = Mathf.Max(22f, 18f * Mathf.CeilToInt(Mathf.Max(1f, text.Length / 46f)));
            RectTransform row = NewRow("Info", height + 8f);
            TextMeshProUGUI label = NewText(row, "Text", text, size, color, TextAlignmentOptions.TopLeft);
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            RectTransform rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 0f);
            rt.offsetMax = new Vector2(-12f, -2f);
        }

        private void RefreshBindings()
        {
            for (int i = 0; i < _refreshers.Count; i++)
            {
                try { _refreshers[i](); } catch { }
            }
        }

        // ----------------------------------------------------------- controls
        private Slider NewSlider(Transform parent, int min, int max, int value, Action<int> onChanged)
        {
            GameObject go = new GameObject("Slider", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Slider slider = go.AddComponent<Slider>();

            Image background = NewImage(rt, "Background", TrackColor, true, true);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            RectTransform fillAreaRt = (RectTransform)fillArea.transform;
            fillAreaRt.SetParent(rt, false);
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(6f, 7f);
            fillAreaRt.offsetMax = new Vector2(-6f, -7f);

            Image fill = NewImage(fillAreaRt, "Fill", AccentColor, false, true);
            RectTransform fillRt = fill.rectTransform;

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            RectTransform handleAreaRt = (RectTransform)handleArea.transform;
            handleAreaRt.SetParent(rt, false);
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(8f, 0f);
            handleAreaRt.offsetMax = new Vector2(-8f, 0f);

            GameObject handleGo = new GameObject("Handle", typeof(RectTransform));
            RectTransform handleRt = (RectTransform)handleGo.transform;
            handleRt.SetParent(handleAreaRt, false);
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(18f, 18f);
            Image handleImage = handleGo.AddComponent<Image>();
            handleImage.sprite = WhiteSprite;
            handleImage.color = new Color(0.95f, 0.97f, 1f, 1f);
            handleImage.raycastTarget = true;

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = Mathf.Clamp(value, min, max);

            int last = Mathf.Clamp(value, min, max);
            slider.onValueChanged.AddListener(v =>
            {
                int i = Mathf.RoundToInt(v);
                if (i == last) return;
                last = i;
                try { if (onChanged != null) onChanged(i); } catch (Exception e) { Log.Warn("Slider callback failed: " + e.Message); }
            });
            return slider;
        }

        private Button NewButton(Transform parent, string name, string label, bool primary, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = primary ? AccentDark : ButtonColor;
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
            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    try { onClick(); } catch (Exception e) { Log.Warn("Button failed: " + e.Message); }
                });
            }

            TextMeshProUGUI text = NewText(rt, "Label", label, 15f, TextColor, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 4f, 4f, 2f, 2f);
            return button;
        }

        private TextMeshProUGUI FindChildText(Transform parent)
        {
            if (parent == null) return null;
            return parent.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        /// <summary>Small per-row reset button: restores only this single setting to its default.</summary>
        private Button AddResetButton(RectTransform row, Action reset, bool topRight)
        {
            if (row == null || reset == null) return null;
            Button button = NewButton(row, "Reset", L10n.ResetShort, false, () =>
            {
                try
                {
                    reset();
                    ModConfig.NotifyChanged();
                    RefreshBindings();
                    ShowSavedToast();
                }
                catch (Exception e)
                {
                    Log.Warn("Per-setting reset failed: " + e.Message);
                }
            });
            RectTransform rt = (RectTransform)button.transform;
            if (topRight)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(42f, 22f);
                rt.anchoredPosition = new Vector2(-8f, -4f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.sizeDelta = new Vector2(42f, 24f);
                rt.anchoredPosition = new Vector2(-8f, 0f);
            }
            TextMeshProUGUI label = FindChildText(button.transform);
            if (label != null)
            {
                label.fontSize = 11f;
                label.color = MutedTextColor;
            }
            return button;
        }

        // -------------------------------------------------------------- assets
        private Sprite WhiteSprite
        {
            get
            {
                if (_white == null) _white = MakeWhiteSprite();
                return _white;
            }
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
            catch (Exception e)
            {
                Log.Warn("White sprite creation failed: " + e.Message);
                return null;
            }
        }

        private Image NewImage(Transform parent, string name, Color color, bool raycast, bool stretch)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (stretch) Stretch(rt);
            return image;
        }

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

            // Prefer a font explicitly known to carry the Chinese UI glyphs.
            try
            {
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                TMP_FontAsset bestCjk = null;
                TMP_FontAsset bestAscii = null;
                for (int i = 0; i < fonts.Length; i++)
                {
                    TMP_FontAsset f = fonts[i];
                    if (f == null) continue;
                    if (HasCjk(f) && HasAscii(f))
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
                try
                {
                    TMP_FontAsset hud = FontLibrary.HudFont;
                    if (hud != null && (!zh || HasCjk(hud))) _font = hud;
                }
                catch { }
            }

            if (_font == null)
            {
                try { _font = TMP_Settings.defaultFontAsset; } catch { }
            }

            if (_font == null) Log.Warn("Settings panel: no TMP font found; text may not render.");
            else Log.Info("Settings panel font: " + _font.name);
        }

        private static bool HasAscii(TMP_FontAsset font)
        {
            try { return font.HasCharacter('A') && font.HasCharacter('0'); } catch { return false; }
        }

        private static bool HasCjk(TMP_FontAsset font)
        {
            try { return font.HasCharacter('水') && font.HasCharacter('设'); } catch { return false; }
        }

        private void OnLanguageChanged()
        {
            try
            {
                ResolveFont();
                if (_visible) Rebuild();
            }
            catch (Exception e)
            {
                Log.Warn("Language rebuild failed: " + e.Message);
            }
        }
    }
}
