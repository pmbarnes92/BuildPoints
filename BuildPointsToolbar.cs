using KSP.UI.Screens;
using UnityEngine;

namespace BuildPoints
{
    /// <summary>
    /// Adds the mod's button to the stock App Launcher (the row of icons
    /// top-right of the screen) and owns the window it opens: a Settings
    /// window with text-entry fields (Space Center), or a Cost Breakdown
    /// window for the current craft (VAB/SPH). One class handles both
    /// since they're mutually exclusive by scene and share the toggle
    /// button plumbing.
    ///
    /// The Settings window edits this save's own copy of the settings
    /// (BuildPointsScenario.Instance.Settings) — changes here only affect
    /// the current game, the same way editing CurrentPoints wouldn't touch
    /// any other save. GameData/BuildPoints/PluginData/settings.cfg (hand
    /// edited outside the game) supplies the defaults new saves start
    /// from; "Reset to Global Defaults" below re-applies it to this save.
    ///
    /// Uses stock ApplicationLauncher rather than a third-party toolbar
    /// mod (Blizzy's Toolbar, etc.) so nothing extra needs installing.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class BuildPointsToolbar : MonoBehaviour
    {
        private ApplicationLauncherButton button;
        private bool showWindow;
        private Rect windowRect = new Rect(300, 100, 360, 0);
        private bool relevantScene;

        // --- Settings window state (Space Center only) ---
        private string[] textFields;

        private static readonly string[] FieldLabels =
        {
            "Base accrual (BP / day)",
            "Facility level bonus (%/level)",
            "Storage cap (BP)",
            "Constant cost per launch (BP)",
            "Cost per unit of vessel funds cost",
            "Cost per part (BP)",
            "Cost per tonne (BP)",
            "Minimum craft cost (BP)",
            "Recovery refund (%)"
        };

        public void Awake()
        {
            BuildPointsConfig.EnsureLoaded();

            GameScenes scene = HighLogic.LoadedScene;
            relevantScene = scene == GameScenes.SPACECENTER || scene == GameScenes.EDITOR;
            if (!relevantScene) return;

            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveButton);
        }

        public void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveButton);
            RemoveButton();
        }

        private void AddButton()
        {
            if (!relevantScene || button != null || ApplicationLauncher.Instance == null) return;

            // NOTE: replace with a real 38x38 icon (load via
            // GameDatabase.Instance.GetTexture("BuildPoints/icon", false))
            // before shipping — a blank square just proves the wiring works.
            Texture2D icon = Texture2D.whiteTexture;

            ApplicationLauncher.AppScenes scenes = HighLogic.LoadedScene == GameScenes.SPACECENTER
                ? ApplicationLauncher.AppScenes.SPACECENTER
                : ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;

            button = ApplicationLauncher.Instance.AddModApplication(
                OnToggleOn, OnToggleOff, null, null, null, null, scenes, icon);
        }

        private void RemoveButton()
        {
            if (button == null || ApplicationLauncher.Instance == null) return;
            ApplicationLauncher.Instance.RemoveModApplication(button);
            button = null;
        }

        private void OnToggleOn()
        {
            showWindow = true;
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER) RefreshTextFieldsFromSettings();
        }

        private void OnToggleOff() => showWindow = false;

        private void CloseWindow()
        {
            showWindow = false;
            button?.SetFalse(false);
        }

        private void RefreshTextFieldsFromSettings()
        {
            var s = BuildPointsScenario.GetActiveSettings();
            textFields = new[]
            {
                s.baseAccrualPerDay.ToString("0.###"),
                s.facilityLevelBonusPercent.ToString("0.###"),
                s.capacity.ToString("0.###"),
                s.constantCost.ToString("0.###"),
                s.fundsCostWeight.ToString("0.###"),
                s.costPerPart.ToString("0.###"),
                s.massCostWeight.ToString("0.###"),
                s.minimumCraftCost.ToString("0.###"),
                s.recoveryRefundPercent.ToString("0.###")
            };
        }

        public void OnGUI()
        {
            if (!showWindow) return;

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawSettingsWindow, "Build Points Settings");
            else if (HighLogic.LoadedSceneIsEditor)
                windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawCostWindow, "Build Points Cost");
        }

        // ------------------------------------------------------------
        // Settings window (Space Center) — edits THIS SAVE's settings
        // ------------------------------------------------------------

        private void DrawSettingsWindow(int id)
        {
            if (textFields == null) RefreshTextFieldsFromSettings();
            var s = BuildPointsScenario.GetActiveSettings();

            GUILayout.BeginVertical();

            GUILayout.Label("Applies to this save only. Edit settings.cfg (GameData/BuildPoints/" +
                             "PluginData) to change what new saves start with.");
            GUILayout.Space(6);

            s.showBuildPointsDisplay = GUILayout.Toggle(s.showBuildPointsDisplay, "Show Build Points display");

            GUILayout.Space(8);

            for (int i = 0; i < FieldLabels.Length; i++)
            {
                GUILayout.Label(FieldLabels[i]);
                textFields[i] = GUILayout.TextField(textFields[i]);
            }

            GUILayout.Space(4);
            s.useInstantTimeSkip = GUILayout.Toggle(s.useInstantTimeSkip,
                "Use instant time-skip (off = real TimeWarp)");

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                ApplyTextFieldsToSettings();
            }
            if (GUILayout.Button("Revert"))
            {
                RefreshTextFieldsFromSettings();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Global Defaults"))
            {
                BuildPointsScenario.Instance?.ResetSettingsToGlobalDefaults();
                RefreshTextFieldsFromSettings();
            }
            if (GUILayout.Button("Close"))
            {
                CloseWindow();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void ApplyTextFieldsToSettings()
        {
            var s = BuildPointsScenario.GetActiveSettings();
            s.baseAccrualPerDay = ParseOrKeep(textFields[0], s.baseAccrualPerDay);
            s.facilityLevelBonusPercent = ParseOrKeep(textFields[1], s.facilityLevelBonusPercent);
            s.capacity = ParseOrKeep(textFields[2], s.capacity);
            s.constantCost = ParseOrKeep(textFields[3], s.constantCost);
            s.fundsCostWeight = ParseOrKeep(textFields[4], s.fundsCostWeight);
            s.costPerPart = ParseOrKeep(textFields[5], s.costPerPart);
            s.massCostWeight = ParseOrKeep(textFields[6], s.massCostWeight);
            s.minimumCraftCost = ParseOrKeep(textFields[7], s.minimumCraftCost);
            s.recoveryRefundPercent = ParseOrKeep(textFields[8], s.recoveryRefundPercent);

            // These now live only in memory on this save's Settings object,
            // exactly like CurrentPoints does — they get written to disk the
            // next time the game itself saves (quicksave, autosave, etc.),
            // via BuildPointsScenario.OnSave.

            // Re-sync the boxes from the (possibly-rejected) parsed values,
            // so a bad entry snaps back to the last good number instead of
            // silently keeping invalid text on screen.
            RefreshTextFieldsFromSettings();
        }

        /// <summary>
        /// Text fields have no min/max like the old sliders did — this only
        /// rejects unparsable or negative input. Add per-field clamps here
        /// (e.g. percentages to 0-200) if players manage to type something
        /// that breaks the cost formula in practice.
        /// </summary>
        private static float ParseOrKeep(string text, float fallback)
        {
            return float.TryParse(text, out float parsed) && parsed >= 0f ? parsed : fallback;
        }

        // ------------------------------------------------------------
        // Cost breakdown window (VAB/SPH)
        // ------------------------------------------------------------

        private void DrawCostWindow(int id)
        {
            GUILayout.BeginVertical();

            var scenario = BuildPointsScenario.Instance;
            double current = scenario != null ? scenario.CurrentPoints : 0;
            double cap = scenario != null ? scenario.GetCapacity() : 0;
            GUILayout.Label($"Current Build Points: {current:0.0} / {cap:0}");
            GUILayout.Space(8);

            if (BuildPointsCalculator.TryGetShipCostBreakdown(out var b))
            {
                GUILayout.Label($"Constant:    {b.constant,8:0.0} BP");
                GUILayout.Label($"Funds cost:  {b.funds,8:0.0} BP   ({b.fundsCost:0} funds)");
                GUILayout.Label($"Part count:  {b.partCountCost,8:0.0} BP   ({b.partCount} parts)");
                GUILayout.Label($"Mass:        {b.mass,8:0.0} BP   ({b.massTonnes:0.00} t)");
                GUILayout.Space(4);
                GUILayout.Label($"Total cost:  {b.total,8:0.0} BP");

                GUILayout.Space(4);
                GUILayout.Label(current + 1e-6 < b.total
                    ? $"Short by {b.total - current:0.0} BP."
                    : "Affordable.");
            }
            else
            {
                GUILayout.Label("No vessel in the editor.");
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Close"))
            {
                CloseWindow();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
