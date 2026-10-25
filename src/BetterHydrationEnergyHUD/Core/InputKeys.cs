using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>
    /// New Input System helpers. The game is driven by the Input System package, so the mod
    /// reads keys through it as well (no legacy UnityEngine.Input dependency).
    /// </summary>
    public static class InputKeys
    {
        public static bool TryParse(string name, out Key key)
        {
            key = Key.None;
            if (string.IsNullOrWhiteSpace(name)) return false;
            try
            {
                key = (Key)Enum.Parse(typeof(Key), name.Trim(), true);
                return key != Key.None;
            }
            catch
            {
                return false;
            }
        }

        public static string ToName(Key key)
        {
            return key == Key.None ? "None" : key.ToString();
        }

        public static string GetDisplayName(string keyName)
        {
            Key key;
            if (!TryParse(keyName, out key) || key == Key.None) return L10n.KeyNone;
            return PrettyName(key);
        }

        public static string PrettyName(Key key)
        {
            switch (key)
            {
                case Key.None: return L10n.KeyNone;
                case Key.LeftCtrl: return "L-Ctrl";
                case Key.RightCtrl: return "R-Ctrl";
                case Key.LeftShift: return "L-Shift";
                case Key.RightShift: return "R-Shift";
                case Key.LeftAlt: return "L-Alt";
                case Key.RightAlt: return "R-Alt";
                case Key.Escape: return "Esc";
                case Key.Space: return "Space";
                case Key.Enter: return "Enter";
                case Key.Backquote: return "`";
                case Key.LeftBracket: return "[";
                case Key.RightBracket: return "]";
                case Key.Semicolon: return ";";
                case Key.Quote: return "'";
                case Key.Comma: return ",";
                case Key.Period: return ".";
                case Key.Slash: return "/";
                case Key.Backslash: return "\\";
                case Key.Minus: return "-";
                case Key.Equals: return "=";
                default:
                    return key.ToString();
            }
        }

        public static bool IsPressedThisFrame(string keyName)
        {
            Key key;
            if (!TryParse(keyName, out key)) return false;
            return IsPressedThisFrame(key);
        }

        public static bool IsPressedThisFrame(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || key == Key.None) return false;
            try
            {
                KeyControl control = keyboard[key];
                return control != null && control.wasPressedThisFrame;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Returns the first key pressed this frame, or Key.None.</summary>
        public static Key CapturePressedKey()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Key.None;
            try
            {
                var all = keyboard.allKeys;
                for (int i = 0; i < all.Count; i++)
                {
                    KeyControl control = all[i];
                    if (control != null && control.wasPressedThisFrame && control.keyCode != Key.None)
                        return control.keyCode;
                }
            }
            catch { }
            return Key.None;
        }

        public static bool IsTextInputFocused()
        {
            try
            {
                if (EventSystem.current == null) return false;
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected == null) return false;
                return selected.GetComponent<TMP_InputField>() != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
