using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterHydrationEnergyHUD.Core
{
    /// <summary>
    /// Debug helper: dumps the runtime state of the mod UI (rects, fonts, colours, alpha) to the
    /// game log. It is only triggered by the debug key or when the panel opens with
    /// <see cref="Log.Verbose"/> enabled, so it costs nothing in normal play.
    /// </summary>
    public static class Diagnostics
    {
        public static void DumpModUi(string reason)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("---- mod UI dump (" + reason + ") ----");
                Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
                int found = 0;
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas canvas = canvases[i];
                    if (canvas == null) continue;
                    if (canvas.name.IndexOf("BHE", StringComparison.OrdinalIgnoreCase) < 0 &&
                        canvas.sortingOrder != 32750)
                        continue;
                    found++;
                    sb.AppendLine("Canvas " + canvas.name + " sortingOrder=" + canvas.sortingOrder +
                                  " active=" + canvas.gameObject.activeInHierarchy);
                    DumpTransform(canvas.transform, sb, 1, 6);
                }
                if (found == 0) sb.AppendLine("(no mod canvas found)");
                Log.Info(sb.ToString());
            }
            catch (Exception e)
            {
                Log.Warn("UI dump failed: " + e.Message);
            }
        }

        private static void DumpTransform(Transform t, StringBuilder sb, int depth, int maxDepth)
        {
            if (t == null || depth > maxDepth) return;
            string indent = new string(' ', depth * 2);
            sb.Append(indent).Append(t.name);

            RectTransform rt = t as RectTransform;
            if (rt != null)
            {
                sb.Append(" size=").Append(rt.rect.width.ToString("F1")).Append("x").Append(rt.rect.height.ToString("F1"))
                  .Append(" pos=").Append(rt.anchoredPosition.x.ToString("F1")).Append(",").Append(rt.anchoredPosition.y.ToString("F1"))
                  .Append(" anchors=").Append(rt.anchorMin.x.ToString("F1")).Append(",").Append(rt.anchorMin.y.ToString("F1"))
                  .Append("..").Append(rt.anchorMax.x.ToString("F1")).Append(",").Append(rt.anchorMax.y.ToString("F1"));
            }

            TMP_Text text = t.GetComponent<TMP_Text>();
            if (text != null)
            {
                sb.Append(" [TMP text='").Append(Short(text.text)).Append("'")
                  .Append(" font=").Append(text.font != null ? text.font.name : "<null>")
                  .Append(" size=").Append(text.fontSize.ToString("F1"))
                  .Append(" color=").Append(ColorToString(text.color))
                  .Append(" enabled=").Append(text.enabled)
                  .Append(" visible=").Append(t.gameObject.activeInHierarchy);
                CanvasRenderer cr = t.GetComponent<CanvasRenderer>();
                if (cr != null) sb.Append(" crAlpha=").Append(cr.GetAlpha().ToString("F2"));
                sb.Append("]");
            }

            Image image = t.GetComponent<Image>();
            if (image != null)
            {
                sb.Append(" [Image color=").Append(ColorToString(image.color))
                  .Append(" raycast=").Append(image.raycastTarget)
                  .Append(" active=").Append(t.gameObject.activeInHierarchy).Append("]");
            }

            sb.AppendLine();
            for (int i = 0; i < t.childCount; i++)
            {
                DumpTransform(t.GetChild(i), sb, depth + 1, maxDepth);
            }
        }

        private static string Short(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ");
            return s.Length > 28 ? s.Substring(0, 28) + "..." : s;
        }

        private static string ColorToString(Color c)
        {
            return "(" + c.r.ToString("F2") + "," + c.g.ToString("F2") + "," + c.b.ToString("F2") + "," + c.a.ToString("F2") + ")";
        }
    }
}
