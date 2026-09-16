using System;
using System.IO;
using UnityEngine;

namespace BuildPoints
{
    /// <summary>
    /// Orchestrates "warp until affordable" using stock TimeWarp instead of
    /// a hard clock jump. The editor scene has no warp loop of its own, so
    /// this: (1) saves the in-progress craft to a named file, (2) exits to
    /// the Space Center, (3) runs a real TimeWarp.WarpTo() right there —
    /// confirmed by Kerbal Construction Time's own source, which runs its
    /// warp-to-completion feature from GameScenes.SPACECENTER the same way
    /// it does from Flight/Tracking Station — ticking forward frame by
    /// frame like any other warp, so mods that do their own per-frame
    /// background simulation (life support, etc.) see it exactly as they
    /// would a normal warp, and (4) stops there once the target time is
    /// reached, with a message telling the player where to find the saved
    /// craft. Doing the warp at the Space Center itself (rather than
    /// Tracking Station) means this needs only one scene load total, and
    /// it's already the scene the player needs to be in to walk into the
    /// VAB/SPH next. Build Points accrual itself needs no special
    /// handling: it already runs off elapsed Universal Time in
    /// BuildPointsScenario.FixedUpdate, so it accrues naturally while the
    /// warp is running.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, true)]
    public class WarpAndReturnController : MonoBehaviour
    {
        private static WarpAndReturnController instance;

        private bool warpPending;
        private bool awaitingWarpStart;
        private double targetUT;
        private EditorFacility pendingFacility;

        public void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Call from the editor popup. Saves the current craft under a
        /// clearly-named file and switches to the Space Center to begin a
        /// real warp toward targetUT. Returns false (does nothing) if
        /// called outside the editor or with no ship present.
        /// </summary>
        public static bool TryRequestWarpAndReturn(double targetUT)
        {
            if (instance == null) return false;
            if (!HighLogic.LoadedSceneIsEditor || EditorLogic.fetch == null || EditorLogic.fetch.ship == null)
                return false;

            var ship = EditorLogic.fetch.ship;

            // NOTE: verify shipFacility against your KSP version — this is
            // the property ShipConstruct exposes to say VAB vs SPH.
            EditorFacility facility = ship.shipFacility;

            try
            {
                string dir = Path.Combine(KSPUtil.ApplicationRootPath, "saves",
                    HighLogic.SaveFolder, "Ships", facility == EditorFacility.SPH ? "SPH" : "VAB");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "BuildPoints - continue here.craft");

                // NOTE: verify this overload against your version — some
                // KSP versions expose this as ShipConstruction.SaveShip,
                // others route through EditorLogic.fetch.ship.SaveShip()
                // returning a ConfigNode you then node.Save(path) yourself.
                ShipConstruction.SaveShip(ship, path);

                instance.pendingFacility = facility;
                instance.targetUT = targetUT;
                instance.warpPending = true;
                instance.awaitingWarpStart = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildPoints] Failed to stash craft for warp-and-return: " + e);
                return false;
            }

            HighLogic.LoadScene(GameScenes.SPACECENTER);
            return true;
        }

        public void Update()
        {
            if (!warpPending) return;
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER) return; // still mid scene-load

            if (awaitingWarpStart)
            {
                if (TimeWarp.fetch == null) return; // scene not fully ready yet this frame
                // NOTE: verify WarpTo's signature — KCT calls an overload
                // with extra (maxRate, ...) args from this same scene; the
                // single-UT overload is what most alarm-clock-style mods
                // use and should exist too, but confirm against your version.
                TimeWarp.fetch.WarpTo(targetUT);
                awaitingWarpStart = false;
                return;
            }

            if (Planetarium.GetUniversalTime() >= targetUT - 0.5)
            {
                TimeWarp.SetRate(0, true); // drop back to 1x, stay right here
                warpPending = false;

                string facilityName = pendingFacility == EditorFacility.SPH ? "SPH" : "VAB";
                ScreenMessages.PostScreenMessage(
                    $"Build Points banked. Head into the {facilityName} and load " +
                    "\"BuildPoints - continue here\" to pick up where you left off.",
                    8f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
    }
}
