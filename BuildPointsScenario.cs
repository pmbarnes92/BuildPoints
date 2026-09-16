using System;
using UnityEngine;

namespace BuildPoints
{
    /// <summary>
    /// Persists the player's current Build Points balance and accrues new
    /// points over time. Runs in every relevant scene so the balance keeps
    /// ticking whether the player is at the Space Center, in the editor, or
    /// in flight.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames,
        GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class BuildPointsScenario : ScenarioModule
    {
        public static BuildPointsScenario Instance { get; private set; }

        /// <summary>Current banked Build Points.</summary>
        public double CurrentPoints { get; private set; }

        // Universal time (in-game seconds) at which we last accrued points.
        // Using UT rather than real time means accrual is warp-safe and
        // doesn't grant free points while the game is paused/closed.
        private double lastAccrualUT = -1;

        public override void OnAwake()
        {
            Instance = this;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU) return;
            if (HighLogic.CurrentGame == null) return;

            double now = Planetarium.GetUniversalTime();
            if (lastAccrualUT < 0)
            {
                lastAccrualUT = now;
                return;
            }

            double elapsedSeconds = now - lastAccrualUT;
            if (elapsedSeconds <= 0) return;

            double rate = GetCurrentAccrualRatePerSecond();
            Accrue(rate * elapsedSeconds);
            lastAccrualUT = now;
        }

        /// <summary>
        /// Accrual rate in BP/second, based on the base rate plus a bonus
        /// tied to the higher of the VAB/SPH upgrade levels (0..1 normalized
        /// by stock KSP). We use whichever facility is more upgraded so
        /// players aren't penalized for specializing in one build track.
        /// </summary>
        public double GetCurrentAccrualRatePerSecond()
        {
            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<BuildPointsSettings>();
            if (settings == null) return 0;

            float vabLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);
            float sphLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);
            float facilityLevel = Mathf.Max(vabLevel, sphLevel); // 0f..1f

            double perDay = settings.baseAccrualPerDay * (1.0 + facilityLevel * (settings.facilityLevelBonusPercent / 100.0));
            return perDay / GetHomeworldDayLengthSeconds();
        }

        /// <summary>
        /// Length of a homeworld solar day, in seconds — used as the "day"
        /// unit for the accrual rate instead of a hardcoded 6h Kerbin day,
        /// so this stays correct under Kopernicus rescales/rescaled systems
        /// (JNSQ, RSS, etc.) where the homeworld's rotation period differs
        /// from stock.
        ///
        /// NOTE: verify solarDayLength against your KSP version — it's the
        /// stock CelestialBody property that drives the day-length used in
        /// the UI's date formatter, and accounts for orbital motion (solar
        /// vs. sidereal day), which is what a "day" means to the player.
        /// Falls back to sidereal rotationPeriod, then to a flat 6h, for
        /// edge cases (e.g. a tidally-locked homeworld, where solar day is
        /// undefined/infinite).
        /// </summary>
        public static double GetHomeworldDayLengthSeconds()
        {
            const double fallback = 6 * 60 * 60; // stock Kerbin day, last resort

            var home = FlightGlobals.GetHomeBody();
            if (home == null) return fallback;

            double day = home.solarDayLength;
            if (double.IsNaN(day) || double.IsInfinity(day) || day <= 0)
                day = home.rotationPeriod;
            if (double.IsNaN(day) || double.IsInfinity(day) || day <= 0)
                day = fallback;

            return day;
        }

        public void Accrue(double amount)
        {
            if (amount <= 0) return;
            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<BuildPointsSettings>();
            double cap = settings != null ? settings.capacity : double.MaxValue;
            CurrentPoints = Math.Min(CurrentPoints + amount, cap);
        }

        /// <summary>Attempts to spend points. Returns false (no state change) if insufficient.</summary>
        public bool TrySpend(double amount)
        {
            if (amount <= 0) return true;
            if (CurrentPoints + 1e-6 < amount) return false;
            CurrentPoints -= amount;
            return true;
        }

        public double GetCapacity()
        {
            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<BuildPointsSettings>();
            return settings != null ? settings.capacity : 0;
        }

        /// <summary>
        /// Seconds of (in-game) time needed before CurrentPoints would reach
        /// neededPoints at the current accrual rate. Returns 0 if already
        /// affordable, or PositiveInfinity if the rate is 0 and it's not.
        /// </summary>
        public double GetSecondsUntilAffordable(double neededPoints)
        {
            double deficit = neededPoints - CurrentPoints;
            if (deficit <= 0) return 0;

            double rate = GetCurrentAccrualRatePerSecond();
            if (rate <= 0) return double.PositiveInfinity;

            return deficit / rate;
        }

        /// <summary>
        /// Advances the game clock directly (the same technique Tracking
        /// Station's on-rails warp uses under the hood) and immediately
        /// grants the Build Points that would have accrued over that span.
        /// This is the "instant" alternative to WarpAndReturnController's
        /// real-warp approach — see BuildPointsSettings.useInstantTimeSkip.
        /// It works from inside the editor without needing to back out to
        /// Tracking Station, but as a hard jump rather than incremental
        /// warp, mods that do their own per-frame background simulation
        /// (e.g. some life support mods) may not react to it the way they
        /// would to a normal multi-second warp.
        /// </summary>
        public void SkipAheadSeconds(double seconds)
        {
            if (seconds <= 0) return;

            double rate = GetCurrentAccrualRatePerSecond();
            Accrue(rate * seconds);

            double newUT = Planetarium.GetUniversalTime() + seconds;
            Planetarium.SetUniversalTime(newUT);
            lastAccrualUT = newUT;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            double points = 0;
            node.TryGetValue("currentPoints", ref points);
            CurrentPoints = points;

            double lastUT = -1;
            node.TryGetValue("lastAccrualUT", ref lastUT);
            lastAccrualUT = lastUT;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("currentPoints", CurrentPoints);
            node.AddValue("lastAccrualUT", lastAccrualUT);
        }
    }
}
