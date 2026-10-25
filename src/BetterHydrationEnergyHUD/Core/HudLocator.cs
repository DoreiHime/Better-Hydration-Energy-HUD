using System;
using UnityEngine;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>Everything needed to anchor HUD labels to the game's water / energy rings.</summary>
    public sealed class HudTargets
    {
        public Canvas Canvas;
        public WaterHUD Water;
        public EnergyHUD Energy;
        public RectTransform WaterIcon;
        public RectTransform EnergyIcon;

        public bool IsAlive
        {
            get
            {
                return Canvas != null && Water != null && Energy != null &&
                       WaterIcon != null && EnergyIcon != null;
            }
        }

        public void Clear()
        {
            Canvas = null;
            Water = null;
            Energy = null;
            WaterIcon = null;
            EnergyIcon = null;
        }
    }

    /// <summary>
    /// Locates the game HUD in <em>any</em> game version. The old 1.x implementation hard-coded
    /// transform paths ("Water/Background/Icon"); here the components are used first, which is
    /// stable across game updates, and the path lookup is only a last resort.
    /// </summary>
    public static class HudLocator
    {
        public static bool TryFind(out HudTargets targets)
        {
            targets = null;
            try
            {
                WaterHUD water = FindComponent<WaterHUD>();
                EnergyHUD energy = FindComponent<EnergyHUD>();

                if (water == null && energy == null) return false;

                Canvas canvas = FindHudCanvas(water != null ? water.transform : energy.transform);
                if (canvas == null) return false;

                RectTransform waterIcon = FindIcon(water, "Water");
                RectTransform energyIcon = FindIcon(energy, "Energy");
                if (waterIcon == null || energyIcon == null) return false;

                targets = new HudTargets
                {
                    Canvas = canvas,
                    Water = water,
                    Energy = energy,
                    WaterIcon = waterIcon,
                    EnergyIcon = energyIcon,
                };
                return targets.IsAlive;
            }
            catch (Exception e)
            {
                Log.Warn("HUD locate failed: " + e.Message);
                targets = null;
                return false;
            }
        }

        private static T FindComponent<T>() where T : Component
        {
            try
            {
                T[] all = UnityEngine.Object.FindObjectsOfType<T>(true);
                if (all == null) return null;
                T bestInactive = null;
                for (int i = 0; i < all.Length; i++)
                {
                    T c = all[i];
                    if (c == null) continue;
                    GameObject go = c.gameObject;
                    if (go.scene.IsValid() && go.activeInHierarchy) return c;
                    if (bestInactive == null && go.scene.IsValid()) bestInactive = c;
                }
                return bestInactive;
            }
            catch
            {
                return null;
            }
        }

        private static Canvas FindHudCanvas(Transform hint)
        {
            try
            {
                Canvas canvas = hint != null ? hint.GetComponentInParent<Canvas>() : null;
                if (canvas != null) return canvas;
            }
            catch { }

            try
            {
                GameObject level = GameObject.Find("LevelManager");
                if (level != null)
                {
                    Transform hud = level.transform.Find("HUDCanvas");
                    if (hud != null)
                    {
                        Canvas c = hud.GetComponent<Canvas>();
                        if (c != null) return c;
                        c = hud.GetComponentInParent<Canvas>();
                        if (c != null) return c;
                    }
                }
            }
            catch { }

            try
            {
                Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] != null && canvases[i].name == "HUDCanvas") return canvases[i];
                }
            }
            catch { }

            return null;
        }

        private static RectTransform FindIcon(WaterHUD water, string panelName)
        {
            if (water != null)
            {
                RectTransform byPath = FindChildPath(water.transform, "Background/Icon");
                if (byPath != null) return byPath;
                try { if (water.backgroundImage != null) return water.backgroundImage.rectTransform; } catch { }
                try { if (water.fillImage != null) return water.fillImage.rectTransform; } catch { }
            }
            return FindIconByPath(panelName);
        }

        private static RectTransform FindIcon(EnergyHUD energy, string panelName)
        {
            if (energy != null)
            {
                RectTransform byPath = FindChildPath(energy.transform, "Background/Icon");
                if (byPath != null) return byPath;
                try { if (energy.backgroundImage != null) return energy.backgroundImage.rectTransform; } catch { }
                try { if (energy.fillImage != null) return energy.fillImage.rectTransform; } catch { }
            }
            return FindIconByPath(panelName);
        }

        private static RectTransform FindChildPath(Transform root, string path)
        {
            if (root == null) return null;
            try
            {
                Transform t = root.Find(path);
                if (t != null) return t as RectTransform;
            }
            catch { }
            return null;
        }

        /// <summary>Legacy fallback: "&lt;panel&gt;/Background/Icon".</summary>
        private static RectTransform FindIconByPath(string panelName)
        {
            try
            {
                GameObject level = GameObject.Find("LevelManager");
                if (level == null) return null;
                Transform hud = level.transform.Find("HUDCanvas");
                if (hud == null) return null;
                Transform panel = hud.Find(panelName);
                if (panel == null) return null;
                Transform icon = panel.Find("Background/Icon");
                if (icon == null) icon = panel.Find("Icon");
                return icon as RectTransform;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Finds the game's health HUD (its TMP text is the native HUD number template).</summary>
        public static HealthHUD FindHealthHud()
        {
            return FindComponent<HealthHUD>();
        }

        /// <summary>Uses the game's own HUD font so text matches vanilla typography.</summary>
        public static void AdoptHudFont()
        {
            try
            {
                HealthHUD health = FindComponent<HealthHUD>();
                if (health != null && health.text != null && health.text.font != null)
                {
                    FontLibrary.SetPreferred(health.text.font);
                    return;
                }
            }
            catch { }

            try
            {
                TMPro.TMP_Text[] texts = UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Text>(true);
                if (texts != null)
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        if (texts[i] != null && texts[i].font != null)
                        {
                            FontLibrary.SetPreferred(texts[i].font);
                            return;
                        }
                    }
                }
            }
            catch { }

            FontLibrary.Invalidate();
        }
    }
}
