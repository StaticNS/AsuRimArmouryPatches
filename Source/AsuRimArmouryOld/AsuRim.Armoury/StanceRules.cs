using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class StanceRules
{
	public static bool CanTakeOrders(Pawn pawn)
	{
		if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
		{
			return false;
		}
		if (pawn.Faction == null || !pawn.Faction.IsPlayer)
		{
			return false;
		}
		if (pawn.IsColonistPlayerControlled)
		{
			return true;
		}
		if (pawn.RaceProps.IsMechanoid && pawn.IsColonyMech)
		{
			return true;
		}
		if (pawn.RaceProps.Animal)
		{
			return AnimalTakesOrders(pawn);
		}
		return false;
	}

	public static bool AnimalTakesOrders(Pawn pawn)
	{
		if (AnimalCanBeCommanded(pawn))
		{
			return true;
		}
		if (pawn.Drafted)
		{
			return pawn.MentalStateDef == null;
		}
		return DraftableAnimalsBridge.CanDraft(pawn);
	}

	public static bool AnimalCanBeCommanded(Pawn pawn)
	{
		if (pawn.playerSettings == null)
		{
			return false;
		}
		if (pawn.training == null)
		{
			return false;
		}
		if (!pawn.training.HasLearned(TrainableDefOf.Release))
		{
			return false;
		}
		return pawn.playerSettings.Master != null;
	}

	public static bool ShowsCombatControls(Pawn pawn)
	{
		if (!CanTakeOrders(pawn))
		{
			return false;
		}
		if (pawn.Drafted)
		{
			return true;
		}
		if (pawn.RaceProps.IsMechanoid && pawn.IsColonyMech)
		{
			return true;
		}
		return pawn.RaceProps.Animal;
	}

	public static bool Allows(Pawn pawn, CombatStance stance, out string reason)
	{
		reason = null;
		if (pawn == null)
		{
			return false;
		}
		if (pawn.WorkTagIsDisabled(WorkTags.Violent) && stance != CombatStance.Hold && stance != CombatStance.Hide)
		{
			reason = "ASA_Stance_NoViolence".Translate();
			return false;
		}
		switch (stance)
		{
		case CombatStance.Brawl:
			if (!HasAnyMelee(pawn))
			{
				reason = "ASA_Stance_NoMelee".Translate();
				return false;
			}
			break;
		case CombatStance.Breach:
			if (!PlayerBreaching.FindBreachTarget(pawn).IsValid)
			{
				reason = "ASA_Stance_NothingToBreach".Translate();
				return false;
			}
			break;
		case CombatStance.Suppress:
		case CombatStance.Covering:
			if (!HasAnyRanged(pawn))
			{
				reason = "ASA_Stance_NoRanged".Translate();
				return false;
			}
			break;
		}
		return true;
	}

	private static bool HasAnyMelee(Pawn pawn)
	{
		if (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
		{
			return true;
		}
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		if (thingWithComps != null && thingWithComps.def.IsMeleeWeapon)
		{
			return true;
		}
		return pawn.RaceProps.Humanlike;
	}

	private static bool HasAnyRanged(Pawn pawn)
	{
		return Arsenal.MainHand(pawn)?.def.IsRangedWeapon ?? false;
	}
}
