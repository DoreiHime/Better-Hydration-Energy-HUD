using System;
using System.Collections.Generic;
using SodaCraft.Localizations;
using UnityEngine;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>
    /// Tiny self-contained localization layer.
    ///
    /// The mod ships one DLL for all languages (the 1.x build needed one DLL per language).
    /// Language is taken from the game itself, so switching the game language switches the mod
    /// UI immediately. The game itself supports 10 languages (English, ChineseSimplified,
    /// ChineseTraditional, Japanese, Korean, French, German, Spanish, Portuguese, Russian);
    /// all of them are translated here and every other language falls back to English.
    /// </summary>
    public static class L10n
    {
        public enum Lang
        {
            En = 0,
            ZhHans = 1,
            ZhHant = 2,
            Ja = 3,
            Ko = 4,
            Fr = 5,
            De = 6,
            Es = 7,
            Pt = 8,
            Ru = 9,
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> ZhHans = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> ZhHant = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> Ja = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> Ko = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> Fr = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> De = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> Es = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> Pt = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>(StringComparer.Ordinal);

        private static Lang _lang = Lang.En;
        private static bool _subscribed;

        public static event Action OnLanguageChanged;

        public static Lang Language => _lang;

        public static void Init()
        {
            ApplyLanguage(DetectLanguage());
            if (!_subscribed)
            {
                _subscribed = true;
                try { LocalizationManager.OnSetLanguage += OnGameLanguageChanged; }
                catch (Exception e) { Log.Warn("Could not subscribe to game language events: " + e.Message); }
            }
        }

        public static void Shutdown()
        {
            if (_subscribed)
            {
                _subscribed = false;
                try { LocalizationManager.OnSetLanguage -= OnGameLanguageChanged; } catch { }
            }
        }

        private static void OnGameLanguageChanged(SystemLanguage language)
        {
            Lang next = Map(language);
            if (next == _lang) return;
            _lang = next;
            try { OnLanguageChanged?.Invoke(); } catch (Exception e) { Log.Warn("L10n listener failed: " + e.Message); }
        }

        public static Lang DetectLanguage()
        {
            try
            {
                if (LocalizationManager.Initialized) return Map(LocalizationManager.CurrentLanguage);
            }
            catch { }
            return Map(Application.systemLanguage);
        }

        private static Lang Map(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.Chinese:
                    return Lang.ZhHans;
                case SystemLanguage.ChineseTraditional:
                    return Lang.ZhHant;
                case SystemLanguage.Japanese:
                    return Lang.Ja;
                case SystemLanguage.Korean:
                    return Lang.Ko;
                case SystemLanguage.French:
                    return Lang.Fr;
                case SystemLanguage.German:
                    return Lang.De;
                case SystemLanguage.Spanish:
                    return Lang.Es;
                case SystemLanguage.Portuguese:
                    return Lang.Pt;
                case SystemLanguage.Russian:
                    return Lang.Ru;
                default:
                    return Lang.En;
            }
        }

        private static void ApplyLanguage(Lang lang)
        {
            _lang = lang;
        }

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            string value;
            switch (_lang)
            {
                case Lang.ZhHans:
                    if (ZhHans.TryGetValue(key, out value)) return value;
                    break;
                case Lang.ZhHant:
                    if (ZhHant.TryGetValue(key, out value)) return value;
                    break;
                case Lang.Ja:
                    if (Ja.TryGetValue(key, out value)) return value;
                    break;
                case Lang.Ko:
                    if (Ko.TryGetValue(key, out value)) return value;
                    break;
                case Lang.Fr:
                    if (Fr.TryGetValue(key, out value)) return value;
                    break;
                case Lang.De:
                    if (De.TryGetValue(key, out value)) return value;
                    break;
                case Lang.Es:
                    if (Es.TryGetValue(key, out value)) return value;
                    break;
                case Lang.Pt:
                    if (Pt.TryGetValue(key, out value)) return value;
                    break;
                case Lang.Ru:
                    if (Ru.TryGetValue(key, out value)) return value;
                    break;
            }
            if (En.TryGetValue(key, out value)) return value;
            return key;
        }

        public static string Format(string key, params object[] args)
        {
            string fmt = T(key);
            try { return string.Format(fmt, args); }
            catch { return fmt; }
        }

        // ---------------------------------------------------------------------
        // Game-provided stat names (automatically translated for all 10 game
        // languages by reusing the game's own localization keys).
        // ---------------------------------------------------------------------
        public static string StatWaterName
        {
            get
            {
                string s = GameText("UI_PlayerStats_Thurst");
                return string.IsNullOrEmpty(s) ? T("stat_water") : s;
            }
        }

        public static string StatEnergyName
        {
            get
            {
                string s = GameText("UI_PlayerStats_Hunger");
                return string.IsNullOrEmpty(s) ? T("stat_energy") : s;
            }
        }

        private static string GameText(string key)
        {
            try
            {
                string s = LocalizationManager.GetPlainText(key);
                if (!string.IsNullOrEmpty(s) && !(s.Length > 2 && s[0] == '*' && s[s.Length - 1] == '*')) return s;
            }
            catch { }
            return null;
        }

        // ---------------------------------------------------------------------
        // Typed accessors used by the settings UI (keeps call sites typo proof).
        // ---------------------------------------------------------------------
        public static string Title => T("title");
        public static string Subtitle => T("subtitle");
        public static string SectionDisplay => T("section_display");
        public static string SectionPosition => T("section_position");
        public static string SectionStyle => T("section_style");
        public static string SectionHotkeys => T("section_hotkeys");
        public static string SectionAbout => T("section_about");
        public static string ShowWaterTop => T("show_water_top");
        public static string ShowWaterBottom => T("show_water_bottom");
        public static string ShowEnergyTop => T("show_energy_top");
        public static string ShowEnergyBottom => T("show_energy_bottom");
        public static string OffsetX => T("offset_x");
        public static string OffsetY => T("offset_y");
        public static string Above => T("above");
        public static string Below => T("below");
        public static string FontSize => T("font_size");
        public static string Decimals => T("decimals");
        public static string UsePercent => T("use_percent");
        public static string HideDiffWhenFull => T("hide_diff_when_full");
        public static string LowWarning => T("low_warning");
        public static string LowThreshold => T("low_threshold");
        public static string FontBold => T("font_bold");
        public static string TextShadow => T("text_shadow");
        public static string KeyOpenPanel => T("key_open_panel");
        public static string KeyToggleHud => T("key_toggle_hud");
        public static string KeyNone => T("key_none");
        public static string KeyPress => T("key_press");
        public static string KeyEscCancel => T("key_esc_cancel");
        public static string ButtonReset => T("btn_reset");
        public static string ButtonClose => T("btn_close");
        public static string HintAutosave => T("hint_autosave");
        public static string AboutFormat => T("about_format");
        public static string Saved => T("saved");
        public static string Percent => T("percent");
        public static string OnText => T("on");
        public static string OffText => T("off");
        public static string TabTitle => T("tab_title");
        public static string TabNote => T("tab_note");
        public static string ResetShort => T("reset_short");

        // ---------------------------------------------------------------------
        // Translation tables.
        // Keys are intentionally stable and flat; add new keys to all three
        // dictionaries (English is the fallback for every other language).
        // ---------------------------------------------------------------------
        static L10n()
        {
            // ---------------- English ----------------
            En["title"] = "Better Hydration & Energy HUD";
            En["subtitle"] = "HUD Settings";
            En["section_display"] = "DISPLAY";
            En["section_position"] = "POSITION OFFSET (px)";
            En["section_style"] = "STYLE & FORMAT";
            En["section_hotkeys"] = "HOTKEYS";
            En["section_about"] = "ABOUT";
            En["show_water_top"] = "Hydration - missing (above ring)";
            En["show_water_bottom"] = "Hydration - current / max (below ring)";
            En["show_energy_top"] = "Hunger - missing (above ring)";
            En["show_energy_bottom"] = "Hunger - current / max (below ring)";
            En["offset_x"] = "X offset";
            En["offset_y"] = "Y offset";
            En["above"] = "Above";
            En["below"] = "Below";
            En["font_size"] = "Font size";
            En["decimals"] = "Decimals";
            En["use_percent"] = "Show percentage";
            En["hide_diff_when_full"] = "Hide missing value when full";
            En["low_warning"] = "Low value warning (recolor)";
            En["low_threshold"] = "Warning threshold";
            En["font_bold"] = "Bold text";
            En["text_shadow"] = "Text shadow";
            En["key_open_panel"] = "Open settings panel";
            En["key_toggle_hud"] = "Show / hide the numbers";
            En["key_none"] = "Not bound";
            En["key_press"] = "Press any key...";
            En["key_esc_cancel"] = "Esc to cancel";
            En["btn_reset"] = "Reset to defaults";
            En["btn_close"] = "Close";
            En["hint_autosave"] = "Changes are saved automatically";
            En["about_format"] = "{0}  v{1}\nby {2}\nThanks to every duckling for the support!";
            En["saved"] = "Saved";
            En["percent"] = "Percent";
            En["stat_water"] = "Hydration";
            En["stat_energy"] = "Hunger";
            En["options_note_official"] = "These options are stored in the mod's settings.ini and applied immediately.";
            En["options_note_position"] = "Tip: press the in-game panel hotkey (F10 by default) for live slider fine-tuning.";
            En["options_note_f10"] = "Keys can be rebound here. Set a key to \"Not bound\" to disable it.";
            En["options_note_thanks"] = "Thanks for supporting the mod! Feedback and bug reports are welcome.";
            En["on"] = "ON";
            En["off"] = "OFF";
            En["tab_title"] = "HUD Values";
            En["tab_note"] = "Only the hotkeys live here. Open the in-game panel (default F10) to change display, position, font and other options - every change is saved instantly.";
            En["reset_short"] = "RST";

            // ---------------- 简体中文 ----------------
            ZhHans["title"] = "更好的吃喝数值可视化";
            ZhHans["subtitle"] = "HUD 设置";
            ZhHans["section_display"] = "显示内容";
            ZhHans["section_position"] = "位置微调（像素）";
            ZhHans["section_style"] = "外观与格式";
            ZhHans["section_hotkeys"] = "快捷键";
            ZhHans["section_about"] = "关于";
            ZhHans["show_water_top"] = "水分差值（圆环上方）";
            ZhHans["show_water_bottom"] = "水分 当前 / 上限（圆环下方）";
            ZhHans["show_energy_top"] = "饱腹差值（圆环上方）";
            ZhHans["show_energy_bottom"] = "饱腹 当前 / 上限（圆环下方）";
            ZhHans["offset_x"] = "横向偏移 X";
            ZhHans["offset_y"] = "纵向偏移 Y";
            ZhHans["above"] = "上方";
            ZhHans["below"] = "下方";
            ZhHans["font_size"] = "字号";
            ZhHans["decimals"] = "小数位数";
            ZhHans["use_percent"] = "显示百分比";
            ZhHans["hide_diff_when_full"] = "满值时隐藏差值";
            ZhHans["low_warning"] = "低值提醒（文字变色）";
            ZhHans["low_threshold"] = "提醒阈值";
            ZhHans["font_bold"] = "粗体";
            ZhHans["text_shadow"] = "文字阴影";
            ZhHans["key_open_panel"] = "打开设置面板";
            ZhHans["key_toggle_hud"] = "显示 / 隐藏数值";
            ZhHans["key_none"] = "未绑定";
            ZhHans["key_press"] = "请按任意键…";
            ZhHans["key_esc_cancel"] = "Esc 取消";
            ZhHans["btn_reset"] = "恢复默认";
            ZhHans["btn_close"] = "关闭";
            ZhHans["hint_autosave"] = "更改会自动保存";
            ZhHans["about_format"] = "{0}  v{1}\n作者：{2}\n感谢每一位鸭宝的支持！";
            ZhHans["saved"] = "已保存";
            ZhHans["percent"] = "百分比";
            ZhHans["stat_water"] = "水分";
            ZhHans["stat_energy"] = "饱腹";
            ZhHans["options_note_official"] = "这些选项保存在 mod 的 settings.ini 中，修改后立即生效。";
            ZhHans["options_note_position"] = "提示：游戏内按 F10（可在下方修改）打开面板，可用滑块实时微调位置。";
            ZhHans["options_note_f10"] = "可在此重新绑定快捷键；设为「未绑定」即可关闭该快捷键。";
            ZhHans["options_note_thanks"] = "感谢每一位鸭宝的支持！欢迎反馈问题与建议。";
            ZhHans["on"] = "开启";
            ZhHans["off"] = "关闭";
            ZhHans["tab_title"] = "吃喝数值";
            ZhHans["tab_note"] = "这里只放快捷键。显示内容、位置、字号等选项请在游戏内按 F10（可改键）打开面板调整，修改会即时保存。";
            ZhHans["reset_short"] = "重置";

            // ---------------- 繁體中文 ----------------
            ZhHant["title"] = "更好的吃喝數值視覺化";
            ZhHant["subtitle"] = "HUD 設定";
            ZhHant["section_display"] = "顯示內容";
            ZhHant["section_position"] = "位置微調（像素）";
            ZhHant["section_style"] = "外觀與格式";
            ZhHant["section_hotkeys"] = "快捷鍵";
            ZhHant["section_about"] = "關於";
            ZhHant["show_water_top"] = "水分差值（圓環上方）";
            ZhHant["show_water_bottom"] = "水分 目前 / 上限（圓環下方）";
            ZhHant["show_energy_top"] = "飽足差值（圓環上方）";
            ZhHant["show_energy_bottom"] = "飽足 目前 / 上限（圓環下方）";
            ZhHant["offset_x"] = "橫向偏移 X";
            ZhHant["offset_y"] = "縱向偏移 Y";
            ZhHant["above"] = "上方";
            ZhHant["below"] = "下方";
            ZhHant["font_size"] = "字級";
            ZhHant["decimals"] = "小數位數";
            ZhHant["use_percent"] = "顯示百分比";
            ZhHant["hide_diff_when_full"] = "滿值時隱藏差值";
            ZhHant["low_warning"] = "低值提醒（文字變色）";
            ZhHant["low_threshold"] = "提醒閾值";
            ZhHant["font_bold"] = "粗體";
            ZhHant["text_shadow"] = "文字陰影";
            ZhHant["key_open_panel"] = "開啟設定面板";
            ZhHant["key_toggle_hud"] = "顯示 / 隱藏數值";
            ZhHant["key_none"] = "未綁定";
            ZhHant["key_press"] = "請按任意鍵…";
            ZhHant["key_esc_cancel"] = "Esc 取消";
            ZhHant["btn_reset"] = "恢復預設";
            ZhHant["btn_close"] = "關閉";
            ZhHant["hint_autosave"] = "變更會自動儲存";
            ZhHant["about_format"] = "{0}  v{1}\n作者：{2}\n感謝每一位鴨寶的支持！";
            ZhHant["saved"] = "已儲存";
            ZhHant["percent"] = "百分比";
            ZhHant["stat_water"] = "水分";
            ZhHant["stat_energy"] = "飽足";
            ZhHant["options_note_official"] = "這些選項儲存在 mod 的 settings.ini 中，修改後立即生效。";
            ZhHant["options_note_position"] = "提示：遊戲內按 F10（可在下方修改）開啟面板，可用滑桿即時微調位置。";
            ZhHant["options_note_f10"] = "可在此重新綁定快捷鍵；設為「未綁定」即可關閉該快捷鍵。";
            ZhHant["options_note_thanks"] = "感謝每一位鴨寶的支持！歡迎回饋問題與建議。";
            ZhHant["on"] = "開啟";
            ZhHant["off"] = "關閉";
            ZhHant["tab_title"] = "吃喝數值";
            ZhHant["tab_note"] = "這裡只放快捷鍵。顯示內容、位置、字級等選項請在遊戲內按 F10（可改鍵）開啟面板調整，修改會即時儲存。";
            ZhHant["reset_short"] = "重置";

            // ---------------- 日本語 ----------------
            Ja["title"] = "より良い水分・満腹度HUD";
            Ja["subtitle"] = "HUD 設定";
            Ja["section_display"] = "表示内容";
            Ja["section_position"] = "位置の微調整（px）";
            Ja["section_style"] = "見た目と書式";
            Ja["section_hotkeys"] = "ホットキー";
            Ja["section_about"] = "このmodについて";
            Ja["show_water_top"] = "水分 – 不足分（リング上）";
            Ja["show_water_bottom"] = "水分 – 現在 / 最大（リング下）";
            Ja["show_energy_top"] = "満腹度 – 不足分（リング上）";
            Ja["show_energy_bottom"] = "満腹度 – 現在 / 最大（リング下）";
            Ja["offset_x"] = "横オフセット X";
            Ja["offset_y"] = "縦オフセット Y";
            Ja["above"] = "上";
            Ja["below"] = "下";
            Ja["font_size"] = "文字サイズ";
            Ja["decimals"] = "小数点以下";
            Ja["use_percent"] = "パーセント表示";
            Ja["hide_diff_when_full"] = "満タン時に不足分を隠す";
            Ja["low_warning"] = "低値警告（色変更）";
            Ja["low_threshold"] = "警告しきい値";
            Ja["font_bold"] = "太字";
            Ja["text_shadow"] = "文字の影";
            Ja["key_open_panel"] = "設定パネルを開く";
            Ja["key_toggle_hud"] = "数値の表示 / 非表示";
            Ja["key_none"] = "未設定";
            Ja["key_press"] = "任意のキーを押してください…";
            Ja["key_esc_cancel"] = "Esc でキャンセル";
            Ja["btn_reset"] = "既定に戻す";
            Ja["btn_close"] = "閉じる";
            Ja["hint_autosave"] = "変更は自動保存されます";
            Ja["about_format"] = "{0}  v{1}\n作者：{2}\n応援してくれるアヒルたちに感謝！";
            Ja["saved"] = "保存しました";
            Ja["percent"] = "パーセント";
            Ja["stat_water"] = "水分";
            Ja["stat_energy"] = "満腹度";
            Ja["options_note_official"] = "これらの設定は mod の settings.ini に保存され、すぐに反映されます。";
            Ja["options_note_position"] = "ヒント：ゲーム内でパネルのキー（既定は F10）を押すと、スライダーでリアルタイムに微調整できます。";
            Ja["options_note_f10"] = "ここでキーを再割り当てできます。「未設定」にすると無効になります。";
            Ja["options_note_thanks"] = "応援ありがとうございます！不具合報告やご意見も歓迎です。";
            Ja["on"] = "オン";
            Ja["off"] = "オフ";
            Ja["tab_title"] = "HUD 数値";
            Ja["tab_note"] = "ここにはホットキーだけがあります。表示内容・位置・文字サイズなどはゲーム内パネル（既定は F10）で変更でき、変更はすぐ保存されます。";
            Ja["reset_short"] = "初期化";

            // ---------------- 한국어 ----------------
            Ko["title"] = "더 나은 수분·포만감 HUD";
            Ko["subtitle"] = "HUD 설정";
            Ko["section_display"] = "표시 내용";
            Ko["section_position"] = "위치 미세 조정 (px)";
            Ko["section_style"] = "스타일 및 형식";
            Ko["section_hotkeys"] = "단축키";
            Ko["section_about"] = "정보";
            Ko["show_water_top"] = "수분 – 부족량 (링 위)";
            Ko["show_water_bottom"] = "수분 – 현재 / 최대 (링 아래)";
            Ko["show_energy_top"] = "포만감 – 부족량 (링 위)";
            Ko["show_energy_bottom"] = "포만감 – 현재 / 최대 (링 아래)";
            Ko["offset_x"] = "가로 오프셋 X";
            Ko["offset_y"] = "세로 오프셋 Y";
            Ko["above"] = "위";
            Ko["below"] = "아래";
            Ko["font_size"] = "글자 크기";
            Ko["decimals"] = "소수 자릿수";
            Ko["use_percent"] = "퍼센트 표시";
            Ko["hide_diff_when_full"] = "가득 찼을 때 부족량 숨기기";
            Ko["low_warning"] = "낮은 값 경고 (색상 변경)";
            Ko["low_threshold"] = "경고 임계값";
            Ko["font_bold"] = "굵은 글씨";
            Ko["text_shadow"] = "글자 그림자";
            Ko["key_open_panel"] = "설정 패널 열기";
            Ko["key_toggle_hud"] = "숫자 표시 / 숨기기";
            Ko["key_none"] = "지정 안 됨";
            Ko["key_press"] = "아무 키나 누르세요...";
            Ko["key_esc_cancel"] = "Esc 취소";
            Ko["btn_reset"] = "기본값으로";
            Ko["btn_close"] = "닫기";
            Ko["hint_autosave"] = "변경 사항은 자동으로 저장됩니다";
            Ko["about_format"] = "{0}  v{1}\n제작: {2}\n응원해 주시는 모든 오리에게 감사합니다!";
            Ko["saved"] = "저장됨";
            Ko["percent"] = "퍼센트";
            Ko["stat_water"] = "수분";
            Ko["stat_energy"] = "포만감";
            Ko["options_note_official"] = "이 설정은 모드의 settings.ini에 저장되며 즉시 적용됩니다.";
            Ko["options_note_position"] = "팁: 게임 내에서 패널 키(기본 F10)를 누르면 슬라이더로 실시간 미세 조정할 수 있습니다.";
            Ko["options_note_f10"] = "여기에서 키를 다시 지정할 수 있습니다. '지정 안 됨'으로 설정하면 비활성화됩니다.";
            Ko["options_note_thanks"] = "응원해 주셔서 감사합니다! 피드백과 버그 제보를 환영합니다.";
            Ko["on"] = "켜짐";
            Ko["off"] = "꺼짐";
            Ko["tab_title"] = "HUD 수치";
            Ko["tab_note"] = "여기에는 단축키만 있습니다. 표시 내용·위치·글자 크기 등은 게임 내 패널(기본 F10)에서 변경할 수 있으며 즉시 저장됩니다.";
            Ko["reset_short"] = "초기화";

            // ---------------- Français ----------------
            Fr["title"] = "Meilleur HUD hydratation & satiété";
            Fr["subtitle"] = "Réglages du HUD";
            Fr["section_display"] = "AFFICHAGE";
            Fr["section_position"] = "DÉCALAGE (px)";
            Fr["section_style"] = "STYLE & FORMAT";
            Fr["section_hotkeys"] = "RACCOURCIS";
            Fr["section_about"] = "À PROPOS";
            Fr["show_water_top"] = "Hydratation - manquant (au-dessus de l'anneau)";
            Fr["show_water_bottom"] = "Hydratation - actuel / max (sous l'anneau)";
            Fr["show_energy_top"] = "Satiété - manquant (au-dessus de l'anneau)";
            Fr["show_energy_bottom"] = "Satiété - actuel / max (sous l'anneau)";
            Fr["offset_x"] = "Décalage X";
            Fr["offset_y"] = "Décalage Y";
            Fr["above"] = "Au-dessus";
            Fr["below"] = "En dessous";
            Fr["font_size"] = "Taille de police";
            Fr["decimals"] = "Décimales";
            Fr["use_percent"] = "Afficher le pourcentage";
            Fr["hide_diff_when_full"] = "Masquer le manquant quand plein";
            Fr["low_warning"] = "Alerte valeur basse (couleur)";
            Fr["low_threshold"] = "Seuil d'alerte";
            Fr["font_bold"] = "Texte en gras";
            Fr["text_shadow"] = "Ombre du texte";
            Fr["key_open_panel"] = "Ouvrir le panneau de réglages";
            Fr["key_toggle_hud"] = "Afficher / masquer les chiffres";
            Fr["key_none"] = "Non assigné";
            Fr["key_press"] = "Appuyez sur une touche...";
            Fr["key_esc_cancel"] = "Échap pour annuler";
            Fr["btn_reset"] = "Rétablir les valeurs par défaut";
            Fr["btn_close"] = "Fermer";
            Fr["hint_autosave"] = "Les modifications sont enregistrées automatiquement";
            Fr["about_format"] = "{0}  v{1}\npar {2}\nMerci à tous les canetons pour le soutien !";
            Fr["saved"] = "Enregistré";
            Fr["percent"] = "Pourcentage";
            Fr["stat_water"] = "Hydratation";
            Fr["stat_energy"] = "Satiété";
            Fr["options_note_official"] = "Ces options sont enregistrées dans le settings.ini du mod et appliquées immédiatement.";
            Fr["options_note_position"] = "Astuce : ouvre le panneau en jeu (F10 par défaut) pour un réglage en direct avec les curseurs.";
            Fr["options_note_f10"] = "Les raccourcis peuvent être modifiés ici. Mets « Non assigné » pour le désactiver.";
            Fr["options_note_thanks"] = "Merci de soutenir le mod ! Vos retours et rapports de bugs sont les bienvenus.";
            Fr["on"] = "OUI";
            Fr["off"] = "NON";
            Fr["tab_title"] = "Valeurs du HUD";
            Fr["tab_note"] = "Ici, uniquement les raccourcis. Ouvre le panneau en jeu (F10 par défaut) pour l'affichage, la position, la police et les autres options — chaque changement est enregistré instantanément.";
            Fr["reset_short"] = "RAZ";

            // ---------------- Deutsch ----------------
            De["title"] = "Besserer Hydration- & Sättigungs-HUD";
            De["subtitle"] = "HUD-Einstellungen";
            De["section_display"] = "ANZEIGE";
            De["section_position"] = "POSITIONSVERSATZ (px)";
            De["section_style"] = "STIL & FORMAT";
            De["section_hotkeys"] = "TASTENKÜRZEL";
            De["section_about"] = "ÜBER";
            De["show_water_top"] = "Hydration – fehlt (über dem Ring)";
            De["show_water_bottom"] = "Hydration – aktuell / max (unter dem Ring)";
            De["show_energy_top"] = "Sättigung – fehlt (über dem Ring)";
            De["show_energy_bottom"] = "Sättigung – aktuell / max (unter dem Ring)";
            De["offset_x"] = "X-Versatz";
            De["offset_y"] = "Y-Versatz";
            De["above"] = "Oben";
            De["below"] = "Unten";
            De["font_size"] = "Schriftgröße";
            De["decimals"] = "Dezimalstellen";
            De["use_percent"] = "Prozent anzeigen";
            De["hide_diff_when_full"] = "Fehlwert bei Vollstand ausblenden";
            De["low_warning"] = "Warnung bei niedrigem Wert (Farbe)";
            De["low_threshold"] = "Warnschwelle";
            De["font_bold"] = "Fetter Text";
            De["text_shadow"] = "Textschatten";
            De["key_open_panel"] = "Einstellungs-Panel öffnen";
            De["key_toggle_hud"] = "Zahlen ein-/ausblenden";
            De["key_none"] = "Nicht belegt";
            De["key_press"] = "Beliebige Taste drücken...";
            De["key_esc_cancel"] = "Esc zum Abbrechen";
            De["btn_reset"] = "Auf Standard zurücksetzen";
            De["btn_close"] = "Schließen";
            De["hint_autosave"] = "Änderungen werden automatisch gespeichert";
            De["about_format"] = "{0}  v{1}\nvon {2}\nDanke an alle Entlein für die Unterstützung!";
            De["saved"] = "Gespeichert";
            De["percent"] = "Prozent";
            De["stat_water"] = "Hydration";
            De["stat_energy"] = "Sättigung";
            De["options_note_official"] = "Diese Optionen werden in der settings.ini des Mods gespeichert und sofort angewendet.";
            De["options_note_position"] = "Tipp: Drücke die Panel-Taste im Spiel (standardmäßig F10) für die Live-Feineinstellung mit den Reglern.";
            De["options_note_f10"] = "Tasten können hier neu belegt werden. Setze „Nicht belegt“, um sie zu deaktivieren.";
            De["options_note_thanks"] = "Danke für die Unterstützung! Feedback und Bugmeldungen sind willkommen.";
            De["on"] = "AN";
            De["off"] = "AUS";
            De["tab_title"] = "HUD-Werte";
            De["tab_note"] = "Hier gibt es nur die Tastenbelegung. Öffne das In-Game-Panel (standardmäßig F10), um Anzeige, Position, Schrift und andere Optionen zu ändern – jede Änderung wird sofort gespeichert.";
            De["reset_short"] = "RST";

            // ---------------- Español ----------------
            Es["title"] = "Mejor HUD de hidratación y saciedad";
            Es["subtitle"] = "Ajustes del HUD";
            Es["section_display"] = "VISUALIZACIÓN";
            Es["section_position"] = "DESPLAZAMIENTO (px)";
            Es["section_style"] = "ESTILO Y FORMATO";
            Es["section_hotkeys"] = "ATAJOS";
            Es["section_about"] = "ACERCA DE";
            Es["show_water_top"] = "Hidratación: cuánto falta (encima del anillo)";
            Es["show_water_bottom"] = "Hidratación: actual / máximo (debajo del anillo)";
            Es["show_energy_top"] = "Saciedad: cuánto falta (encima del anillo)";
            Es["show_energy_bottom"] = "Saciedad: actual / máximo (debajo del anillo)";
            Es["offset_x"] = "Desplazamiento X";
            Es["offset_y"] = "Desplazamiento Y";
            Es["above"] = "Encima";
            Es["below"] = "Debajo";
            Es["font_size"] = "Tamaño de fuente";
            Es["decimals"] = "Decimales";
            Es["use_percent"] = "Mostrar porcentaje";
            Es["hide_diff_when_full"] = "Ocultar lo que falta al estar lleno";
            Es["low_warning"] = "Aviso de valor bajo (color)";
            Es["low_threshold"] = "Umbral de aviso";
            Es["font_bold"] = "Texto en negrita";
            Es["text_shadow"] = "Sombra del texto";
            Es["key_open_panel"] = "Abrir panel de ajustes";
            Es["key_toggle_hud"] = "Mostrar / ocultar números";
            Es["key_none"] = "Sin asignar";
            Es["key_press"] = "Pulsa cualquier tecla...";
            Es["key_esc_cancel"] = "Esc para cancelar";
            Es["btn_reset"] = "Restablecer valores predeterminados";
            Es["btn_close"] = "Cerrar";
            Es["hint_autosave"] = "Los cambios se guardan automáticamente";
            Es["about_format"] = "{0}  v{1}\npor {2}\n¡Gracias a todos los patitos por el apoyo!";
            Es["saved"] = "Guardado";
            Es["percent"] = "Porcentaje";
            Es["stat_water"] = "Hidratación";
            Es["stat_energy"] = "Saciedad";
            Es["options_note_official"] = "Estas opciones se guardan en settings.ini del mod y se aplican al instante.";
            Es["options_note_position"] = "Consejo: pulsa la tecla del panel en el juego (F10 por defecto) para ajustar en vivo con los controles.";
            Es["options_note_f10"] = "Aquí puedes reasignar las teclas. Pon «Sin asignar» para desactivarlas.";
            Es["options_note_thanks"] = "¡Gracias por apoyar el mod! Se agradecen comentarios e informes de errores.";
            Es["on"] = "SÍ";
            Es["off"] = "NO";
            Es["tab_title"] = "Valores del HUD";
            Es["tab_note"] = "Aquí solo están los atajos. Abre el panel en el juego (F10 por defecto) para cambiar visualización, posición, fuente y más; cada cambio se guarda al instante.";
            Es["reset_short"] = "RST";

            // ---------------- Português ----------------
            Pt["title"] = "Melhor HUD de hidratação e saciedade";
            Pt["subtitle"] = "Definições do HUD";
            Pt["section_display"] = "EXIBIÇÃO";
            Pt["section_position"] = "DESLOCAMENTO (px)";
            Pt["section_style"] = "ESTILO E FORMATO";
            Pt["section_hotkeys"] = "ATALHOS";
            Pt["section_about"] = "SOBRE";
            Pt["show_water_top"] = "Hidratação — quanto falta (acima do anel)";
            Pt["show_water_bottom"] = "Hidratação — atual / máximo (abaixo do anel)";
            Pt["show_energy_top"] = "Saciedade — quanto falta (acima do anel)";
            Pt["show_energy_bottom"] = "Saciedade — atual / máximo (abaixo do anel)";
            Pt["offset_x"] = "Deslocamento X";
            Pt["offset_y"] = "Deslocamento Y";
            Pt["above"] = "Acima";
            Pt["below"] = "Abaixo";
            Pt["font_size"] = "Tamanho da fonte";
            Pt["decimals"] = "Casas decimais";
            Pt["use_percent"] = "Mostrar percentagem";
            Pt["hide_diff_when_full"] = "Ocultar o que falta quando cheio";
            Pt["low_warning"] = "Aviso de valor baixo (mudar cor)";
            Pt["low_threshold"] = "Limite de aviso";
            Pt["font_bold"] = "Texto a negrito";
            Pt["text_shadow"] = "Sombra do texto";
            Pt["key_open_panel"] = "Abrir painel de definições";
            Pt["key_toggle_hud"] = "Mostrar / ocultar números";
            Pt["key_none"] = "Não atribuído";
            Pt["key_press"] = "Pressiona qualquer tecla...";
            Pt["key_esc_cancel"] = "Esc para cancelar";
            Pt["btn_reset"] = "Repor predefinições";
            Pt["btn_close"] = "Fechar";
            Pt["hint_autosave"] = "As alterações são guardadas automaticamente";
            Pt["about_format"] = "{0}  v{1}\npor {2}\nObrigado a todos os patinhos pelo apoio!";
            Pt["saved"] = "Guardado";
            Pt["percent"] = "Percentagem";
            Pt["stat_water"] = "Hidratação";
            Pt["stat_energy"] = "Saciedade";
            Pt["options_note_official"] = "Estas opções são guardadas no settings.ini do mod e aplicadas de imediato.";
            Pt["options_note_position"] = "Dica: pressiona a tecla do painel no jogo (F10 por predefinição) para ajustar ao vivo com os controlos.";
            Pt["options_note_f10"] = "Podes reatribuir as teclas aqui. Define «Não atribuído» para desativar.";
            Pt["options_note_thanks"] = "Obrigado por apoiares o mod! Comentários e relatórios de bugs são bem-vindos.";
            Pt["on"] = "LIGADO";
            Pt["off"] = "DESLIGADO";
            Pt["tab_title"] = "Valores do HUD";
            Pt["tab_note"] = "Aqui só estão os atalhos. Abre o painel no jogo (F10 por predefinição) para alterar exibição, posição, fonte e outras opções — cada alteração é guardada de imediato.";
            Pt["reset_short"] = "REP";

            // ---------------- Русский ----------------
            Ru["title"] = "Улучшенный HUD воды и голода";
            Ru["subtitle"] = "Настройки HUD";
            Ru["section_display"] = "ОТОБРАЖЕНИЕ";
            Ru["section_position"] = "СМЕЩЕНИЕ (px)";
            Ru["section_style"] = "СТИЛЬ И ФОРМАТ";
            Ru["section_hotkeys"] = "ГОРЯЧИЕ КЛАВИШИ";
            Ru["section_about"] = "О МОДЕ";
            Ru["show_water_top"] = "Вода — сколько не хватает (над кольцом)";
            Ru["show_water_bottom"] = "Вода — текущее / максимум (под кольцом)";
            Ru["show_energy_top"] = "Голод — сколько не хватает (над кольцом)";
            Ru["show_energy_bottom"] = "Голод — текущее / максимум (под кольцом)";
            Ru["offset_x"] = "Смещение по X";
            Ru["offset_y"] = "Смещение по Y";
            Ru["above"] = "Сверху";
            Ru["below"] = "Снизу";
            Ru["font_size"] = "Размер шрифта";
            Ru["decimals"] = "Знаков после запятой";
            Ru["use_percent"] = "Показывать проценты";
            Ru["hide_diff_when_full"] = "Скрывать нехватку при полном";
            Ru["low_warning"] = "Предупреждение о низком значении (цвет)";
            Ru["low_threshold"] = "Порог предупреждения";
            Ru["font_bold"] = "Жирный текст";
            Ru["text_shadow"] = "Тень текста";
            Ru["key_open_panel"] = "Открыть панель настроек";
            Ru["key_toggle_hud"] = "Показать / скрыть цифры";
            Ru["key_none"] = "Не назначено";
            Ru["key_press"] = "Нажмите любую клавишу...";
            Ru["key_esc_cancel"] = "Esc — отмена";
            Ru["btn_reset"] = "Сбросить до стандартных";
            Ru["btn_close"] = "Закрыть";
            Ru["hint_autosave"] = "Изменения сохраняются автоматически";
            Ru["about_format"] = "{0}  v{1}\nавтор: {2}\nСпасибо всем утятам за поддержку!";
            Ru["saved"] = "Сохранено";
            Ru["percent"] = "Проценты";
            Ru["stat_water"] = "Вода";
            Ru["stat_energy"] = "Голод";
            Ru["options_note_official"] = "Эти параметры хранятся в settings.ini мода и применяются сразу.";
            Ru["options_note_position"] = "Совет: нажмите клавишу панели в игре (по умолчанию F10) для живой настройки ползунками.";
            Ru["options_note_f10"] = "Здесь можно переназначить клавиши. Выберите «Не назначено», чтобы отключить.";
            Ru["options_note_thanks"] = "Спасибо за поддержку мода! Отзывы и сообщения об ошибках приветствуются.";
            Ru["on"] = "ВКЛ";
            Ru["off"] = "ВЫКЛ";
            Ru["tab_title"] = "Значения HUD";
            Ru["tab_note"] = "Здесь только горячие клавиши. Откройте панель в игре (по умолчанию F10), чтобы изменить отображение, положение, шрифт и другие параметры — изменения сохраняются мгновенно.";
            Ru["reset_short"] = "СБР";
        }
    }
}
