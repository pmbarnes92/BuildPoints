using System.Collections.Generic;

namespace BuildPoints
{
	/// <summary>
	/// Computes the Build Points cost of a vessel — either the one
	/// currently in the editor (charged on launch) or a just-recovered
	/// one (refunded on recovery). Both paths share the same cost
	/// formula and the same part-summing helper so they can never drift
	/// out of sync with each other.
	/// </summary>
	public static class BuildPointsCalculator
	{
		public static bool TryGetCurrentShipCost(out double bpCost, out double fundsCost, out int partCount)
		{
			bpCost = 0;
			fundsCost = 0;
			partCount = 0;

			if (!HighLogic.LoadedSceneIsEditor || EditorLogic.fetch == null || EditorLogic.fetch.ship == null)
				return false;

			var ship = EditorLogic.fetch.ship;
			partCount = ship.parts.Count;
			if (partCount == 0) return false;

			var availableParts = new List<AvailablePart>(partCount);
			foreach (Part part in ship.parts)
			{
				if (part.partInfo == null) continue; // defensive: shouldn't happen for a placed part
				availableParts.Add(part.partInfo);
			}

			SumPartCostsAndMass(availableParts, out fundsCost, out double massTonnes);

			var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<BuildPointsSettings>();
			if (settings == null) return false;

			bpCost = ComputeCost(settings, fundsCost, partCount, massTonnes);
			return true;
		}

		/// <summary>
		/// Same cost formula as TryGetCurrentShipCost, but applied to a
		/// recovered vessel's ProtoVessel rather than a live editor
		/// ShipConstruct — used to size the Build Points refund on
		/// recovery. Like the editor path, this sums cost/mass off each
		/// part's AvailablePart template (partInfo.partConfig) rather
		/// than that part's actual persisted resource levels — so it's
		/// consistent with the launch charge, but inherits the same
		/// "ignores partial fuel" simplification already flagged for
		/// GetPartCostsAndMass in the README.
		///
		/// NOTE: verify ProtoPartSnapshot.partInfo against your KSP
		/// version — it should be the same AvailablePart reference the
		/// live Part exposes, populated when the vessel is loaded/recovered.
		/// A part whose mod was removed since launch will have this null;
		/// such parts are skipped rather than failing the whole refund.
		/// </summary>
		public static bool TryGetRecoveredVesselCost(ProtoVessel protoVessel, out double bpCost, out double fundsCost, out int partCount)
		{
			bpCost = 0;
			fundsCost = 0;
			partCount = 0;

			if (protoVessel?.protoPartSnapshots == null) return false;
			partCount = protoVessel.protoPartSnapshots.Count;
			if (partCount == 0) return false;

			var availableParts = new List<AvailablePart>(partCount);
			foreach (ProtoPartSnapshot pps in protoVessel.protoPartSnapshots)
			{
				if (pps?.partInfo == null) continue;
				availableParts.Add(pps.partInfo);
			}
			if (availableParts.Count == 0) return false;

			SumPartCostsAndMass(availableParts, out fundsCost, out double massTonnes);

			var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<BuildPointsSettings>();
			if (settings == null) return false;

			bpCost = ComputeCost(settings, fundsCost, partCount, massTonnes);
			return true;
		}

		/// <summary>
		/// cost = constantCost + (fundsCost * fundsCostWeight)
		///        + (partCount * costPerPart) + (massTonnes * massCostWeight),
		/// floored at minimumCraftCost. The refund handler multiplies this
		/// result by recoveryRefundPercent — the floor applies before that
		/// percentage, same as it would at launch.
		/// </summary>
		private static double ComputeCost(BuildPointsSettings settings, double fundsCost, int partCount, double massTonnes)
		{
			double raw = settings.constantCost
				+ (fundsCost * settings.fundsCostWeight)
				+ (partCount * settings.costPerPart)
				+ (massTonnes * settings.massCostWeight);
			return raw < settings.minimumCraftCost ? settings.minimumCraftCost : raw;
		}

		// Stock helper ShipConstruction.GetPartCostsAndMass is per-part, not
		// per-ship (it takes a ConfigNode + AvailablePart, not a whole
		// ShipConstruct/ProtoVessel) — so sum it across every part to get
		// ship-wide totals. Shared by the editor and recovery paths, since
		// both ultimately hand it one AvailablePart per part.
		private static void SumPartCostsAndMass(List<AvailablePart> availableParts, out double fundsCost, out double massTonnes)
		{
			float totalDryCost = 0f, totalFuelCost = 0f, totalDryMass = 0f, totalFuelMass = 0f;
			foreach (AvailablePart ap in availableParts)
			{
				ShipConstruction.GetPartCostsAndMass(ap.partConfig, ap,
					out float dryCost, out float fuelCost, out float dryMass, out float fuelMass);
				totalDryCost += dryCost;
				totalFuelCost += fuelCost;
				totalDryMass += dryMass;
				totalFuelMass += fuelMass;
			}
			fundsCost = totalDryCost + totalFuelCost;
			massTonnes = totalDryMass + totalFuelMass;
		}
	}
}