namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>Shared runtime state that both the HUD and the settings UI need to read.</summary>
    public static class ModRuntime
    {
        public const string ModName = "BetterHydrationEnergyHUD";
        public const string ModVersion = "2.0.0";
        public const string Author = "Dorei_Hime";

        /// <summary>True once a live WaterHUD/EnergyHUD pair has been bound in a loaded level.</summary>
        public static bool HudBound;

        /// <summary>True when the optional ModSetting framework is present and connected.</summary>
        public static bool ModSettingAvailable;

        /// <summary>True while the mod panel is open (used to avoid hotkey conflicts).</summary>
        public static bool SettingsOpen;

        /// <summary>Session-only quick hide (toggled by the user's shortcut, not persisted).</summary>
        public static bool HudHiddenRuntime;
    }
}
