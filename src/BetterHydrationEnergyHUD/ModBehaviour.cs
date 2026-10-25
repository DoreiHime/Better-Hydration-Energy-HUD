using System;
using System.Collections;
using System.IO;
using Duckov.CustomOptions;
using Duckov.Modding;
using UnityEngine;
using BetterHydrationEnergyHUD.Core;
using BetterHydrationEnergyHUD.Integration;
using BetterHydrationEnergyHUD.UI;

namespace BetterHydrationEnergyHUD
{
    /// <summary>
    /// Mod entry point. The game loads a type named "&lt;mod name&gt;.ModBehaviour" that inherits
    /// Duckov.Modding.ModBehaviour.
    ///
    /// 2.0 highlights:
    ///  * event driven HUD (zero polling of the stat values),
    ///  * native settings page inside the official options screen (CustomOptionsPanel),
    ///  * a reworked in-game panel (F10) with configurable hotkeys, TMP typography and animations,
    ///  * one DLL for all languages, following the game language,
    ///  * no third-party prerequisites at all.
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private HudOverlay _hud;
        private Coroutine _watchdog;
        private bool _cleaned;
        private bool _subscribed;
        private float _nextHotkeyCheck;
        private bool _wasPanelVisible;

        // ------------------------------------------------------------------ Unity
        private void Awake()
        {
            try
            {
                ModConfig.Load();
                L10n.Init();
                Log.Info("v" + ModRuntime.ModVersion + " loading (game " + GetGameVersion() + ")");
            }
            catch (Exception e)
            {
                Log.Error("Awake failed", e);
            }
        }

        private void Start()
        {
            try
            {
                _hud = gameObject.AddComponent<HudOverlay>();
                _watchdog = StartCoroutine(WatchdogLoop());
            }
            catch (Exception e)
            {
                Log.Error("Start failed", e);
            }
        }

        protected override void OnAfterSetup()
        {
            try
            {
                Subscribe();
            }
            catch (Exception e)
            {
                Log.Error("OnAfterSetup failed", e);
            }
        }

        private void Update()
        {
            try
            {
                ModConfig.TickAutosave();

                if (_hud != null) _hud.Tick();

                HandleHotkeys();
                HandleDiagnostics();
            }
            catch (Exception e)
            {
                // Never let an exception spam the game log every frame.
                _nextHotkeyCheck = Time.unscaledTime + 5f;
                Log.Warn("Update error: " + e.Message);
            }
        }

        /// <summary>Debug-only helpers: opening the panel dumps the UI state once when Log.Verbose is on.</summary>
        private void HandleDiagnostics()
        {
            if (!Log.Verbose) return;
            try
            {
                bool visible = SettingsPanel.Instance != null && SettingsPanel.Instance.IsVisible;
                if (visible && !_wasPanelVisible)
                {
                    Diagnostics.DumpModUi("panel-open");
                }
                _wasPanelVisible = visible;
            }
            catch { }
        }

        // -------------------------------------------------------------- hotkeys
        private void HandleHotkeys()
        {
            if (Time.unscaledTime < _nextHotkeyCheck) return;
            if (InputKeys.IsTextInputFocused()) return;

            bool panelVisible = SettingsPanel.Instance != null && SettingsPanel.Instance.IsVisible;

            // F10 (or whatever the user configured) always closes the in-game panel.
            if (panelVisible)
            {
                ModRuntime.SettingsOpen = true;
                if (SettingsPanel.Instance.IsCapturingKey) return;
                if (InputKeys.IsPressedThisFrame(ModConfig.OpenPanelKey))
                {
                    SettingsPanel.Instance.Hide();
                }
                return;
            }

            ModRuntime.SettingsOpen = false;

            bool openPressed = InputKeys.IsPressedThisFrame(ModConfig.OpenPanelKey);
            bool togglePressed = InputKeys.IsPressedThisFrame(ModConfig.ToggleHudKey) &&
                                 !SameKey(ModConfig.ToggleHudKey, ModConfig.OpenPanelKey);

            if (togglePressed)
            {
                ModRuntime.HudHiddenRuntime = !ModRuntime.HudHiddenRuntime;
                if (_hud != null) _hud.ApplyConfig();
            }

            if (openPressed)
            {
                SettingsPanel.Ensure().Show();
            }
        }

        private static bool SameKey(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            if (string.Equals(a, "None", StringComparison.OrdinalIgnoreCase)) return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // ---------------------------------------------------------------- events
        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            try { CustomOptionsPanel.OnPanelEnabled += OfficialOptionsPage.HandlePanelEnabled; } catch (Exception e) { Log.Warn("CustomOptionsPanel hook unavailable: " + e.Message); }
            try { LevelManager.OnLevelBeginInitializing += OnLevelBeginInitializing; } catch { }
            try { LevelManager.OnAfterLevelInitialized += OnLevelInitialized; } catch { }
            try { LevelManager.OnMainCharacterDead += OnMainCharacterDeadHandler; } catch { }
            try { SceneLoader.onStartedLoadingScene += OnSceneLoadingStarted; } catch { }
            try { SceneLoader.onFinishedLoadingScene += OnSceneLoadingFinished; } catch { }
            try { MainMenu.OnMainMenuAwake += OnMainMenuAwake; } catch { }
            try { L10n.OnLanguageChanged += OnLanguageChanged; } catch { }
            try { ModConfig.OnChanged += OnConfigChanged; } catch { }
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            try { CustomOptionsPanel.OnPanelEnabled -= OfficialOptionsPage.HandlePanelEnabled; } catch { }
            try { LevelManager.OnLevelBeginInitializing -= OnLevelBeginInitializing; } catch { }
            try { LevelManager.OnAfterLevelInitialized -= OnLevelInitialized; } catch { }
            try { LevelManager.OnMainCharacterDead -= OnMainCharacterDeadHandler; } catch { }
            try { SceneLoader.onStartedLoadingScene -= OnSceneLoadingStarted; } catch { }
            try { SceneLoader.onFinishedLoadingScene -= OnSceneLoadingFinished; } catch { }
            try { MainMenu.OnMainMenuAwake -= OnMainMenuAwake; } catch { }
            try { L10n.OnLanguageChanged -= OnLanguageChanged; } catch { }
            try { ModConfig.OnChanged -= OnConfigChanged; } catch { }
        }

        private void OnLevelBeginInitializing()
        {
            try
            {
                if (SettingsPanel.Instance != null) SettingsPanel.Instance.Hide();
                if (_hud != null) _hud.OnLevelBegin();
            }
            catch (Exception e)
            {
                Log.Warn("OnLevelBegin failed: " + e.Message);
            }
        }

        private void OnSceneLoadingStarted(SceneLoadingContext context)
        {
            OnLevelBeginInitializing();
        }

        private void OnSceneLoadingFinished(SceneLoadingContext context)
        {
            try
            {
                if (_hud != null) _hud.OnLevelInitialized();
                StartCoroutine(RebindSoon());
            }
            catch (Exception e)
            {
                Log.Warn("OnSceneLoadingFinished failed: " + e.Message);
            }
        }

        private void OnLevelInitialized()
        {
            try
            {
                if (_hud != null) _hud.OnLevelInitialized();
                StartCoroutine(RebindSoon());
            }
            catch (Exception e)
            {
                Log.Warn("OnLevelInitialized failed: " + e.Message);
            }
        }

        private IEnumerator RebindSoon()
        {
            // The HUD may be created one or two frames after the level reports initialized.
            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                if (_hud == null) yield break;
                _hud.Watchdog();
                if (_hud.Bound) yield break;
            }
        }

        private void OnMainCharacterDeadHandler(DamageInfo info)
        {
            OnMainCharacterDead();
        }

        private void OnMainCharacterDead()
        {
            try
            {
                if (_hud != null) _hud.OnPlayerDead();
            }
            catch (Exception e)
            {
                Log.Warn("OnMainCharacterDead failed: " + e.Message);
            }
        }

        private void OnMainMenuAwake()
        {
            try
            {
                if (SettingsPanel.Instance != null) SettingsPanel.Instance.Hide();
                _nextHotkeyCheck = 0f;
            }
            catch { }
        }

        private void OnLanguageChanged()
        {
            try
            {
                if (SettingsPanel.Instance != null) SettingsPanel.Instance.Rebuild();
            }
            catch (Exception e)
            {
                Log.Warn("Language change handling failed: " + e.Message);
            }
        }

        private void OnConfigChanged()
        {
            try
            {
                if (_hud != null) _hud.ApplyConfig();
            }
            catch (Exception e)
            {
                Log.Warn("Config apply failed: " + e.Message);
            }
        }

        // -------------------------------------------------------------- watchdog
        private IEnumerator WatchdogLoop()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                yield return wait;
                try
                {
                    if (_hud != null)
                    {
                        _hud.Watchdog();
                        ModRuntime.HudBound = _hud.Bound;
                    }
                }
                catch (Exception e)
                {
                    Log.Warn("Watchdog: " + e.Message);
                }
            }
        }

        // --------------------------------------------------------------- cleanup
        protected override void OnBeforeDeactivate()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            try
            {
                if (_watchdog != null) StopCoroutine(_watchdog);
                _watchdog = null;
                Unsubscribe();
                L10n.Shutdown();
                OfficialOptionsPage.DestroyAll();
                if (SettingsPanel.Instance != null)
                {
                    try { Destroy(SettingsPanel.Instance.gameObject); } catch { }
                }
                if (_hud != null)
                {
                    try { Destroy(_hud); } catch { }
                    _hud = null;
                }
                ModRuntime.HudBound = false;
            }
            catch (Exception e)
            {
                Log.Warn("Cleanup: " + e.Message);
            }
        }

        // ---------------------------------------------------------------- helpers
        private static string GetGameVersion()
        {
            try
            {
                string info = Path.Combine(Application.dataPath, "..", "Info.ini");
                if (File.Exists(info))
                {
                    string[] lines = File.ReadAllLines(info);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.StartsWith("version", StringComparison.OrdinalIgnoreCase))
                        {
                            int eq = line.IndexOf('=');
                            if (eq > 0) return line.Substring(eq + 1).Trim();
                        }
                    }
                }
            }
            catch { }
            return "?";
        }
    }
}
