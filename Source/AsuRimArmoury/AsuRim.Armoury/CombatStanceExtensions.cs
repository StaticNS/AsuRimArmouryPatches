namespace AsuRim.Armoury;

public static class CombatStanceExtensions
{
	public static bool Advances(this CombatStance stance)
	{
		if (stance != CombatStance.Engage && stance != CombatStance.Brawl && stance != CombatStance.Hunt && stance != CombatStance.Breach && stance != CombatStance.Immobilise && stance != CombatStance.Suppress && stance != CombatStance.Covering)
		{
			return stance == CombatStance.Medic;
		}
		return true;
	}

	public static bool WantsMelee(this CombatStance stance)
	{
		return stance == CombatStance.Brawl;
	}

	public static string LabelKey(this CombatStance stance)
	{
		return "ASA_Stance_" + stance;
	}

	public static bool Offerable(this CombatStance stance)
	{
		return stance != CombatStance.Skirmish;
	}

	public static CombatStance Normalise(this CombatStance stance)
	{
		if (stance != CombatStance.Skirmish)
		{
			return stance;
		}
		return CombatStance.Engage;
	}

	public static bool SquadOnly(this CombatStance stance)
	{
		return stance == CombatStance.Escort;
	}

	public static string DescKey(this CombatStance stance)
	{
		return "ASA_Stance_" + stance.ToString() + "_desc";
	}

	public static string IconPath(this CombatStance stance)
	{
		return "UI/Stances/ASA_" + stance;
	}
}
