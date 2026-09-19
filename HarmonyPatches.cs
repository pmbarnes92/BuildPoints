using HarmonyLib;
using UnityEngine;

namespace BuildPoints
{
    /// <summary>
    /// Installs the Harmony patches once at game start, and loads
    /// BuildPointsConfig.Defaults from
    /// GameData/BuildPoints/PluginData/settings.cfg (creating it with
    /// built-in defaults on first run) so it's ready before any save needs
    /// to seed its own settings from it. Harmony is the standard, safe way
    /// to intercept a stock method (here, the editor's Launch action)
    /// without hunting for a public GameEvent that fires early enough to
    /// cancel the launch outright.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class BuildPointsBootstrap : MonoBehaviour
    {
        public void Awake()
        {
            BuildPointsConfig.EnsureLoaded();

            var harmony = new Harmony("com.buildpoints.mod");
            harmony.PatchAll();
            DontDestroyOnLoad(this);
        }
    }

    /// <summary>
    /// Prefix patch on the editor's launch entry point. Returning false from
    /// a Harmony Prefix cancels the original method, so an insufficient
    /// balance simply blocks the launch with a warning popup instead of
    /// spawning the vessel and having to revert it.
    ///
    /// NOTE: "launchVessel" is the method name as of KSP 1.12.x in the
    /// decompiled Assembly-CSharp.dll. Confirm this against your own copy
    /// with ILSpy/dnSpy before shipping — if the signature differs, adjust
    /// the [HarmonyPatch] attribute (method name and/or argument types)
    /// accordingly.
    /// </summary>
    [HarmonyPatch(typeof(EditorLogic), "launchVessel")]
    public static class LaunchGatePatch
    {
        public static bool Prefix()
        {
            var scenario = BuildPointsScenario.Instance;
            if (scenario == null) return true; // fail open if not loaded

            if (!BuildPointsCalculator.TryGetCurrentShipCost(out double bpCost, out _, out _))
                return true; // couldn't compute a cost; don't block launch on a calculation failure

            double have = scenario.CurrentPoints;
            double cap = scenario.GetCapacity();

            if (bpCost <= have)
            {
                scenario.TrySpend(bpCost);
                return true; // paid — proceed with the real launch
            }

            if (bpCost > cap)
            {
                // Craft costs more than the player could ever bank. Warping
                // won't help — the craft itself needs to change, or the cap
                // needs to be raised in the Build Points Settings window.
                PopupDialog.SpawnPopupDialog(
                    new MultiOptionDialog(
                        "buildPointsImpossible",
                        $"Vessel cost: {bpCost:0.0} BP\nMaximum capacity: {cap:0.0} BP\n\n" +
                        "This craft costs more Build Points than you can ever bank. Reduce its cost, " +
                        "mass, or part count, or raise the Build Points cap in the Settings window " +
                        "(toolbar button, Space Center).",
                        "Craft Exceeds Build Points Capacity",
                        HighLogic.UISkin,
                        new DialogGUIButton("OK", () => { })),
                    false, HighLogic.UISkin);
                return false;
            }

            // Affordable in principle, just not banked yet — offer to skip
            // ahead in time rather than making the player sit and wait.
            double secondsNeeded = scenario.GetSecondsUntilAffordable(bpCost);
            bool canWarp = !double.IsInfinity(secondsNeeded);

            string body = $"Vessel cost: {bpCost:0.0} BP\nAvailable: {have:0.0} / {cap:0.0} BP\n\n" +
                (canWarp
                    ? $"At the current accrual rate, you'll have enough in about {FormatDuration(secondsNeeded)}."
                    : "Your current accrual rate is 0, so waiting won't help — check the Build Points " +
                      "Settings window or upgrade your VAB/SPH.");

            var buttons = canWarp
                ? new[]
                  {
                      new DialogGUIButton("Warp Until Affordable", () =>
                      {
                          bool instant = scenario.Settings.useInstantTimeSkip;

                          if (instant)
                          {
                              scenario.SkipAheadSeconds(secondsNeeded + 1.0); // small buffer against float rounding
                              ScreenMessages.PostScreenMessage(
                                  "Build Points accrued — press Launch again.", 4f, ScreenMessageStyle.UPPER_CENTER);
                          }
                          else
                          {
                              double targetUT = Planetarium.GetUniversalTime() + secondsNeeded + 1.0;
                              if (!WarpAndReturnController.TryRequestWarpAndReturn(targetUT))
                              {
                                  ScreenMessages.PostScreenMessage(
                                      "Couldn't start warp — try saving your craft and launching again.",
                                      5f, ScreenMessageStyle.UPPER_CENTER);
                              }
                          }
                      }),
                      new DialogGUIButton("Cancel", () => { })
                  }
                : new[] { new DialogGUIButton("OK", () => { }) };

            PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog("buildPointsInsufficient", body, "Not Enough Build Points",
                    HighLogic.UISkin, buttons),
                false, HighLogic.UISkin);

            return false; // cancel the launch
        }

        /// <summary>
        /// Formats a duration using the same homeworld "day" the accrual
        /// formula itself uses (see BuildPointsScenario.GetHomeworldDayLengthSeconds),
        /// so the estimate stays internally consistent regardless of the
        /// player's Kerbin-time/Earth-time display setting, and correct
        /// under rescaled systems where the homeworld's day length differs
        /// from stock.
        /// </summary>
        private static string FormatDuration(double seconds)
        {
            double secondsPerDay = BuildPointsScenario.GetHomeworldDayLengthSeconds();
            int days = (int)(seconds / secondsPerDay);
            seconds -= days * secondsPerDay;
            int hours = (int)(seconds / 3600);
            seconds -= hours * 3600;
            int minutes = (int)(seconds / 60);

            if (days > 0) return $"{days}d {hours}h";
            if (hours > 0) return $"{hours}h {minutes}m";
            return $"{Mathf.Max(minutes, 1)}m";
        }
    }
}
