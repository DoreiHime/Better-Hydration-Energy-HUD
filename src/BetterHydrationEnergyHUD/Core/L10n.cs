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
    /// UI immediately. Chinese (Simplified / Traditional) and English are fully translated;
    /// every other game language falls back to English.
    /// </summary>
    public static class L10n
    {
        public enum Lang
        {
            En = 0,
            ZhHans = 1,
            ZhHant = 2,
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> ZhHans = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> ZhHant = new Dictionary<string, string>(StringComparer.Ordinal);

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
        }
    }
}
