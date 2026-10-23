using Verse;

namespace AsuRim.Armoury;

internal static class BrawlMeleeOnly
{
	internal static Verb Filter(Pawn pawn, Thing target, Verb verb)
	{
		if (verb == null || verb.IsMeleeAttack || !ArmourySettings.stancesEnabled)
		{
			return verb;
		}
		if (!IsHandWeapon(pawn, verb.EquipmentSource))
		{
			return verb;
		}
		if (!RangeDirector.InBrawl(pawn))
		{
			return verb;
		}
		if (!pawn.kindDef.canMeleeAttack)
		{
			return null;
		}
		return pawn.meleeVerbs?.TryGetMeleeVerb(target);
	}

	internal static bool Blocks(Verb verb)
	{
		if (!ArmourySettings.stancesEnabled || verb == null || verb.IsMeleeAttack)
		{
			return false;
		}
		if (!(verb.caster is Pawn pawn))
		{
			return false;
		}
		if (!IsHandWeapon(pawn, verb.EquipmentSource))
		{
			return false;
		}
		return RangeDirector.InBrawl(pawn);
	}

	private static bool IsHandWeapon(Pawn pawn, ThingWithComps source)
	{
		if (source == null || pawn == null)
		{
			return false;
		}
		if (source != pawn.equipment?.Primary)
		{
			return Arsenal.IsOffHand(source);
		}
		return true;
	}
}
