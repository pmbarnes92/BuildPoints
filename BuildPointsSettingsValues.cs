namespace BuildPoints
{
    /// <summary>
    /// The full set of mod tunables. Used two ways: as the mod-wide
    /// defaults loaded from settings.cfg (BuildPointsConfig.Defaults), and
    /// as the live, per-save values a given game actually plays with
    /// (BuildPointsScenario.Settings). A new save starts as a copy of the
    /// global defaults; after that the two are independent — editing
    /// settings.cfg by hand or editing the in-game Settings window each
    /// only touch their own copy, until the player explicitly hits
    /// "Reset to Global Defaults".
    /// </summary>
    public class BuildPointsSettingsValues
    {
        // --- Accrual ---
        public float baseAccrualPerDay = 5f;
        public float facilityLevelBonusPercent = 50f;

        // --- Capacity ---
        public float capacity = 200f;

        // --- Cost formula ---
        // cost = constantCost + (vesselFundsCost * fundsCostWeight)
        //        + (partCount * costPerPart) + (vesselMass * massCostWeight)
        public float constantCost = 0f;
        public float fundsCostWeight = 0.01f;
        public float costPerPart = 0.5f;
        public float massCostWeight = 0.2f;
        public float minimumCraftCost = 1f;

        // --- Recovery ---
        public float recoveryRefundPercent = 50f;

        // --- "Warp until affordable" behavior ---
        public bool useInstantTimeSkip = false;

        // --- Display ---
        public bool showBuildPointsDisplay = true;

        public BuildPointsSettingsValues Clone()
        {
            var copy = new BuildPointsSettingsValues();
            copy.CopyFrom(this);
            return copy;
        }

        public void CopyFrom(BuildPointsSettingsValues other)
        {
            baseAccrualPerDay = other.baseAccrualPerDay;
            facilityLevelBonusPercent = other.facilityLevelBonusPercent;
            capacity = other.capacity;
            constantCost = other.constantCost;
            fundsCostWeight = other.fundsCostWeight;
            costPerPart = other.costPerPart;
            massCostWeight = other.massCostWeight;
            minimumCraftCost = other.minimumCraftCost;
            recoveryRefundPercent = other.recoveryRefundPercent;
            useInstantTimeSkip = other.useInstantTimeSkip;
            showBuildPointsDisplay = other.showBuildPointsDisplay;
        }

        public void Load(ConfigNode node)
        {
            ReadFloat(node, "baseAccrualPerDay", ref baseAccrualPerDay);
            ReadFloat(node, "facilityLevelBonusPercent", ref facilityLevelBonusPercent);
            ReadFloat(node, "capacity", ref capacity);
            ReadFloat(node, "constantCost", ref constantCost);
            ReadFloat(node, "fundsCostWeight", ref fundsCostWeight);
            ReadFloat(node, "costPerPart", ref costPerPart);
            ReadFloat(node, "massCostWeight", ref massCostWeight);
            ReadFloat(node, "minimumCraftCost", ref minimumCraftCost);
            ReadFloat(node, "recoveryRefundPercent", ref recoveryRefundPercent);
            ReadBool(node, "useInstantTimeSkip", ref useInstantTimeSkip);
            ReadBool(node, "showBuildPointsDisplay", ref showBuildPointsDisplay);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("baseAccrualPerDay", baseAccrualPerDay);
            node.AddValue("facilityLevelBonusPercent", facilityLevelBonusPercent);
            node.AddValue("capacity", capacity);
            node.AddValue("constantCost", constantCost);
            node.AddValue("fundsCostWeight", fundsCostWeight);
            node.AddValue("costPerPart", costPerPart);
            node.AddValue("massCostWeight", massCostWeight);
            node.AddValue("minimumCraftCost", minimumCraftCost);
            node.AddValue("recoveryRefundPercent", recoveryRefundPercent);
            node.AddValue("useInstantTimeSkip", useInstantTimeSkip);
            node.AddValue("showBuildPointsDisplay", showBuildPointsDisplay);
        }

        // NOTE: verify ConfigNode.TryGetValue(string, ref float/bool) against
        // your KSP version — BuildPointsScenario already relies on the same
        // overload for double (currentPoints/lastAccrualUT), so these should
        // exist alongside it, but confirm float/bool specifically compile.
        private static void ReadFloat(ConfigNode node, string key, ref float value)
        {
            float parsed = value;
            if (node.TryGetValue(key, ref parsed)) value = parsed;
        }

        private static void ReadBool(ConfigNode node, string key, ref bool value)
        {
            bool parsed = value;
            if (node.TryGetValue(key, ref parsed)) value = parsed;
        }
    }
}
