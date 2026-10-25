using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>
    /// Persistent mod settings.
    ///
    /// The file format stays the legacy "settings.ini" format so that existing users keep
    /// their layout when upgrading from 1.x. New keys are appended with safe defaults.
    /// </summary>
    public static class ModConfig
    {
        public const int CurrentVersion = 2;

        // ---- display visibility -------------------------------------------------
        public static bool ShowWaterTop = true;
        public static bool ShowWaterBottom = true;
        public static bool ShowHungerTop = true;
        public static bool ShowHungerBottom = true;

        // ---- per-label offsets (kept identical to 1.x keys) ---------------------
        public static float WaterTopOffsetX = 0f;
        public static float WaterTopOffset = 30f;
        public static float WaterBottomOffsetX = 0f;
        public static float WaterBottomOffset = 30f;
        public static float HungerTopOffsetX = 0f;
        public static float HungerTopOffset = 30f;
        public static float HungerBottomOffsetX = 0f;
        public static float HungerBottomOffset = 30f;

        // ---- appearance ---------------------------------------------------------
        public static int HudFontSize = 22;
        public static int Decimals = 0;
        public static bool UsePercent = false;
        public static bool HideDiffWhenFull = false;
        public static bool LowValueWarning = true;
        public static int LowValueThreshold = 25;
        public static bool FontBold = false;
        public static bool TextShadow = true;

        // ---- keys ---------------------------------------------------------------
        /// <summary>InputSystem Key name ("F10"). "None" disables the shortcut.</summary>
        public static string OpenPanelKey = "F10";
        /// <summary>Quick "hide numbers" shortcut. "None" disables it.</summary>
        public static string ToggleHudKey = "None";

        // ---- runtime-only state -------------------------------------------------

        /// <summary>Default values, used by the per-setting reset buttons and ResetToDefaults.</summary>
        public static class Defaults
        {
            public const bool ShowWaterTop = true;
            public const bool ShowWaterBottom = true;
            public const bool ShowHungerTop = true;
            public const bool ShowHungerBottom = true;
            public const float WaterTopOffsetX = 0f;
            public const float WaterTopOffset = 30f;
            public const float WaterBottomOffsetX = 0f;
            public const float WaterBottomOffset = 30f;
            public const float HungerTopOffsetX = 0f;
            public const float HungerTopOffset = 30f;
            public const float HungerBottomOffsetX = 0f;
            public const float HungerBottomOffset = 30f;
            public const int HudFontSize = 22;
            public const int Decimals = 0;
            public const bool UsePercent = false;
            public const bool HideDiffWhenFull = false;
            public const bool LowValueWarning = true;
            public const int LowValueThreshold = 25;
            public const bool FontBold = false;
            public const bool TextShadow = true;
            public const string OpenPanelKey = "F10";
            public const string ToggleHudKey = "None";
        }

        /// <summary>Raised after any persisted setting changed (UI edits).</summary>
        public static event Action OnChanged;

        private static string _path;
        private static float _lastSaveTime = -100f;
        private const float SaveDebounceSeconds = 0.5f;

        public static string ConfigPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_path)) return _path;
                try
                {
                    string dir = Path.GetDirectoryName(typeof(ModConfig).Assembly.Location);
                    if (string.IsNullOrEmpty(dir)) dir = ".";
                    _path = Path.Combine(dir, "settings.ini");
                }
                catch
                {
                    _path = "settings.ini";
                }
                return _path;
            }
        }

        public static void NotifyChanged(bool persist = true)
        {
            try { OnChanged?.Invoke(); } catch (Exception e) { Log.Warn("OnChanged listener failed: " + e.Message); }
            if (persist) SaveDebounced();
        }

        public static void SaveDebounced()
        {
            _lastSaveTime = Time.unscaledTime;
        }

        /// <summary>Called from Update; writes at most twice per second while editing.</summary>
        public static void TickAutosave()
        {
            if (_lastSaveTime > -100f && Time.unscaledTime - _lastSaveTime >= SaveDebounceSeconds)
            {
                _lastSaveTime = -100f;
                Save();
            }
        }

        public static void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Better Hydration & Energy HUD - settings");
                sb.AppendLine("# 修改后可直接保存，游戏内会自动读取 / Edit and restart the mod to apply");
                sb.AppendLine("Version=" + CurrentVersion.ToString(CultureInfo.InvariantCulture));
                AppendBool(sb, "ShowWaterTop", ShowWaterTop);
                AppendBool(sb, "ShowWaterBottom", ShowWaterBottom);
                AppendBool(sb, "ShowHungerTop", ShowHungerTop);
                AppendBool(sb, "ShowHungerBottom", ShowHungerBottom);
                AppendFloat(sb, "WaterTopOffsetX", WaterTopOffsetX);
                AppendFloat(sb, "WaterTopOffset", WaterTopOffset);
                AppendFloat(sb, "WaterBottomOffsetX", WaterBottomOffsetX);
                AppendFloat(sb, "WaterBottomOffset", WaterBottomOffset);
                AppendFloat(sb, "HungerTopOffsetX", HungerTopOffsetX);
                AppendFloat(sb, "HungerTopOffset", HungerTopOffset);
                AppendFloat(sb, "HungerBottomOffsetX", HungerBottomOffsetX);
                AppendFloat(sb, "HungerBottomOffset", HungerBottomOffset);
                sb.AppendLine("HudFontSize=" + HudFontSize.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Decimals=" + Decimals.ToString(CultureInfo.InvariantCulture));
                AppendBool(sb, "UsePercent", UsePercent);
                AppendBool(sb, "HideDiffWhenFull", HideDiffWhenFull);
                AppendBool(sb, "LowValueWarning", LowValueWarning);
                sb.AppendLine("LowValueThreshold=" + LowValueThreshold.ToString(CultureInfo.InvariantCulture));
                AppendBool(sb, "FontBold", FontBold);
                AppendBool(sb, "TextShadow", TextShadow);
                sb.AppendLine("OpenPanelKey=" + OpenPanelKey);
                sb.AppendLine("ToggleHudKey=" + ToggleHudKey);
                File.WriteAllText(ConfigPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Log.Warn("Failed to save settings: " + e.Message);
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                string[] lines = File.ReadAllLines(ConfigPath);
                foreach (string raw in lines)
                {
                    string line = raw == null ? "" : raw.Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    ApplyKey(key, val);
                }
            }
            catch (Exception e)
            {
                Log.Warn("Failed to load settings: " + e.Message);
            }
        }

        private static void ApplyKey(string key, string val)
        {
            bool b;
            float f;
            int i;
            switch (key)
            {
                case "ShowWaterTop": if (TryBool(val, out b)) ShowWaterTop = b; break;
                case "ShowWaterBottom": if (TryBool(val, out b)) ShowWaterBottom = b; break;
                case "ShowHungerTop": if (TryBool(val, out b)) ShowHungerTop = b; break;
                case "ShowHungerBottom": if (TryBool(val, out b)) ShowHungerBottom = b; break;
                case "WaterTopOffsetX": if (TryFloat(val, out f)) WaterTopOffsetX = f; break;
                case "WaterTopOffset": if (TryFloat(val, out f)) WaterTopOffset = f; break;
                case "WaterBottomOffsetX": if (TryFloat(val, out f)) WaterBottomOffsetX = f; break;
                case "WaterBottomOffset": if (TryFloat(val, out f)) WaterBottomOffset = f; break;
                case "HungerTopOffsetX": if (TryFloat(val, out f)) HungerTopOffsetX = f; break;
                case "HungerTopOffset": if (TryFloat(val, out f)) HungerTopOffset = f; break;
                case "HungerBottomOffsetX": if (TryFloat(val, out f)) HungerBottomOffsetX = f; break;
                case "HungerBottomOffset": if (TryFloat(val, out f)) HungerBottomOffset = f; break;
                case "HudFontSize": if (TryInt(val, out i)) HudFontSize = Mathf.Clamp(i, 10, 48); break;
                case "Decimals": if (TryInt(val, out i)) Decimals = Mathf.Clamp(i, 0, 2); break;
                case "UsePercent": if (TryBool(val, out b)) UsePercent = b; break;
                case "HideDiffWhenFull": if (TryBool(val, out b)) HideDiffWhenFull = b; break;
                case "LowValueWarning": if (TryBool(val, out b)) LowValueWarning = b; break;
                case "LowValueThreshold": if (TryInt(val, out i)) LowValueThreshold = Mathf.Clamp(i, 0, 90); break;
                case "FontBold": if (TryBool(val, out b)) FontBold = b; break;
                case "TextShadow": if (TryBool(val, out b)) TextShadow = b; break;
                case "OpenPanelKey": if (!string.IsNullOrEmpty(val)) OpenPanelKey = val; break;
                case "ToggleHudKey": if (!string.IsNullOrEmpty(val)) ToggleHudKey = val; break;
                // legacy language switch files: ignore silently
                case "Language": break;
            }
        }

        public static void ResetToDefaults()
        {
            ShowWaterTop = Defaults.ShowWaterTop;
            ShowWaterBottom = Defaults.ShowWaterBottom;
            ShowHungerTop = Defaults.ShowHungerTop;
            ShowHungerBottom = Defaults.ShowHungerBottom;
            WaterTopOffsetX = Defaults.WaterTopOffsetX;
            WaterTopOffset = Defaults.WaterTopOffset;
            WaterBottomOffsetX = Defaults.WaterBottomOffsetX;
            WaterBottomOffset = Defaults.WaterBottomOffset;
            HungerTopOffsetX = Defaults.HungerTopOffsetX;
            HungerTopOffset = Defaults.HungerTopOffset;
            HungerBottomOffsetX = Defaults.HungerBottomOffsetX;
            HungerBottomOffset = Defaults.HungerBottomOffset;
            HudFontSize = Defaults.HudFontSize;
            Decimals = Defaults.Decimals;
            UsePercent = Defaults.UsePercent;
            HideDiffWhenFull = Defaults.HideDiffWhenFull;
            LowValueWarning = Defaults.LowValueWarning;
            LowValueThreshold = Defaults.LowValueThreshold;
            FontBold = Defaults.FontBold;
            TextShadow = Defaults.TextShadow;
            OpenPanelKey = Defaults.OpenPanelKey;
            ToggleHudKey = Defaults.ToggleHudKey;
            NotifyChanged();
        }

        private static void AppendBool(StringBuilder sb, string key, bool value)
        {
            sb.AppendLine(key + "=" + (value ? "True" : "False"));
        }

        private static void AppendFloat(StringBuilder sb, string key, float value)
        {
            sb.AppendLine(key + "=" + value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static bool TryBool(string s, out bool result)
        {
            if (bool.TryParse(s, out result)) return true;
            if (s == "1") { result = true; return true; }
            if (s == "0") { result = false; return true; }
            return false;
        }

        private static bool TryFloat(string s, out float result)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryInt(string s, out int result)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
