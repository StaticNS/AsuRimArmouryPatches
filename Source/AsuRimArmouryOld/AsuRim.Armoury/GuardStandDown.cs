using System;
using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

public static class GuardStandDown
{
	public const string SurveillanceShiftId = "ferny.SurveillanceShift";

	private static bool present;

	private static bool captured;

	private static bool enabled;

	public static bool Present => present;

	public static bool Enabled
	{
		get
		{
			if (!captured)
			{
				return ArmourySettings.surveillanceShiftIntegration;
			}
			return enabled;
		}
	}

	public static bool Active
	{
		get
		{
			if (present)
			{
				return Enabled;
			}
			return false;
		}
	}

	static GuardStandDown()
	{
		enabled = true;
		try
		{
			present = ModLister.AnyModActiveNoSuffix(new List<string> { "ferny.SurveillanceShift" });
		}
		catch (Exception)
		{
			present = false;
		}
	}

	public static void CaptureSetting()
	{
		if (!captured)
		{
			enabled = ArmourySettings.surveillanceShiftIntegration;
			captured = true;
		}
	}

	public static bool IsSquadEscort(Pawn guard, Pawn charge)
	{
		if (guard == null || charge == null)
		{
			return false;
		}
		Squad squad = SquadManager.Current?.SquadOf(guard);
		if (squad != null && squad.Escorting)
		{
			return squad.Leader == charge;
		}
		return false;
	}

	public static bool LeadsEscort(Pawn charge)
	{
		if (charge == null)
		{
			return false;
		}
		Squad squad = SquadManager.Current?.SquadOf(charge);
		if (squad != null && squad.Escorting)
		{
			return squad.Leader == charge;
		}
		return false;
	}

	public static bool BodyguardAllowed(Pawn guard, Pawn charge)
	{
		if (!Active)
		{
			return true;
		}
		if (guard != null && guard.Drafted)
		{
			return IsSquadEscort(guard, charge);
		}
		return false;
	}
}
