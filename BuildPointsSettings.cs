using System;
using System.Reflection;

namespace BuildPoints
{
    /// <summary>
    /// Exposes the mod's tunables on the stock Difficulty Settings screen
    /// (Settings -> Difficulty Options -> Build Points), so players can
    /// tune the economy without editing a config file by hand.
    /// </summary>
    public class BuildPointsSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Build Points";
        public override string Section => "Build Points";
        public override string DisplaySection => "Build Points";
        public override int SectionOrder => 1;
        public override GameParameters.GameMode GameMode =>
            GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE;
        public override bool HasPresets => false;

        // --- Accrual ---

        [GameParameters.CustomFloatParameterUI("Base accrual rate (BP / day)",
            toolTip = "Build Points earned per in-game day with fully un-upgraded VAB/SPH.",
            minValue = 0f, maxValue = 50f, stepCount = 100)]
        public float baseAccrualPerDay = 5f;

        [GameParameters.CustomFloatParameterUI("Facility level bonus (%/level)",
            toolTip = "Extra accrual per upgrade level of the active editor's facility (VAB or SPH), stacked additively on the base rate.",
            minValue = 0f, maxValue = 200f, stepCount = 40)]
        public float facilityLevelBonusPercent = 50f;

        // --- Capacity ---

        [GameParameters.CustomFloatParameterUI("Storage cap (BP)",
            toolTip = "Maximum Build Points that can be banked at once.",
            minValue = 10f, maxValue = 2000f, stepCount = 199)]
        public float capacity = 200f;

        // --- Cost formula ---
        // cost = constantCost + (vesselFundsCost * fundsCostWeight)
        //        + (partCount * costPerPart) + (vesselMass * massCostWeight)

        [GameParameters.CustomFloatParameterUI("Constant cost per launch (BP)",
            toolTip = "Flat Build Points cost applied to every launch, regardless of the craft. Set to 0 to disable.",
            minValue = 0f, maxValue = 50f, stepCount = 50)]
        public float constantCost = 0f;

        [GameParameters.CustomFloatParameterUI("Cost per unit of vessel cost",
            toolTip = "Build Points charged per unit of the vessel's stock funds cost (dry + resources).",
            minValue = 0f, maxValue = 1f, stepCount = 100)]
        public float fundsCostWeight = 0.01f;

        [GameParameters.CustomFloatParameterUI("Cost per part",
            toolTip = "Build Points charged per part on the vessel.",
            minValue = 0f, maxValue = 20f, stepCount = 40)]
        public float costPerPart = 0.5f;

        [GameParameters.CustomFloatParameterUI("Cost per tonne",
            toolTip = "Build Points charged per tonne of the vessel's total mass (dry + resources).",
            minValue = 0f, maxValue = 20f, stepCount = 40)]
        public float massCostWeight = 0.2f;

        [GameParameters.CustomFloatParameterUI("Minimum craft cost (BP)",
            toolTip = "Floor applied after the formula above, so trivial craft still cost something.",
            minValue = 0f, maxValue = 50f, stepCount = 50)]
        public float minimumCraftCost = 1f;

		// --- Recovery ---

		[GameParameters.CustomFloatParameterUI("Recovery refund (%)",
			toolTip = "Percentage of a vessel's Build Points cost refunded when it's recovered " +
				"(Tracking Station recovery, or the quick-recover right-click option). Calculated " +
				"with the same formula as the original launch cost. Set to 0 to disable refunds.",
			minValue = 0f, maxValue = 100f, stepCount = 100)]
		public float recoveryRefundPercent = 50f;

		// --- "Warp until affordable" behavior ---

		[GameParameters.CustomParameterUI("Use instant time-skip",
            toolTip = "OFF (default): exits to Tracking Station and runs real TimeWarp — slower, " +
                "but other mods that simulate things in the background (life support, etc.) see it " +
                "like any other warp.\nON: jumps the clock straight to the target time without leaving " +
                "the VAB/SPH — faster, but those mods may not react to it the same way they would a " +
                "normal warp.")]
        public bool useInstantTimeSkip = false;

        public override bool Enabled(MemberInfo member, GameParameters parameters) => true;
        public override bool Interactible(MemberInfo member, GameParameters parameters) => true;
        public override void SetDifficultyPreset(GameParameters.Preset preset) { /* no presets */ }
    }
}
