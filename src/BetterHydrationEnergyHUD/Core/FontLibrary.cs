using System;
using TMPro;
using UnityEngine;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>
    /// Resolves a TMP font asset that the game already uses, so the mod text matches the
    /// game's own typography (and supports CJK). Never ships its own font.
    /// </summary>
    public static class FontLibrary
    {
        private static TMP_FontAsset _font;
        private static bool _resolved;

        public static TMP_FontAsset HudFont
        {
            get
            {
                if (!_resolved || _font == null) Resolve();
                return _font;
            }
        }

        /// <summary>Call when a level is loaded so the font can be refreshed from HUD text.</summary>
        public static void Invalidate()
        {
            _resolved = false;
        }

        /// <summary>Forces a specific font, e.g. the one used by the game's own HUD.</summary>
        public static void SetPreferred(TMP_FontAsset font)
        {
            if (font == null) return;
            _font = font;
            _resolved = true;
        }

        private static void Resolve()
        {
            _resolved = true;
            _font = null;
            try
            {
                TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
                TMP_FontAsset withCjk = null;
                TMP_FontAsset any = null;
                for (int i = 0; i < all.Length; i++)
                {
                    TMP_Text text = all[i];
                    if (text == null) continue;
                    TMP_FontAsset f = text.font;
                    if (f == null) continue;
                    if (any == null) any = f;
                    try
                    {
                        if (withCjk == null && WantsCjk && HasChar(f, '水') && HasChar(f, '饱')) withCjk = f;
                    }
                    catch { }
                }
                _font = withCjk ?? any;
            }
            catch (Exception e)
            {
                Log.Warn("Font resolve failed: " + e.Message);
            }

            if (_font == null)
            {
                try { _font = TMP_Settings.defaultFontAsset; } catch { }
            }
            if (_font == null)
            {
                Log.Warn("No TMP font asset found; HUD text may not render.");
            }
        }

        private static bool WantsCjk => L10n.Language != L10n.Lang.En;

        private static bool HasChar(TMP_FontAsset font, char c)
        {
            try { return font.HasCharacter(c); }
            catch { return false; }
        }

        /// <summary>Creates an instance material with a soft underlay (readability shadow).</summary>
        public static Material CreateTextMaterial(TMP_FontAsset font, Color underlayColor, float softness)
        {
            if (font == null || font.material == null) return null;
            try
            {
                Material mat = new Material(font.material);
                mat.EnableKeyword("UNDERLAY_ON");
                mat.DisableKeyword("UNDERLAY_INNER");
                mat.SetColor("_UnderlayColor", underlayColor);
                mat.SetFloat("_UnderlayOffsetX", 1.2f);
                mat.SetFloat("_UnderlayOffsetY", -1.2f);
                mat.SetFloat("_UnderlayDilate", 0.1f);
                mat.SetFloat("_UnderlaySoftness", softness);
                return mat;
            }
            catch (Exception e)
            {
                Log.Warn("Text material creation failed: " + e.Message);
                return null;
            }
        }
    }
}
