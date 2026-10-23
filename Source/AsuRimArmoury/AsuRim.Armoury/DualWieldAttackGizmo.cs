using System.Collections.Generic;
using Verse;

namespace AsuRim.Armoury;

internal static class DualWieldAttackGizmo
{
	internal static IEnumerable<Gizmo> Process(IEnumerable<Gizmo> values, Pawn pawn)
	{
		if (!ArmouryTracked.OffHand.Contains(pawn))
		{
			return values;
		}
		ThingWithComps thingWithComps = Arsenal.MainHand(pawn);
		ThingWithComps thingWithComps2 = Arsenal.OffHand(pawn);
		if (thingWithComps == null || thingWithComps2 == null || thingWithComps.def.IsMeleeWeapon == thingWithComps2.def.IsMeleeWeapon)
		{
			return values;
		}
		return Appended(values, thingWithComps2);
	}

	private static IEnumerable<Gizmo> Appended(IEnumerable<Gizmo> values, ThingWithComps off)
	{
		foreach (Gizmo value in values)
		{
			yield return value;
		}
		CompEquippable comp = off.GetComp<CompEquippable>();
		if (comp == null)
		{
			yield break;
		}
		foreach (Command verbsCommand in comp.GetVerbsCommands())
		{
			if (verbsCommand is Command_VerbTarget)
			{
				yield return verbsCommand;
			}
		}
	}
}
