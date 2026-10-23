using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AsuRim.Armoury;

public static class WalkFireGizmo
{
	public static IEnumerable<Gizmo> For(Pawn pawn)
	{
		CompArsenal comp = CompArsenal.For(pawn);
		if (comp != null && ArmourySettings.walkFireEnabled && pawn.equipment?.Primary != null && pawn.equipment.Primary.def.IsRangedWeapon)
		{
			yield return new Command_Toggle
			{
				defaultLabel = ArmouryText.Get("ASA_WalkFire"),
				defaultDesc = ArmouryText.Get("ASA_WalkFire_Desc"),
				icon = StanceIcons.Get("UI/Stances/ASA_WalkFire", TexCommand.Attack),
				groupKey = 84610004,
				isActive = () => comp.walkFire,
				toggleAction = () =>
				{
					comp.walkFire = !comp.walkFire;
				}
			};
		}
	}
}
